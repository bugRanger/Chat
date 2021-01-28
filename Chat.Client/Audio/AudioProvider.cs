namespace Chat.Client.Audio
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Chat.Audio;
    using Chat.Client.Network;

    public class AudioProvider : IAudioController, IAudioTransport, IDisposable
    {
        #region Fields

        private readonly Dictionary<int, AudioRoute> _routes;
        private readonly AudioPlayback _playback;
        private readonly AudioCapture _capture;
        private readonly INetworkStream _transport;

        private bool _disposed;

        #endregion Fields

        #region Events

        public event Action<IAudioPacket> Received;

        #endregion Events

        #region Properties

        public AudioFormat Format { get; }

        #endregion Properties

        #region Constructors

        public AudioProvider(AudioFormat format, INetworkStream transport)
        {
            Format = format;

            _transport = transport;
            _transport.PreparePacket += OnTransportReceived;

            _routes = new Dictionary<int, AudioRoute>();
            _playback = new AudioPlayback(Format);
            _capture = new AudioCapture(Format);
            _capture.Received += OnCaptureReceived;
        }

        ~AudioProvider()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Append(int routeId, IAudioCodec codec) 
        {
            if (_routes.ContainsKey(routeId))
            {
                return;
            }

            var route = new AudioRoute(codec, this) { Id = routeId };
            _routes[routeId] = route;
            _playback.Append(route);
        }

        public void Remove(int routeId)
        {
            if (!_routes.Remove(routeId, out var route))
            {
                return;
            }

            try
            {
                _playback.Remove(route);
            }
            finally
            {
                route.Dispose();
            }
        }

        public void Send(IAudioPacket packet) 
        {
            _transport.Send(packet.Pack());
        }

        private void OnTransportReceived(byte[] bytes, ref int offset, int count)
        {
            var packet = new AudioPacket();

            while (packet.TryUnpack(bytes, ref offset, count))
            {
                Received?.Invoke(packet);
            }
        }

        private void OnCaptureReceived(ArraySegment<byte> bytes)
        {
            foreach (var route in _routes.Values)
            {
                route.Write(bytes);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _capture.Received -= OnCaptureReceived;
                _capture.Dispose();
                _playback.Dispose();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
