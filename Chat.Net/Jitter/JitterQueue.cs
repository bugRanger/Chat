namespace Chat.Net.Jitter
{
    using System;

    public class JitterQueue<T> 
        where T : IPacket
    {
        #region Fields

        private readonly object _locker;

        private readonly uint _limit;
        private readonly T[] _packets;

        private uint _indexPull;
        private uint _indexPush;
        private uint _reordered;
        private uint _losses;

        #endregion Fields

        #region Constructors

        public JitterQueue(uint limit)
        {
            _locker = new object();

            _packets = new T[limit];
            _limit = limit;

            _indexPull = int.MaxValue;
            _indexPush = 0;
            _reordered = 0;
            _losses = 0;
        }

        #endregion Constructors

        #region Methods

        public void Push(T packet)
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

        public uint? Pull(bool hungry, out T packet)
        {
            packet = default;

            lock (_locker)
            {
                if (hungry)
                {
                    _losses += 1;
                }

                if (hungry && _losses != 0 && _losses <= _limit)
                {
                    return null;
                }

                if (!hungry && _reordered != 0 && _reordered < _limit)
                {
                    return null;
                }

                var index = _indexPull;
                packet = _packets[index % _limit];

                if (packet == null)
                {
                    if (hungry)
                    {
                        return index;
                    }

                    return null;
                }

                _packets[index % _limit] = default;
                _indexPull += 1;

                if (!hungry && packet != null)
                {
                    _losses = 0;
                }

               return index;
            }
        }

        public T Peek()
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
                    _packets[i] = default;
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
