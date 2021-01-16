namespace Chat.Client.Call
{
    using System;

    using Chat.Media;

    class CallSession : IAudioReceiver, IAudioSender
    {
        #region Fields

        private readonly AudioPlayer _player;
        private readonly AudioCapture _capture;

        private bool _disposing;

        #endregion Fields

        #region Properties

        public int Id { get; }

        public int RouteId { get; }

        #endregion Properties

        #region Events

        public event Action<IAudioPacket> Prepared;
        public event Action<ArraySegment<byte>> Received;

        #endregion Events

        #region Constructors

        public CallSession(int id, int routeId, IAudioCodec codec)
        {
            _player = new AudioPlayer(codec, this);
            _capture = new AudioCapture(codec, this);

            Id = id;
            RouteId = routeId;
        }

        ~CallSession() 
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public void Handle(IAudioPacket packet) 
        {
            if (packet.RouteId != RouteId)
                return;

            Received?.Invoke(packet.Payload);
        }

        public void Send(ArraySegment<byte> bytes)
        {
            var packet = new AudioPacket
            {
                RouteId = RouteId,
                Payload = bytes,
            };

            Prepared?.Invoke(packet);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) 
        {
            if (_disposing)
            {
                return;
            }

            _capture.Dispose();
            _player.Dispose();
            _disposing = true;
        }

        #endregion Methods
    }
}
