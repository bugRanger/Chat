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

        void Append(IPEndPoint route);

        void Remove(IPEndPoint route);

        bool Contains(IPEndPoint route);

        #endregion Methods
    }
}
