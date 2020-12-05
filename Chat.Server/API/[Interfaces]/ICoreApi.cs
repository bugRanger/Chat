namespace Chat.Server.API
{
    using System;
    using System.Net;

    using Chat.Api;

    public interface ICoreApi
    {
        #region Methods

        void Send(IMessage message, params IPEndPoint[] remotes);

        void Send(IMessage message, IPEndPoint remote, int index);

        void Disconnect(IPEndPoint remote);

        void Registration<T>(Action<IPEndPoint, int, T> action) where T : IMessage;

        #endregion Methods
    }
}