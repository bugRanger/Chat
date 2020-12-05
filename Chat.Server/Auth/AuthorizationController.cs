namespace Chat.Server.Auth
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    public class AuthorizationController : IAuthorizationController
    {
        #region Fields

        private readonly object _locker;
        private readonly Dictionary<IPEndPoint, IUser> _remoteToUser;
        private readonly Dictionary<string, IUser> _nameToUser;

        #endregion Fields

        #region Constructors

        public AuthorizationController() 
        {
            _locker = new object();
            _remoteToUser = new Dictionary<IPEndPoint, IUser>();
            _nameToUser = new Dictionary<string, IUser>();
        }

        #endregion Constructors

        #region Methods

        public void AddOrUpdate(IPEndPoint remote, Action<User> update)
        {
            lock (_locker)
            {
                if (!_remoteToUser.TryGetValue(remote, out IUser user))
                {
                    user = new User();
                }
                else
                {
                    _remoteToUser.Remove(user.Remote);
                    _nameToUser.Remove(user.Name);
                }

                update((User)user);

                _nameToUser[user.Name] = user;
                _remoteToUser[remote] = user;
            }
        }

        public bool TryRemove(IPEndPoint remote, out IUser user)
        {
            lock (_locker)
            {
                if (!_remoteToUser.TryGetValue(remote, out user))
                {
                    return false;
                }

                _nameToUser.Remove(user.Name);
                _remoteToUser.Remove(remote);

                return true;
            }
        }

        public bool TryGet(string userName, out IUser user)
        {
            return _nameToUser.TryGetValue(userName, out user);
        }

        public bool TryGet(IPEndPoint endPoint, out IUser user)
        {
            return _remoteToUser.TryGetValue(endPoint, out user);
        }

        public IUser[] GetUsers(Func<IUser, bool> prepare = null)
        {
            return _nameToUser.Values.Where(s => prepare?.Invoke(s) ?? true).ToArray();
        }

        #endregion Methods
    }
}
