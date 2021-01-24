namespace Chat.Client.Call
{
    using System;

    using Chat.Media;

    class CallSession : IAudioSender, IDisposable
    {
        #region Constants

        private const int BUFFERING_MS = 150;

        #endregion Constants

        #region Fields

        private readonly AudioBuffer _buffer;
        private readonly AudioPlayer _player;
        private readonly AudioCapture _capture;

        private uint _sequenceId;
        private bool _disposing;

        #endregion Fields

        #region Properties

        public int Id { get; }

        public int RouteId { get; }

        #endregion Properties

        #region Events

        public event Action<IAudioPacket> Prepared;

        #endregion Events

        #region Constructors

        public CallSession(int id, int routeId, IAudioCodec codec)
        {
            _buffer = new AudioBuffer(TimeSpan.FromMilliseconds(BUFFERING_MS));
            _player = new AudioPlayer(codec, _buffer);
            _capture = new AudioCapture(codec, this);
            _sequenceId = 0;

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
            if (packet.SessionId != Id)
                return;

            _buffer.Enqueue(packet);
        }

        public void Send(ArraySegment<byte> bytes)
        {
            var packet = new AudioPacket
            {
                SessionId = Id,
                RouteId = RouteId,
                Payload = bytes,
                SequenceId = ++_sequenceId,
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
                return;

            _disposing = true;

            _capture.Dispose();
            _player.Dispose();
            _buffer.Dispose();
        }

        #endregion Methods
    }
}
