namespace Chat.Server
{
    using System;
    using System.Net;

    public interface IUser
    {
        IPEndPoint Remote { get; }

        string Name { get; }
    }
}
