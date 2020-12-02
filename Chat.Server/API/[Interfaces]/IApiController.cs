namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Api;

    public interface IApiController
    {
        void Send(IMessage message, params IPEndPoint[] remotes);

        void Registration<T>(params Action<IPEndPoint, T>[] actions) where T : IMessage;
    }
}