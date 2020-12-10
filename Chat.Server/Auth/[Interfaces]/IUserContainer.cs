namespace Chat.Server
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    public interface IUserContainer
    {
        #region Events

        event Action<IUser> Append;

        #endregion Events

        #region Methods

        bool TryGet(string userName, out IUser user);

        bool TryGet(IPEndPoint endPoint, out IUser user);

        IEnumerable<IUser> GetUsers();

        #endregion Methods
    }
}
