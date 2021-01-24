namespace Chat.Media
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class AudioBuffer : IAudioReceiver, IDisposable
    {
        #region Constants

        private const int WAIT_MS = 10;
        private const int BUFFER_LIMIT = 50;

        #endregion Constants

        #region Fields

        private readonly TimeSpan _delta;
        private readonly Thread _reader;
        private readonly Dictionary<uint, IAudioPacket> _buffering;
        private DateTime _lastTime;
        private uint _lastIndex;
        private bool _closing;

        #endregion Fields

        #region Events

        public event Action<ArraySegment<byte>> Received;

        #endregion Events

        #region Constructors

        public AudioBuffer(TimeSpan delta)
        {
            _delta = delta;
            _lastIndex = uint.MinValue;
            _closing = false;
            _buffering = new Dictionary<uint, IAudioPacket>();
            _reader = new Thread(() => Dequeue());
            _reader.Start();
        }

        #endregion Constructors

        #region Methods

        public void Enqueue(IAudioPacket packet)
        {
            if (packet.SequenceId <= _lastIndex)
                return;

            _buffering[GetIndex(packet.SequenceId)] = packet;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dequeue()
        {
            while (!_closing)
            {
                var currentTime = DateTime.Now;
                if (!_buffering.TryGetValue(GetIndex(_lastIndex + 1), out IAudioPacket packet) && currentTime - _lastTime <= _delta)
                {
                    Thread.Sleep(WAIT_MS);
                    continue;
                }

                do
                {
                    _lastTime = currentTime;
                    _lastIndex = GetIndex(_lastIndex + 1);

                    if (packet == null)
                    {
                        break;
                    }

                    _buffering[GetIndex(packet.SequenceId)] = null;
                    Received?.Invoke(packet.Payload);
                }
                while (_buffering.TryGetValue(GetIndex(_lastIndex + 1), out packet));
            }
        }

        private uint GetIndex(uint index)
        {
            return index % BUFFER_LIMIT;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _closing = true;
            _reader.Join();

            _buffering.Clear();
        }

        #endregion Methods
    }
}
