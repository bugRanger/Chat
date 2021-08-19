namespace Chat.Server.Audio
{
    using System;
    using System.Net;

    using Chat.Audio;

    public class RouteTransport : IAudioTransport
    {
        #region Fields

        private readonly IPEndPoint _remote;
        private readonly IAudioProvider _provider;

        #endregion Fields

        #region Constructors

        public RouteTransport(IPEndPoint remote, IAudioProvider provider) 
        {
            _remote = remote;
            _provider = provider;
        }

        #endregion Constructors

        #region Methods

        public void Send(IAudioPacket packet)
        {
            _provider.SendTo(_remote, packet);
        }

        #endregion Methods
    }
}
