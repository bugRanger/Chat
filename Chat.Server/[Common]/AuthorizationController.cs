namespace Chat.Server
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    public class AuthorizationController : IAuthorizationController
    {
        #region Fields

        private readonly object _locker;
        private readonly Dictionary<IPEndPoint, IUser> _remoteToUser;

        #endregion Fields

        #region Constructors

        public AuthorizationController() 
        {
            _locker = new object();
            _remoteToUser = new Dictionary<IPEndPoint, IUser>();
        }

        #endregion Constructors

        #region Methods

        public bool TryAddOrUpdate(IPEndPoint remote)
        {
            throw new NotImplementedException();
        }

        public bool TryRemove(string user, IPEndPoint remote)
        {
            throw new NotImplementedException();
        }

        public bool TryGet(string user)
        {
            throw new NotImplementedException();
        }

        public bool TryGet(IPEndPoint endPoint)
        {
            throw new NotImplementedException();
        }

        #endregion Methods
    }
}
