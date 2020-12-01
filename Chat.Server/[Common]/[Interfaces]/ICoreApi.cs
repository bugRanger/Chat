namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Api;

    public interface ICoreApi
    {
        void Handle(IPEndPoint remote, IMessage message);
    }
}