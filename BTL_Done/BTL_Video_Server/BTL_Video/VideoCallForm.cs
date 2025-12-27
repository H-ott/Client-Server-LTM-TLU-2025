using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using NAudio.Wave;

namespace BTL_Video
{
    public partial class VideoCallForm : Form
    {
        private readonly string _remoteIp;
        private readonly int _videoPort; // remote port to send video to
        private readonly int _audioPort; // remote port to send audio to
        private readonly int _videoReceivePort; // local port to receive video on
        private readonly int _audioReceivePort; // local port to receive audio on

        private UdpClient? _videoSender;
        private UdpClient? _audioSender;
        private UdpClient? _videoReceiver;
        private UdpClient? _audioReceiver;

        private CancellationTokenSource? _cts;

        // AForge video capture
        private VideoCaptureDevice? _videoDevice;
        private int _videoWidth = 320;   // changed per suggestion
        private int _videoHeight = 240;  // changed per suggestion
        private long _jpegQuality = 30L; // changed per suggestion

        // NAudio
        private WaveInEvent? _waveIn;
        private WaveOutEvent? _waveOut;
        private BufferedWaveProvider? _waveProvider;
        private readonly WaveFormat _waveFormat = new WaveFormat(16000, 16, 1); // 16k mono PCM

        // toggles
        private bool _cameraOn = true;
        private bool _micOn = true;

        // fragmentation
        private const int ChunkSize = 5000; // use 6KB chunks per suggestion
        private readonly int _maxUdpPayload = ChunkSize;
        private int _nextMessageId = 1;
        private readonly ConcurrentDictionary<int, ReassemblyState> _reassembly = new();
        private readonly TimeSpan _reassemblyTimeout = TimeSpan.FromMilliseconds(700);

        private const int MaxAllowedChunks = 1024;           // protect against runaway chunk counts
        private const int MaxAllowedTotalBytes = 512 * 1024; // max assembled frame ~512KB

        // throttling / timing using Stopwatch
        private readonly double _stopwatchMsFactor = 1000.0 / Stopwatch.Frequency;
        private readonly int _minFrameIntervalMs = 60;
        private long _lastFrameSentTimestampMs = 0;

        // encode/send pipeline
        private readonly BlockingCollection<Bitmap> _encodeQueue = new BlockingCollection<Bitmap>(boundedCapacity: 2);
        private Task? _encodeSendTask;

        // reassembly gating - prefer newest frames, drop older incomplete ones
        private int _lastMessageId = -1; // per suggestion: last fully/seen message id

        public VideoCallForm(string remoteIp, int videoPort, int audioPort, int videoReceivePort, int audioReceivePort)
        {
            _remoteIp = remoteIp;
            _videoPort = videoPort;
            _audioPort = audioPort;
            _videoReceivePort = videoReceivePort;
            _audioReceivePort = audioReceivePort;
            InitializeComponent();
            FormClosing += VideoCallForm_FormClosing;
            Load += VideoCallForm_Load;
        }

        private void VideoCallForm_Load(object? sender, EventArgs e)
        {
            StartCall();
            _ = Task.Run(ReassemblyCleanupLoop);
        }

        private void StartCall()
        {
            _cts = new CancellationTokenSource();

            // UDP senders (no bind required)
            _videoSender = new UdpClient();
            _audioSender = new UdpClient();

            // UDP receivers bound to configured local receive ports.
            _videoReceiver = CreateBoundUdpClient(_videoReceivePort, "video");
            _audioReceiver = CreateBoundUdpClient(_audioReceivePort, "audio");

            UpdateLocalPortLabel();

            // start encode/send worker
            _encodeSendTask = Task.Run(() => EncodeSendLoop(_cts.Token));

            // Start receive loops
            _ = Task.Run(() => VideoReceiveLoop(_cts.Token));
            _ = Task.Run(() => AudioReceiveLoop(_cts.Token));

            // Start capture/send according to toggles
            if (_cameraOn) StartVideoCapture();
            if (_micOn) StartAudioCapture();

            // lightweight connectivity probe (temporary)
            _ = Task.Run(() => StartConnectivityProbe(_cts.Token));
        }

