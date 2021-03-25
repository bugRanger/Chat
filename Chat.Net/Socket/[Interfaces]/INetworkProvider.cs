namespace Chat.Net.Socket
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    public interface INetworkProvider
    {
        #region Methods

        void Start(IPEndPoint endPoint);

        Task StartAsync(IPEndPoint endPoint);

        void Stop();

        #endregion Methods
    }
}