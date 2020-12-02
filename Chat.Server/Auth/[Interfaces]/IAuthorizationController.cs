namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Server.Auth;

    public interface IAuthorizationController : IUserContainer
    {
        #region Methods

        void AddOrUpdate(IPEndPoint remote, Action<User> update);

        bool TryRemove(IPEndPoint remote, out IUser user);

        #endregion Methods
    }
}
