namespace Chat.Server.Audio
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    using Chat.Media;
    using Chat.Server.Call;

    public class BridgeRouter : IAudioRouter
    {
        #region Fields

        private readonly object _locker;
        private readonly KeyContainer _container;
        private readonly IAudioProvider _provider;
        private readonly Dictionary<int, IPEndPoint> _routes;

        #endregion Fields

        #region Properties

        public IPEndPoint this[int index] => _routes[index];

        public int Count => _routes.Count;

        #endregion Properties

        #region Constructors

        public BridgeRouter(KeyContainer container, IAudioProvider provider) 
        {
            _locker = new object();
            _routes = new Dictionary<int, IPEndPoint>();

            _container = container;
            _provider = provider;
            _provider.Received += OnProviderReceived;
        }

        #endregion Constructors

        #region Methods

        public int AddRoute(IPEndPoint remote)
        {
            lock (_locker)
            {
                var routeId = _container.Take();
                _routes[routeId] = remote;

                return routeId;
            }
        }

        public void DelRoute(int routeId)
        {
            lock (_locker)
            {
                if (!_routes.Remove(routeId, out _))
                {
                    return;
                }
                
                _container.Release(routeId);
            }
        }

        public void Dispose()
        {
            _provider.Received -= OnProviderReceived;
        }

        private void OnProviderReceived(IAudioPacket packet) 
        {
            if (!_routes.ContainsKey(packet.RouteId))
            {
                return;
            }

            foreach (var route in _routes.ToArray())
            {
                if (route.Key == packet.RouteId)
                {
                    continue;
                }
                
                var repack = new AudioPacket
                {
                    RouteId = route.Key,
                    Timestamp = packet.Timestamp,
                    Payload = packet.Payload,
                };

                _provider.Send(route.Value, repack);
            }
        }

        #endregion Methods
    }
}
