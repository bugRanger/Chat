namespace Chat.Audio
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    using NAudio.Wave;

    public class AudioBuffer : ISampleProvider
    {
        #region Fields
        
        private readonly BufferedWaveProvider _waveProvider;
        private readonly ISampleProvider _sampleProvider;
        private readonly Jitter _jitter;
        private readonly IAudioCodec _codec;

        private readonly CancellationTokenSource _cancellation;
        private readonly Thread _thread;

        private bool _disposed;

        #endregion Fields

        #region Properties

        public WaveFormat WaveFormat => _waveProvider.WaveFormat;

        #endregion Properties

        #region Constructors

        public AudioBuffer(IAudioCodec codec)
        {
            _codec = codec;
            _waveProvider = new BufferedWaveProvider(_codec.Format.ToWaveFormat());
            _waveProvider.DiscardOnBufferOverflow = false;

            _sampleProvider = _waveProvider.ToSampleProvider();

            //_delta = delta;
            //_lastIndex = uint.MinValue;

            //_buffering = new ConcurrentDictionary<uint, IAudioPacket>();

            //_ = Task.Run(async () => await Dequeue(_cancellation.Token));

            _jitter = new Jitter(codec);

            _cancellation = new CancellationTokenSource();
            _thread = new Thread(JitterPulled);
            _thread.Start();
        }

        #endregion Constructors

        #region Methods

        public void Enqueue(IAudioPacket packet)
        {


            // TODO:
            // 1. Кадр уже есть, не смотря на время. Передаем его дальше.
            // 2. Есть еще время для ожидания кадра.
            // 3. Время вышло переходим к следующему кадру.
            //if (packet.SequenceId <= _lastIndex)
            //    return;

            //if (_buffering.TryAdd(GetIndex(packet.SequenceId), packet))
            //    return;

            var uncompressed = _codec.Decode(packet.Payload);

            _jitter.Push(packet);

            HandleFrame(false);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _sampleProvider.Read(buffer, offset, count);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void JitterPulled(object state)
        {
            while (!_cancellation.IsCancellationRequested)
            {
                Thread.Sleep(_codec.Format.Duration);

                HandleFrame(true);
            }
        }

        private void HandleFrame(bool hungry)
        {
            IAudioPacket packet;
            do
            {
                packet = _jitter.Pull(hungry);
                if (packet == null)
                {
                    break;
                }

                byte[] uncompressed;
                if (packet.Payload != null)
                {
                    uncompressed = _codec.Decode(packet.Payload);
                }
                else
                {
                    packet = _jitter.Peek();
                    uncompressed = _codec?.Restore(packet?.Payload ?? null);
                }

                _waveProvider.AddSamples(uncompressed, 0, uncompressed.Length);
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

                _jitter.Clear();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}