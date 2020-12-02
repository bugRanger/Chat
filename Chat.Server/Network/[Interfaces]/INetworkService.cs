namespace Chat.Server
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    public interface INetworkService
    {
        #region Methods

        void Start(IPEndPoint endPoint, int limit = 1000);

        Task StartAsync(IPEndPoint endPoint, int limit = 1000);

        void Stop();

        #endregion Methods
    }
}