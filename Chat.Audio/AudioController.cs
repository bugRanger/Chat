namespace Chat.Client.Audio
{
    using System;
    using System.Collections.Generic;

    using Chat.Audio;
    using Chat.Net.Socket;

    public class AudioController : IAudioController, IAudioTransport, IDisposable
    {
        #region Fields

        private readonly Func<AudioFormat, IAudioCodec> _codecFactory;
        private readonly Dictionary<int, AudioRoute> _routes;
        private readonly List<IAudioConsumer> _consumers;
        private readonly INetworkStream _transport;

        private bool _disposed;

        #endregion Fields

        #region Properties

        public AudioFormat Format { get; }

        #endregion Properties

        #region Constructors

        public AudioController(AudioFormat format, INetworkStream transport, Func<AudioFormat, IAudioCodec> codecFactory)
        {
            Format = format;

            _codecFactory = codecFactory;

            _transport = transport;
            _transport.Received += OnTransportReceived;

            _routes = new Dictionary<int, AudioRoute>();
            _consumers = new List<IAudioConsumer>();
        }

        ~AudioController()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public void Registration(Func<AudioFormat, IAudioConsumer> makeConsumer)
        {
            _consumers.Add(makeConsumer(Format));
        }

        public bool TryGet(int routeId, out IWaveStream route)
        {
            route = null;

            if (!_routes.ContainsKey(routeId))
            {
                return false;
            }

            route = _routes[routeId];
            return true;
        }

        public void Append(int routeId) 
        {
            if (_routes.ContainsKey(routeId))
            {
                return;
            }

            var codec = _codecFactory(Format);
            var route = new AudioRoute(codec, this, MakeJitter) 
            { 
                Id = routeId 
            };

            _routes[routeId] = route;

            foreach (IAudioConsumer consumer in _consumers)
            {
                consumer.Append(route);
            }
        }

        public void Remove(int routeId)
        {
            if (!_routes.Remove(routeId, out var route))
            {
                return;
            }

            try
            {
                foreach (IAudioConsumer consumer in _consumers)
                {
                    consumer.Remove(route);
                }
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private IJitterProvider MakeJitter(IAudioCodec codec)
        {
            return new JitterBufferProvider(codec);
        }

        private void OnTransportReceived(byte[] bytes, ref int offset, int count)
        {
            var packet = new AudioPacket();

            while (packet.TryUnpack(bytes, ref offset, count))
            {
                if (!_routes.TryGetValue(packet.RouteId, out AudioRoute route))
                {
                    continue;
                }

                route.Handle(packet);
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
                _transport.Received -= OnTransportReceived;

                for (int i = _routes.Count - 1; i >= 0; i--)
                {
                    Remove(_routes[i].Id);
                }

                _consumers.Clear();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
