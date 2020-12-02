namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Api;

    public interface IApiController
    {
        void Handle(IPEndPoint remote, IMessage message);
    }
}