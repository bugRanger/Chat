namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Server.Auth;

    public interface IAuthorizationController
    {
        #region Methods

        void AddOrUpdate(IPEndPoint remote, Action<User> update);

        bool TryRemove(IPEndPoint remote, out IUser user);

        bool TryGet(string userName, out IUser user);

        bool TryGet(IPEndPoint endPoint, out IUser user);

        IUser[] GetUsers();

        #endregion Methods
    }
}
