namespace Chat.Server
{
    using System;

    using Chat.Api;

    public interface ICoreApi
    {
        void Handle(IConnection client, IMessage message);
    }
}