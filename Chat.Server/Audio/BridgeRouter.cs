namespace Chat.Server.Audio
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    using Chat.Audio;
    using Chat.Server.Call;

    public class BridgeRouter : IAudioRouter
    {
        #region Fields

        private readonly object _locker;
        private readonly KeyContainer _container;
        private readonly IAudioProvider _provider;
        private readonly Dictionary<IPEndPoint, int> _routes;

        #endregion Fields

        #region Properties

        public int Count => _routes.Count;

        #endregion Properties

        #region Constructors

        public BridgeRouter(KeyContainer container, IAudioProvider provider) 
        {
            _locker = new object();
            _routes = new Dictionary<IPEndPoint, int>();

            _container = container;
            _provider = provider;
            _provider.Received += OnProviderReceived;
        }

        #endregion Constructors

        #region Methods

        public int Append(IPEndPoint route)
        {
            lock (_locker)
            {
                if (!_routes.TryGetValue(route, out int routeId))
                {
                    routeId = _container.Take();
                    _routes[route] = routeId;
                }

                return routeId;
            }
        }

        public void Remove(IPEndPoint route)
        {
            lock (_locker)
            {
                if (!_routes.Remove(route, out int routeId))
                {
                    return;
                }
                
                _container.Release(routeId);
            }
        }

        public bool TryGet(IPEndPoint route, out int routeId)
        {
            return _routes.TryGetValue(route, out routeId);
        }

        public void Dispose()
        {
            _provider.Received -= OnProviderReceived;

            var routes = _routes.Values.ToArray();
            _routes.Clear();

            foreach (var routeId in routes)
            {
                _container.Release(routeId);
            }
        }

        private void OnProviderReceived(IPEndPoint remote, IAudioPacket packet) 
        {
            if (remote == null || !_routes.TryGetValue(remote, out int routeId) || packet.RouteId != routeId)
            {
                return;
            }

            foreach (var route in _routes.ToArray())
            {
                if (route.Value == packet.RouteId)
                {
                    continue;
                }

                _provider.Send(route.Key, packet);
            }
        }

        #endregion Methods
    }
}
