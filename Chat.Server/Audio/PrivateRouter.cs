namespace Chat.Server.Audio
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    using Chat.Audio;

    public class PrivateRouter : IAudioRouter
    {
        #region Fields

        private readonly object _locker;
        private readonly IAudioProvider _provider;
        private readonly HashSet<IPEndPoint> _routes;

        private bool _disposed;

        #endregion Fields

        #region Properties

        public int Count => _routes.Count;

        #endregion Properties

        #region Constructors

        public PrivateRouter(IAudioProvider provider) 
        {
            _locker = new object();
            _routes = new HashSet<IPEndPoint>();

            _provider = provider;
            _provider.Received += OnProviderReceived;
        }

        #endregion Constructors

        #region Methods

        public void Append(IPEndPoint route, int routeId)
        {
            lock (_locker)
            {
                _routes.Add(route);
            }
        }

        public void Remove(IPEndPoint route)
        {
            lock (_locker)
            {
                _routes.Remove(route);
            }
        }

        public bool Contains(IPEndPoint route)
        {
            return _routes.Contains(route);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        private void OnProviderReceived(IPEndPoint remote, IAudioPacket packet) 
        {
            if (remote == null || !_routes.Contains(remote))
            {
                return;
            }

            foreach (var route in _routes.ToArray())
            {
                if (remote.Equals(route))
                {
                    continue;
                }

                _provider.SendTo(route, packet);
            }
        }

        #endregion Methods
    }
}
