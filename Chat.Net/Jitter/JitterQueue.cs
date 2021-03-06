﻿namespace Chat.Net.Jitter
{
    using System;

    public class JitterQueue<T> 
        where T : IPacket
    {
        #region Fields

        private readonly object _locker;

        private readonly uint _limit;
        private readonly uint _dropped;
        private readonly T[] _packets;

        private uint? _indexLast;
        private uint? _indexPull;
        private int? _indexPush;
        private int _losses;
        private int _satiety;
        private bool _marker;

        #endregion Fields

        #region Constructors

        public JitterQueue(uint limit)
        {
            _locker = new object();

            _packets = new T[limit];
            _limit = limit;
            _dropped = limit * 2;
        }

        #endregion Constructors

        #region Methods

        public void Push(T packet)
        {
            lock (_locker)
            {
                int delta = 0;

                if (_indexPush.HasValue)
                {
                    delta = (int)(packet.SequenceId - _indexPush);

                    if (Math.Abs(delta) >= _limit)
                    {
                        if (delta < 0 && -delta < _dropped)
                        {
                            return;
                        }

                        Clear();
                        delta = 0;
                    }
                }

                if (!_indexPush.HasValue)
                {
                    _indexPush = (int)packet.SequenceId;
                }

                if (!_indexLast.HasValue || _indexLast.Value > packet.SequenceId)
                {
                    _indexLast = packet.SequenceId;
                }

                if (!packet.Mark && !_marker) 
                {
                    _satiety++;
                }
                
                if (packet.Mark || !_marker && _satiety + 1 >= _limit)
                {
                    _indexPull = _indexLast;
                    _marker = true;
                    _satiety = 0;
                }

                _packets[(_indexPush.Value + delta) % _limit] = packet;

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
                if (!_indexPull.HasValue)
                    return null;

                if (hungry)
                    _losses++;

                if (hungry && _losses != 0 && _losses < _limit)
                    return null;

                var index = _indexPull.Value % _limit;

                packet = _packets[index];
                if (packet == null || packet.SequenceId != _indexPull.Value)
                    packet = default;

                if (!hungry && packet == null)
                    return null;

                try
                {
                    _packets[index] = default;

                    if (!hungry && packet != null)
                        _losses = 0;

                    return _indexPull;
                }
                finally
                {
                    _indexPull++;
                }
            }
        }

        public T Peek()
        {
            lock (_locker)
            {
                return _indexPull.HasValue 
                    ? _packets[_indexPull.Value % _limit] 
                    : default;
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

                _indexLast = null;
                _indexPull = null;
                _indexPush = null;
                _satiety = 0;
                _losses = 0;
                _marker = false;
            }
        }

        #endregion Methods
    }
}
