namespace Chat.Server
{
    using System;
    using System.Net;

    public interface IAuthorizationController
    {
        bool TryAddOrUpdate(string userId, IPEndPoint remote);

        bool TryRemove(IPEndPoint remote, out IUser user);

        bool TryGet(string userId, out IUser user);

        bool TryGet(IPEndPoint endPoint, out IUser user);

        IUser[] GetUsers();
    }
}
