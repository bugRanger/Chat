namespace Chat.Server
{
    using System;
    using System.Net;

    public interface IUserContainer
    {
        #region Methods

        bool TryGet(string userName, out IUser user);

        bool TryGet(IPEndPoint endPoint, out IUser user);

        IUser[] GetUsers();

        #endregion Methods
    }
}