        private async Task StartConnectivityProbe(CancellationToken ct)
        {
            try
            {
                var remoteVideo = new IPEndPoint(IPAddress.Parse(_remoteIp), _videoPort);
                var remoteAudio = new IPEndPoint(IPAddress.Parse(_remoteIp), _audioPort);
                var pingBytes = System.Text.Encoding.UTF8.GetBytes("PING");

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        if (_videoSender != null) _videoSender.Send(pingBytes, pingBytes.Length, remoteVideo);
                        if (_audioSender != null) _audioSender.Send(pingBytes, pingBytes.Length, remoteAudio);
                    }
                    catch { }
                    await Task.Delay(1000, ct).ContinueWith(_ => { });
                }
            }
            catch { }
        }

        private UdpClient CreateBoundUdpClient(int port, string kind)
        {
            try
            {
                var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                try { sock.ExclusiveAddressUse = false; } catch { }
                sock.Bind(new IPEndPoint(IPAddress.Any, port));
                return new UdpClient { Client = sock };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to bind local {kind} receive UDP {port}: {ex.Message}\nFalling back to ephemeral port.");
                return new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            }
        }

        private void UpdateLocalPortLabel()
        {
            int localVideoPort = 0, localAudioPort = 0;
            try { if (_videoReceiver?.Client != null && _videoReceiver.Client.LocalEndPoint is IPEndPoint vep) localVideoPort = vep.Port; } catch { }
            try { if (_audioReceiver?.Client != null && _audioReceiver.Client.LocalEndPoint is IPEndPoint aep) localAudioPort = aep.Port; } catch { }

            try
            {
                var txt = $"Local ports: video={localVideoPort} audio={localAudioPort}";
                if (lblLocalPorts.InvokeRequired) lblLocalPorts.BeginInvoke(new Action(() => lblLocalPorts.Text = txt));
                else lblLocalPorts.Text = txt;
            }
            catch { }
        }

        #region Video capture / send / receive

        private void StartVideoCapture()
        {
            if (_videoDevice != null && _videoDevice.IsRunning) return;

            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (devices.Count == 0) return;

            var device = devices[0];
            _videoDevice = new VideoCaptureDevice(device.MonikerString);

            try
            {
                var desired = _videoDevice.VideoCapabilities;
                if (desired.Length > 0)
                {
                    VideoCapabilities best = desired[0];
                    foreach (var c in desired)
                    {
                        if (Math.Abs(c.FrameSize.Width - _videoWidth) < Math.Abs(best.FrameSize.Width - _videoWidth))
                            best = c;
                    }
                    _videoDevice.VideoResolution = best;
                }
            }
            catch { }

            _videoDevice.NewFrame += VideoDevice_NewFrame;
            _videoDevice.Start();
        }

        private void StopVideoCapture()
        {
            try
            {
                if (_videoDevice != null)
                {
                    if (_videoDevice.IsRunning) _videoDevice.SignalToStop();
                    _videoDevice.NewFrame -= VideoDevice_NewFrame;
                    _videoDevice = null;
                }
            }
            catch { }
        }

        // small helper: fast resize using HighSpeed interpolation
        // small helper: fast resize using NearestNeighbor interpolation (HighSpeed is not available)
        private Bitmap Resize(Bitmap src, int w, int h)
        {
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(src, 0, 0, w, h);
            }
            return bmp;
        }

        // use suggested lightweight JPEG encoder (quality fixed)
        private byte[]? EncodeJpeg(Bitmap bmp)
        {
            try
            {
                using var ms = new MemoryStream();
                var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                var encParams = new EncoderParameters(1);
                encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, _jpegQuality);
                bmp.Save(ms, encoder, encParams);
                return ms.ToArray();
            }
            catch { return null; }
        }

        // NewFrame now prepares a resized bitmap and enqueues for encoding (non-blocking)
        private void VideoDevice_NewFrame(object? sender, NewFrameEventArgs eventArgs)
        {
            Bitmap? previewBmp = null;
            Bitmap? toEnqueue = null;

            try
            {
                // per suggestion: resize immediately to reduce CPU + bandwidth
                using var src = (Bitmap)eventArgs.Frame.Clone();
                var resized = Resize(src, _videoWidth, _videoHeight);

                // create preview bitmap for UI
                previewBmp = (Bitmap)resized.Clone();

                // throttle enqueue using Stopwatch timestamp (ms)
                long nowMs = (long)(Stopwatch.GetTimestamp() * _stopwatchMsFactor);
                long lastMs = Interlocked.Read(ref _lastFrameSentTimestampMs);
                if (nowMs - lastMs >= _minFrameIntervalMs)
                {
                    // try to enqueue a clone for encoding, update last timestamp only if enqueue succeeds
                    toEnqueue = (Bitmap)resized.Clone();
                    if (_encodeQueue.TryAdd(toEnqueue))
                    {
                        Interlocked.Exchange(ref _lastFrameSentTimestampMs, nowMs);
                        toEnqueue = null; // ownership transferred
                        System.Diagnostics.Debug.WriteLine($"[Enqueue] frame enqueued at {nowMs} ms");
                    }
                    else
                    {
                        // queue full: drop this encode copy
                        toEnqueue?.Dispose();
                        toEnqueue = null;
                        System.Diagnostics.Debug.WriteLine("[Enqueue] queue full, drop frame");
                    }
                }

                resized.Dispose();
            }
            catch
            {
                previewBmp?.Dispose();
                toEnqueue?.Dispose();
                previewBmp = null;
                toEnqueue = null;
            }

            // update local preview
            if (previewBmp != null)
            {
                if (pictureBoxLocal.InvokeRequired)
                {
                    pictureBoxLocal.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var old = pictureBoxLocal.Image;
                            pictureBoxLocal.Image = previewBmp;
                            old?.Dispose();
                        }
                        catch { previewBmp?.Dispose(); }
                    }));
                }
                else
                {
                    var old = pictureBoxLocal.Image;
                    pictureBoxLocal.Image = previewBmp;
                    old?.Dispose();
                }
            }
        }

        // encode + send worker
        private void EncodeSendLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    Bitmap bmp = null!;
                    try
                    {
                        if (!_encodeQueue.TryTake(out bmp, 200, ct)) continue;
                    }
                    catch (OperationCanceledException) { break; }

                    try
                    {
                        var jpeg = EncodeJpeg(bmp);
                        bmp.Dispose();
                        if (jpeg != null && _videoSender != null)
                        {
                            SendUdpFragmented(_videoSender, jpeg, _remoteIp, _videoPort);
                            System.Diagnostics.Debug.WriteLine($"[Send] fragmented frame size={jpeg.Length}");
                        }
                    }
                    catch { try { bmp?.Dispose(); } catch { } }
                }
            }
            catch { }
            finally
            {
                // drain and dispose remaining bitmaps
                while (_encodeQueue.TryTake(out var remaining))
                {
                    try { remaining.Dispose(); } catch { }
                }
            }
        }

        private void SendUdpFragmented(UdpClient sender, byte[] payload, string remoteIp, int remotePort)
        {
            const int headerLen = 8;
            int maxPayloadPerPacket = _maxUdpPayload - headerLen;
            if (maxPayloadPerPacket <= 0) maxPayloadPerPacket = 1000;

            int totalChunks = (payload.Length + maxPayloadPerPacket - 1) / maxPayloadPerPacket;
            if (totalChunks > MaxAllowedChunks) totalChunks = MaxAllowedChunks; // safety
            int messageId = Interlocked.Increment(ref _nextMessageId);

            var remote = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * maxPayloadPerPacket;
                int chunkSize = Math.Min(maxPayloadPerPacket, payload.Length - offset);
                var buffer = new byte[headerLen + chunkSize];
                Buffer.BlockCopy(BitConverter.GetBytes(messageId), 0, buffer, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)totalChunks), 0, buffer, 4, 2);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)i), 0, buffer, 6, 2);
                Buffer.BlockCopy(payload, offset, buffer, headerLen, chunkSize);
                try { sender.Send(buffer, buffer.Length, remote); } catch { }
            }
        }

        #endregion

        #region Video receive / reassembly

        private async Task VideoReceiveLoop(CancellationToken ct)
        {
            if (_videoReceiver == null) return;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var res = await _videoReceiver.ReceiveAsync(ct);
                    var data = res.Buffer;
                    System.Diagnostics.Debug.WriteLine($"[Recv] UDP {data.Length} bytes from {res.RemoteEndPoint}");

                    if (data.Length >= 8)
                    {
                        int messageId = BitConverter.ToInt32(data, 0);
                        ushort total = BitConverter.ToUInt16(data, 4);
                        ushort idx = BitConverter.ToUInt16(data, 6);
                        int headerLen = 8;
                        int payloadLen = data.Length - headerLen;
                        if (payloadLen <= 0) continue;

                        // per suggestion: prefer newest frames — drop older partials when new message arrives
                        int lastSeen = Interlocked.Add(ref _lastMessageId, 0);
                        if (messageId <= lastSeen)
                        {
                            // ignore late/old messages
                            continue;
                        }

                        // if messageId is newer, drop existing partials (avoid blocking on old frames)
                        if (messageId > lastSeen)
                        {
                            foreach (var kv in _reassembly.Keys)
                            {
                                if (kv < messageId)
                                {
                                    if (_reassembly.TryRemove(kv, out var st)) st.ClearChunks();
                                }
                            }
                            Interlocked.Exchange(ref _lastMessageId, messageId);
                        }

                        if (total == 0 || total > MaxAllowedChunks) { System.Diagnostics.Debug.WriteLine($"[Recv] rejecting msgId={messageId} total={total}"); continue; }

                        var state = _reassembly.GetOrAdd(messageId, id => new ReassemblyState((int)total));
                        state.AddChunk(idx, data, headerLen, payloadLen);

                        if (state.IsComplete)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Recv] reassembly complete msgId={messageId} totalLen={state.TotalLength}");

                            if (state.TotalLength <= 0 || state.TotalLength > MaxAllowedTotalBytes)
                            {
                                _reassembly.TryRemove(messageId, out _);
                                continue;
                            }

                            var ms = new MemoryStream(state.TotalLength);
                            for (int i = 0; i < state.TotalChunks; i++)
                            {
                                var chunk = state.GetChunk(i);
                                if (chunk == null) { ms.Dispose(); break; }
                                ms.Write(chunk, 0, chunk.Length);
                            }
                            _reassembly.TryRemove(messageId, out _);
                            ms.Position = 0;

                            int b1 = ms.ReadByte();
                            int b2 = ms.ReadByte();
                            if (b1 != 0xFF || b2 != 0xD8) { ms.Dispose(); continue; }
                            ms.Position = 0;

                            Image img;
                            try { img = Image.FromStream(ms); }
                            catch { ms.Dispose(); continue; }

                            Bitmap uiBmp;
                            try { uiBmp = new Bitmap(img); }
                            catch { img.Dispose(); continue; }
                            img.Dispose();

                            if (pictureBoxVideo.InvokeRequired)
                            {
                                pictureBoxVideo.BeginInvoke(new Action(() =>
                                {
                                    var old = pictureBoxVideo.Image;
                                    pictureBoxVideo.Image = uiBmp;
                                    old?.Dispose();
                                }));
                            }
                            else
                            {
                                var old = pictureBoxVideo.Image;
                                pictureBoxVideo.Image = uiBmp;
                                old?.Dispose();
                            }
                        }

                        continue;
                    }

                    // fallback single-packet image
                    using var ms2 = new MemoryStream(data);
                    Image img2;
                    try { img2 = Image.FromStream(ms2); } catch { continue; }

                    Bitmap uiBmp2;
                    try { uiBmp2 = new Bitmap(img2); } catch { img2.Dispose(); continue; }
                    img2.Dispose();

                    if (pictureBoxVideo.InvokeRequired)
                    {
                        pictureBoxVideo.BeginInvoke(new Action(() =>
                        {
                            var old = pictureBoxVideo.Image;
                            pictureBoxVideo.Image = uiBmp2;
                            old?.Dispose();
                        }));
                    }
                    else
                    {
                        var old = pictureBoxVideo.Image;
                        pictureBoxVideo.Image = uiBmp2;
                        old?.Dispose();
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Recv] exception: {ex}"); await Task.Delay(10); }
            }
        }

        private async Task ReassemblyCleanupLoop()
        {
            while (_cts != null && !_cts.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    foreach (var kv in _reassembly.ToArray())
                    {
                        if (now - kv.Value.FirstAdded > _reassemblyTimeout)
                        {
                            kv.Value.ClearChunks();
                            _reassembly.TryRemove(kv.Key, out _);
                            System.Diagnostics.Debug.WriteLine($"[ReassemblyCleanup] removed msgId={kv.Key}");
                        }
                    }
                }
                catch { }
                await Task.Delay(250);
            }
        }

        public class ReassemblyState
        {
            private readonly byte[][] _chunks;
            private readonly int[] _lengths;
            public DateTime FirstAdded { get; } = DateTime.UtcNow;
            public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;
            public int TotalChunks { get; }
            private int _totalLength;
            public int TotalLength => _totalLength;

            public ReassemblyState(int totalChunks)
            {
                TotalChunks = totalChunks;
                _chunks = new byte[totalChunks][];
                _lengths = new int[totalChunks];
                _totalLength = 0;
            }

            public void AddChunk(int idx, byte[] buffer, int offset, int count)
            {
                if (idx < 0 || idx >= TotalChunks) return;
                var arr = new byte[count];
                Buffer.BlockCopy(buffer, offset, arr, 0, count);
                if (_chunks[idx] == null)
                {
                    _chunks[idx] = arr;
                    _lengths[idx] = count;
                    Interlocked.Add(ref _totalLength, count);
                    LastUpdated = DateTime.UtcNow;
                }
            }

            public byte[]? GetChunk(int idx)
            {
                if (idx < 0 || idx >= TotalChunks) return null;
                return _chunks[idx];
            }

            public bool IsComplete
            {
                get
                {
                    for (int i = 0; i < TotalChunks; i++) if (_chunks[i] == null) return false;
                    return true;
                }
            }

            public void ClearChunks()
            {
                for (int i = 0; i < _chunks.Length; i++) { _chunks[i] = null; _lengths[i] = 0; }
                try { Interlocked.Exchange(ref _totalLength, 0); } catch { _totalLength = 0; }
            }
        }

        #endregion

        #region Audio capture / send / receive

        // Replace StartAudioCapture() body with this
        private void StartAudioCapture()
        {
            if (_waveIn != null) return;

            try
            {
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = 0,
                    WaveFormat = _waveFormat,
                    BufferMilliseconds = 100, // larger capture buffer
                    NumberOfBuffers = 3
                };
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Audio capture start failed: {ex.Message}");
                try { _waveIn?.Dispose(); _waveIn = null; } catch { }
            }

            try
            {
                if (_waveProvider == null)
                {
                    _waveProvider = new BufferedWaveProvider(_waveFormat)
                    {
                        DiscardOnBufferOverflow = true,
                        BufferLength = _waveFormat.AverageBytesPerSecond * 4 // 4s buffer
                    };
                }

                if (_waveOut == null)
                {
                    _waveOut = new WaveOutEvent
                    {
                        DesiredLatency = 200 // increase playback latency to smooth jitter
                    };
                    _waveOut.Init(_waveProvider);
                    _waveOut.Play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Audio playback init failed: {ex.Message}");
            }
        }

        private void StopAudioCapture()
        {
            try { if (_waveIn != null) _waveIn.DataAvailable -= WaveIn_DataAvailable; _waveIn?.StopRecording(); _waveIn?.Dispose(); _waveIn = null; } catch { }
            try { _waveOut?.Stop(); _waveOut?.Dispose(); _waveOut = null; } catch { }
            try { _waveProvider = null; } catch { }
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (!_micOn) return;
                if (_audioSender == null) return;
                var remote = new IPEndPoint(IPAddress.Parse(_remoteIp), _audioPort);
                _audioSender.Send(e.Buffer, e.BytesRecorded, remote);
            }
            catch { }
        }

        private async Task AudioReceiveLoop(CancellationToken ct)
        {
            if (_audioReceiver == null) return;

            if (_waveProvider == null)
            {
                _waveProvider = new BufferedWaveProvider(_waveFormat) { DiscardOnBufferOverflow = true, BufferLength = _waveFormat.AverageBytesPerSecond * 5 };
            }
            if (_waveOut == null)
            {
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_waveProvider);
                _waveOut.Play();
            }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var res = await _audioReceiver.ReceiveAsync(ct);
                    var buffer = res.Buffer;
                    try { _waveProvider.AddSamples(buffer, 0, buffer.Length); } catch { }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception) { await Task.Delay(10); }
            }
        }

        #endregion

        // UI handlers for toggles
        private void btnToggleCamera_Click(object? sender, EventArgs e)
        {
            _cameraOn = !_cameraOn;
            if (_cameraOn) { btnToggleCamera.Text = "Turn Camera Off"; StartVideoCapture(); }
            else { btnToggleCamera.Text = "Turn Camera On"; StopVideoCapture(); if (pictureBoxLocal.InvokeRequired) pictureBoxLocal.BeginInvoke(new Action(() => { pictureBoxLocal.Image?.Dispose(); pictureBoxLocal.Image = null; })); else { pictureBoxLocal.Image?.Dispose(); pictureBoxLocal.Image = null; } }
        }

        private void btnToggleMic_Click(object? sender, EventArgs e)
        {
            _micOn = !_micOn;
            if (_micOn) { btnToggleMic.Text = "Mute Microphone"; StartAudioCapture(); }
            else { btnToggleMic.Text = "Unmute Microphone"; StopAudioCapture(); }
        }

        private void StopCall()
        {
            try { _cts?.Cancel(); } catch { }
            try { StopVideoCapture(); } catch { }
            try { StopAudioCapture(); } catch { }
            try { _encodeQueue.CompleteAdding(); _encodeSendTask?.Wait(500); } catch { }

            try { _videoSender?.Close(); _videoSender?.Dispose(); } catch { }
            try { _audioSender?.Close(); _audioSender?.Dispose(); } catch { }
            try { _videoReceiver?.Close(); _videoReceiver?.Dispose(); } catch { }
            try { _audioReceiver?.Close(); _audioReceiver?.Dispose(); } catch { }
        }

        private void VideoCallForm_FormClosing(object? sender, FormClosingEventArgs e) { StopCall(); }
    }
}