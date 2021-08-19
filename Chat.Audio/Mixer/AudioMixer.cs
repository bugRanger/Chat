namespace Chat.Audio.Mixer
{
    using System;
    using System.Threading;
    using System.Collections.Generic;

    public class AudioMixer : IDisposable
    {
        #region Fields

        private readonly object _locker;

        private readonly HashSet<ISampleStream> _streams;
        private readonly AudioFormat _format;
        private readonly int _interval;

        private CancellationTokenSource _cancellation;
        private Worker<MixedChunk> _queue;
        private EventWaitHandle _waiter;
        private EventWaitHandle _worker;
        private Thread _thread;

        private bool _disposed;

        #endregion Fields

        #region Constructors

        public AudioMixer(AudioFormat format)
            : this(format, format.Duration)
        {
        }

        public AudioMixer(AudioFormat format, int interval)
        {
            _locker = new object();
            _format = format;
            _interval = interval;

            _streams = new HashSet<ISampleStream>();

            _cancellation = new CancellationTokenSource();
            _queue = new Worker<MixedChunk>(chunk => chunk.Unpack());
            _waiter = new EventWaitHandle(false, EventResetMode.AutoReset);
            _worker = new EventWaitHandle(false, EventResetMode.AutoReset);
            _thread = new Thread(Handle);
            _thread.Start();
        }

        #endregion Constructors

        #region Methods

        public void Append(ISampleStream stream)
        {
            lock (_locker)
            {
                _streams.Add(stream);
            }
        }

        public void Remove(ISampleStream stream)
        {
            lock (_locker)
            {
                _streams.Remove(stream);
            }
        }

        public void WaitSync()
        {
            _waiter.Set();
            _worker.WaitOne();
            _queue.WaitSync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Handle()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                _waiter.WaitOne(_interval);

                lock (_locker)
                {
                    var chunk = new MixedChunk(_format);
                    chunk.Pack(_streams);

                    _queue.Enqueue(chunk);
                }

                _worker.Set();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _cancellation.Cancel();

                _thread.Join();
                _thread = null;

                _waiter.Dispose();
                _waiter = null;

                _worker.Dispose();
                _worker = null;

                _queue.Dispose();
                _queue = null;

                _cancellation.Dispose();
                _cancellation = null;

                _streams.Clear();
            }

            _disposed = true;
        }

        #endregion Methods
    }

}