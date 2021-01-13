namespace Chat.Server.Audio
{
    using System;
    using System.Net;

    using Chat.Media;

    public class AudioProvider : IAudioProvider
    {
        #region Fields

        private readonly INetworkСontroller _network;

        #endregion Fields

        #region Events

        public event Action<IAudioPacket> Received;

        #endregion Events

        #region Constructors

        public AudioProvider(INetworkСontroller network) 
        {
            _network = network;
            _network.PreparePacket += OnNetworkPreparePacket;
        }

        private void OnNetworkPreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count)
        {
            var packet = new AudioPacket();
            if (!packet.TryUnpack(bytes, ref offset, count))
            {
                return;
            }

            Received?.Invoke(packet);
        }

        #endregion Constructors

        #region Methods

        public void Send(IPEndPoint target, IAudioPacket packet)
        {
            _network.Send(target, packet.Pack());
        }

        #endregion Methods
    }
}
