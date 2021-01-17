namespace Chat.Server.Auth
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    public interface IUserContainer
    {
        #region Events

        event Action<IUser> Disconnected;

        #endregion Events

        #region Methods

        bool TryGet(string userName, out IUser user);

        bool TryGet(IPEndPoint endPoint, out IUser user);

        IEnumerable<IUser> GetUsers();

        #endregion Methods
    }
}
