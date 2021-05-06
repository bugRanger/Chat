namespace Chat.Audio
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public class Worker<T> : IDisposable
    {
        #region Fields

        private readonly ConcurrentQueue<T> _queue;
        private readonly Action<T> _action;

        private CancellationTokenSource _cancellation;
        private EventWaitHandle _waiter;
        private EventWaitHandle _worker;
        private Thread _thread;

        private bool _disposed;

        #endregion Fields

        #region Constructors

        public Worker(Action<T> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _queue = new ConcurrentQueue<T>();

            _cancellation = new CancellationTokenSource();
            _waiter = new EventWaitHandle(false, EventResetMode.AutoReset);
            _worker = new EventWaitHandle(false, EventResetMode.AutoReset);
            _thread = new Thread(HandleAction);
            _thread.Start();
        }

        #endregion Constructors

        #region Methods

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
            _worker.Set();
        }

        public void WaitSync()
        {
            _waiter.WaitOne();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

                _worker.Dispose();
                _worker = null;

                _waiter.Dispose();
                _waiter = null;

                _cancellation.Dispose();
                _cancellation = null;
            }

            _disposed = true;
        }

        private void HandleAction()
        {
            try
            {
                while (WaitHandle.WaitAny(new[] { _cancellation.Token.WaitHandle, _worker }) > 0)
                {
                    while (_queue.TryDequeue(out var item))
                    {
                        _action.Invoke(item);
                    }

                    _waiter.Set();
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
        }

        #endregion Methods
    }
}
