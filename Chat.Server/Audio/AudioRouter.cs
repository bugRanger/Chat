namespace Chat.Server.Call
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    public class AudioRouter : IAudioRouter
    {
        #region Fields

        private readonly object _locker;
        private readonly KeyContainer _container;
        private readonly INetworkСontroller _network;
        private readonly Dictionary<int, IPEndPoint> _routes;

        #endregion Fields

        #region Properties

        public int Count => _routes.Count;

        #endregion Properties

        #region Constructors

        public AudioRouter(KeyContainer container, INetworkСontroller network) 
        {
            _locker = new object();
            _routes = new Dictionary<int, IPEndPoint>();

            _container = container;
            _network = network;
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

        public void Handle(int routeId, byte[] bytes) 
        {
            if (!_routes.ContainsKey(routeId))
            {
                return;
            }

            foreach (var route in _routes.ToArray())
            {
                if (route.Key == routeId)
                {
                    continue;
                }

                _network.Send(route.Value, bytes);
            }
        }

        #endregion Methods
    }
}
