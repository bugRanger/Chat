namespace Chat.Server.Audio
{
    using System;
    using System.Net;

    public interface IAudioRouter : IDisposable
    {
        #region Properties

        int Count { get; }

        #endregion Properties

        #region Methods

        int Append(IPEndPoint route);

        void Remove(IPEndPoint route);

        bool TryGet(IPEndPoint route, out int routeId);

        #endregion Methods
    }
}
