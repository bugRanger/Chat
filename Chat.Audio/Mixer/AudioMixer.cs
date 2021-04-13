namespace Chat.Audio
{
    using System;
    using System.Threading;
    using System.Collections.Generic;

    using NLog;
    using NAudio.Wave;

    public class AudioMixer : IDisposable
    {
        #region Constants

        internal const int LOCKER_FOR_TESTS = -1;

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;
        private readonly object _locker;

        private readonly HashSet<IAudioStream> _streams;
        private readonly Queue<AudioChunk> _chunks;
        private readonly AudioFormat _format;
        private readonly int _interval;

        private CancellationTokenSource _cancellation;
        private ManualResetEventSlim _sync;
        private Thread _thread;

        private bool _disposed;

        #endregion Fields

        #region Constructors

        public AudioMixer(AudioFormat format)
            : this(format.Duration)
        {
            _format = format;
        }

        internal AudioMixer(int interval)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _locker = new object();
            _interval = interval;

            _streams = new HashSet<IAudioStream>();
            _chunks = new Queue<AudioChunk>();

            _cancellation = new CancellationTokenSource();
            _sync = new ManualResetEventSlim(false);
            _thread = new Thread(HandleMixer);
            _thread.Start();
        }

        #endregion Constructors

        #region Methods

        internal void WaitSync()
        {
            _sync.Set();
        }

        public void Append(IAudioStream stream)
        {
            lock (_locker)
            {
                _streams.Add(stream);
            }
        }

        public void Removed(IAudioStream stream)
        {
            lock (_locker)
            {
                _streams.Remove(stream);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void HandleMixer()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                if (_sync.Wait(_interval))
                {
                    _sync.Reset();
                }

                var samplesCount = _format.GetSamples();
                var mixture = new byte[samplesCount];

                lock (_locker)
                {
                    foreach (IAudioStream stream in _streams)
                    {
                        try
                        {
                            var samples = new byte[samplesCount];

                            int count = stream.Read(samples, 0, samplesCount);
                            if (count > 0)
                            {
                                Sum32Bit(samples, mixture);
                            }

                            _chunks.Enqueue(new AudioChunk(stream, new ArraySegment<byte>(samples, 0, count)));
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                        }
                    }
                }

                while (_chunks.TryDequeue(out AudioChunk chunk))
                {
                    try
                    {
                        if (chunk.Samples.Count - chunk.Samples.Offset > 0)
                        {
                            Sub32Bit(mixture, chunk.Samples);
                        }

                        chunk.Write();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }
                }
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

                _sync.Dispose();
                _sync = null;

                _cancellation.Dispose();
                _cancellation = null;

                _streams.Clear();
            }

            _disposed = true;
        }

        [Obsolete("Remove: use safe methods.")]
        internal static unsafe void Sum32Bit(ArraySegment<byte> source, byte[] dest)
        {
            fixed (byte* pSource = &source.Array[0], pDest = &dest[0])
            {
                int count = (source.Count - source.Offset) / 4;

                float* pfSource = (float*)pSource;
                float* pfDest = (float*)pDest;

                for (int n = 0; n < count; n++)
                {
                    pfDest[n] += pfSource[n];
                }
            }
        }

        [Obsolete("Remove: use safe methods.")]
        internal static unsafe void Sub32Bit(byte[] source, ArraySegment<byte> dest)
        {
            fixed (byte* pSource = &source[0], pDest = &dest.Array[0])
            {
                int count = (dest.Count - dest.Offset) / 4;

                float* pfSource = (float*)pSource;
                float* pfDest = (float*)pDest;

                for (int n = 0; n < count; n++)
                {
                    pfDest[n] = pfSource[n] - pfDest[n];
                }
            }
        }

        #endregion Methods
    }
}
