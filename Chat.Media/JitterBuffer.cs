namespace Chat.Audio
{
    using System;

    public enum JitterResult
    {
        Nothing,
        Restore,
        Current,
    }

    public class JitterBuffer<T> : where T : struct
    {
        #region Fields

        private readonly object _locker;
        private readonly T?[] _buffer;

        private int _pushIndex;
        private int _pullIndex;

        #endregion Fields

        #region Events

        public event Action<T> Pulled;

        #endregion Events

        #region Constructors

        public JitterBuffer(int step)
        {
            _locker = new object();
            _buffer = new T?[step];

            _pushIndex = 0;
            _pullIndex = 0;
        }

        #endregion Constructors

        #region Methods

        public void Push(int index, T data)
        {
            lock (_locker)
            {
                if (index < _pullIndex)
                    return;

                _pushIndex = index;
                _buffer[index] = data;

                // TODO Джиттер должен иметь "буфер" данных для выравнивание от потерь.

                Pull(index);
            }
        }

        public void WaitSync()
        {
            //_queue.WaitSync();
        }

        public void Await()
        {
            lock (_locker)
            {
                Pull(_pullIndex + 1);
            }
        }

        private void Pull(int index) 
        {
            lock (_locker)
            {
                var pullIndex = _pullIndex + 1;
                var pullData = _buffer[pullIndex];

                if (pullIndex != index && !pullData.HasValue)
                    return;

                //_queue.Enqueue(pullData.Value);

                _pullIndex = pullIndex;
                _buffer[pullIndex] = null;
            }
        }

        #endregion Methods
    }
}
