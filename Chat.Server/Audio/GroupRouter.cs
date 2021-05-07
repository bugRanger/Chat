namespace Chat.Server.Audio
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using Chat.Audio;
    using Chat.Audio.Mixer;
    using Chat.Audio.Codecs;

    public class GroupRouter : IAudioRouter
    {
        #region Fields

        private readonly object _locker;
        private readonly IAudioProvider _provider;
        private readonly Dictionary<IPEndPoint, AudioRoute> _routes;
        private readonly IAudioCodec _codec;
        private readonly AudioMixer _mixer;

        private bool _disposed;

        #endregion Fields

        #region Properties

        public int Count => _routes.Count;

        #endregion Properties

        #region Constructors

        public GroupRouter(IAudioProvider provider, IAudioCodec codec)
        {
            _locker = new object();
            _routes = new Dictionary<IPEndPoint, AudioRoute>();

            _codec = codec;
            _mixer = new AudioMixer(_codec.Format);

            _provider = provider;
            _provider.Received += OnProviderReceived;
        }

        #endregion Constructors

        #region Methods

        public void Append(IPEndPoint route, int routeId)
        {
            lock (_locker)
            {
                if (_routes.ContainsKey(route))
                    return;

                _routes[route] = new AudioRoute(_codec, MakeTransport(route), MakeJitter)
                {
                    Id = routeId,
                };
                _mixer.Append(_routes[route].AsSampleStream());
            }
        }

        public void Remove(IPEndPoint route)
        {
            lock (_locker)
            {
                if (!_routes.Remove(route, out var stream))
                    return;

                _mixer.Remove(_routes[route].AsSampleStream());
            }
        }

        public bool Contains(IPEndPoint route)
        {
            return _routes.ContainsKey(route);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private IAudioTransport MakeTransport(IPEndPoint route)
        {
            return new RouteTransport(route, _provider);
        }

        private IJitterProvider MakeJitter(IAudioCodec codec)
        {
            return new JitterQueueProvider(codec);
        }

        private void OnProviderReceived(IPEndPoint remote, IAudioPacket packet)
        {
            if (!_routes.TryGetValue(remote, out var stream))
                return;

            stream.Handle(packet);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _provider.Received -= OnProviderReceived;
                _routes.Clear();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
