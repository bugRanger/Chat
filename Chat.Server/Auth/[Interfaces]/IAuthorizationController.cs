namespace Chat.Server.Auth
{
    using System;
    using System.Net;

    public interface IAuthorizationController : IUserContainer
    {
        #region Methods

        public IUser AddOrUpdate(IPEndPoint remote, string name);

        bool TryRemove(IPEndPoint remote, out IUser user);

        #endregion Methods
    }
}
