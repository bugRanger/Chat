namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Server.login;

    public interface IAuthorizationController : IUserContainer
    {
        #region Methods

        public IUser AddOrUpdate(IPEndPoint remote, string name);

        bool TryRemove(IPEndPoint remote, out IUser user);

        #endregion Methods
    }
}
