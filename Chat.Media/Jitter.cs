namespace Chat.Audio
{
    using System;

    public class Jitter
    {
        #region Constants

        private const uint JITTER_MAX_DURATION = 120;

        #endregion Constants

        #region Fields

        private readonly object _locker;

        private readonly IAudioPacket[] _packets;
        private readonly uint _limit;

        private uint _indexPull;
        private uint _indexPush;
        private uint _reordered;
        private uint _losses;

        #endregion Fields

        #region Constructors

        public Jitter(IAudioCodec codec)
        {
            _locker = new object();

            _limit = JITTER_MAX_DURATION / codec.Format.Duration;
            _packets = new IAudioPacket[_limit];

            _indexPull = int.MaxValue;
            _indexPush = 0;
            _reordered = 0;
            _losses = 0;
        }

        #endregion Constructors

        #region Methods

        public void Push(IAudioPacket packet)
        {
            lock (_locker)
            {
                var index = packet.SequenceId % _limit;
                var current = _packets[index];

                if (current != null)
                    return;

                if (packet.Mark)
                    _indexPull = packet.SequenceId;

                bool ordered = packet.SequenceId == _indexPush + 1;
                if (ordered)
                    _reordered = 0;
                else
                    _reordered += 1;

                _packets[index] = packet;
                _indexPush = packet.SequenceId;
            }
        }

        public IAudioPacket Pull(bool hungry = false)
        {
            lock (_locker)
            {
                if (hungry)
                    _losses += 1;

                if (hungry && _losses != 0 && _losses <= _limit)
                    return null;

                if (!hungry && _reordered != 0 && _reordered < _limit)
                    return null;

                var index = _indexPull % _limit;
                var packet = _packets[index];

                if (packet == null)
                    if (!hungry)
                        return null;
                    else
                        return new AudioPacket
                        {
                            SequenceId = _indexPull,
                            Payload = null,
                        };

                _packets[index] = null;
                _indexPull += 1;

                if (!hungry && packet != null)
                    _losses = 0;

               return packet;
            }
        }

        public IAudioPacket Peek()
        {
            lock (_locker)
            {
                return _packets[_indexPull % _limit];
            }
        }

        public void Clear()
        {
            lock (_locker)
            {
                for (int i = 0; i < _limit; i++)
                {
                    _packets[i] = null;
                }

                _indexPull = int.MaxValue;
                _indexPush = 0;
                _reordered = 0;
                _losses = 0;
            }
        }

        #endregion Methods
    }
}
