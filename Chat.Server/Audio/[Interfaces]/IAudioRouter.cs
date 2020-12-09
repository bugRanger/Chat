namespace Chat.Server.Call
{
    using System;
    using System.Net;

    public interface IAudioRouter
    {
        #region Properties

        int Count { get; }

        #endregion Properties

        #region Methods

        int AddRoute(IPEndPoint remote);

        void DelRoute(int routeId);

        #endregion Methods
    }
}
