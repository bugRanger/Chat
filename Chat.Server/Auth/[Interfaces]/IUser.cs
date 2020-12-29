namespace Chat.Server.Auth
{
    using System;
    using System.Net;

    public interface IUser
    {
        IPEndPoint Remote { get; }

        string Name { get; }
    }
}
