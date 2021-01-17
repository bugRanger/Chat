namespace Chat.Server.Auth
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    public class AuthorizationController : IAuthorizationController
    {
        #region Fields

        private readonly object _locker;
        private readonly Dictionary<IPEndPoint, User> _remoteToUser;
        private readonly Dictionary<string, User> _nameToUser;

        #endregion Fields

        #region Events

        public event Action<IUser> Disconnected;

        #endregion Events

        #region Constructors

        public AuthorizationController() 
        {
            _locker = new object();
            _remoteToUser = new Dictionary<IPEndPoint, User>();
            _nameToUser = new Dictionary<string, User>();
        }

        #endregion Constructors

        #region Methods

        public IUser AddOrUpdate(IPEndPoint remote, string name)
        {
            lock (_locker)
            {
                if (remote == null || string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }

                if (!_remoteToUser.TryGetValue(remote, out User user))
                {
                    user = new User();
                }
                else
                {
                    _remoteToUser.Remove(user.Remote);
                    _nameToUser.Remove(user.Name);
                }

                user.Remote = remote;
                user.Name = name;

                _remoteToUser[user.Remote] = user;
                _nameToUser[user.Name] = user;

                return user;
            }
        }

        public bool TryRemove(IPEndPoint remote, out IUser user)
        {
            lock (_locker)
            {
                user = null;

                if (!_remoteToUser.TryGetValue(remote, out User match))
                {
                    return false;
                }

                user = match;

                _remoteToUser.Remove(user.Remote);
                _nameToUser.Remove(user.Name);

                Disconnected?.Invoke(user);

                return true;
            }
        }

        public bool TryGet(string userName, out IUser user)
        {
            user = null;

            if (!_nameToUser.TryGetValue(userName, out var match))
            {
                return false;
            }

            user = match;
            return true;
        }

        public bool TryGet(IPEndPoint remote, out IUser user)
        {
            user = null;

            if (!_remoteToUser.TryGetValue(remote, out var match))
            {
                return false;
            }

            user = match;
            return true;
        }

        public IEnumerable<IUser> GetUsers()
        {
            lock (_locker)
            {
                return new List<IUser>(_nameToUser.Values);
            }
        }

        #endregion Methods
    }
}
