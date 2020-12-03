namespace Chat.Server.API
{
    using System;
    using System.Net;

    using Chat.Api;

    public interface ICoreApi
    {
        #region Methods

        void Send(IMessage message, params IPEndPoint[] remotes);

        void Disconnect(IPEndPoint remote);

        public void Append(IApiModule module);

        void Registration<T>(Action<IPEndPoint, T> action) where T : IMessage;

        #endregion Methods
    }
}