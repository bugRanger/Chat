namespace Chat.Server.API
{
    using System;
    using System.Net;

    using Chat.Api;

    public interface ICoreApi
    {
        #region Events

        event Action<IPEndPoint> ConnectionClosing;

        #endregion Events

        #region Methods

        void Send(IMessage message, params IPEndPoint[] remotes);

        void Send(IMessage message, IPEndPoint remote, int index);

        void Disconnect(IPEndPoint remote);

        void Registration(IApiModule module);

        void Registration<T>(Action<IPEndPoint, int, T> action) where T : IMessage;

        #endregion Methods
    }
}