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
                var delta = (byte)(_indexPull == uint.MaxValue ? 0 : packet.SequenceId - _indexPush);
                if (delta >= _limit || delta >= -_limit)
                {
                    if (delta < 0 && -delta > _limit)
                    {
                        return;
                    }

                    delta = 0;
                    Clear();
                }

                var index = (_indexPush + delta) % _limit;
                if (packet.Mark)
                    _indexPull = packet.SequenceId;

                _packets[index] = packet;

                if (delta != 0)
                {
                    _reordered += 1;
                }
                else
                {
                    _reordered = 0;
                }

                if (delta > 0)
                {
                    _indexPush += delta;
                }
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
