namespace Chat.Server.Audio
{
    using System;
    using System.Net;

    public interface IAudioRouter : IDisposable
    {
        #region Properties

        IPEndPoint this[int index] { get; }

        int Count { get; }

        #endregion Properties

        #region Methods

        int AddRoute(IPEndPoint remote);

        void DelRoute(int routeId);

        #endregion Methods
    }
}
