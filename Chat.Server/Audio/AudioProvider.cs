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

        public event PackedReceived Received;

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

            Received?.Invoke(packet.RouteId, packet.Payload);
        }

        #endregion Constructors

        #region Methods

        public void Send(IPEndPoint target, int routeId, ArraySegment<byte> bytes)
        {
            var packet = new AudioPacket()
            {
                RouteId = routeId,
                Payload = bytes,
            }
            .Pack();

            _network.Send(target, packet);
        }

        #endregion Methods
    }
}
