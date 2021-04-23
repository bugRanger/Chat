namespace Chat.Net.Jitter
{
    using System;
    using System.Threading;

    public class JitterTimer<TPacket> : IDisposable
        where TPacket : IPacket
    {
        #region Constants

        public const uint JITTER_MAX_DURATION = 120;

        #endregion Constants

        #region Fields

        private readonly ushort _duration;
        private readonly JitterQueue<TPacket> _jitter;

        private CancellationTokenSource _cancellation;
        private Thread _thread;

        private bool _disposed;

        #endregion Fields

        #region Events

        public event Action<TPacket, bool> Completed;

        #endregion Events

        #region Constructors

        public JitterTimer(ushort duration)
        {
            _duration = duration;

            _jitter = new JitterQueue<TPacket>(JITTER_MAX_DURATION / duration);
            _cancellation = new CancellationTokenSource();
            _thread = new Thread(CheckHungry);
            _thread.Start();
        }

        #endregion Constructors

        #region Methods

        public void Append(TPacket packet)
        {
            _jitter.Push(packet);
            Handle(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CheckHungry()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                Thread.Sleep(_duration);

                Handle(true);
            }
        }

        private void Handle(bool hungry)
        {
            TPacket packet;
            do
            {
                uint? index = _jitter.Pull(hungry, out packet);
                if (!index.HasValue)
                {
                    break;
                }

                var recover = packet == null;
                if (recover)
                {
                    packet = _jitter.Peek();
                }

                Completed?.Invoke(packet, recover);
            }
            while (packet != null);
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

                _cancellation.Dispose();
                _cancellation = null;

                _jitter.Clear();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
