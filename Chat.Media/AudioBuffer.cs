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
        #region Constants

        //private const int BUFFER_LIMIT = 50;
        //private const int BUFFER_WAIT = 10;

        #endregion Constants

        #region Fields
        
        //private readonly ConcurrentDictionary<uint, IAudioPacket> _buffering;
        //private readonly CancellationTokenSource _cancellation;
        private readonly BufferedWaveProvider _waveProvider;
        private readonly ISampleProvider _sampleProvider;
        private readonly IAudioCodec _codec;

        //private readonly int _delta;
        //private uint _lastIndex;
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
            //_cancellation = new CancellationTokenSource();

            //_ = Task.Run(async () => await Dequeue(_cancellation.Token));
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
            _waveProvider.AddSamples(uncompressed, 0, uncompressed.Length);
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

        //private async Task Dequeue(CancellationToken token)
        //{
        //    // TODO:
        //    // 1. Кадр уже есть, не смотря на время. Передаем его дальше.
        //    // 2. Есть еще время для ожидания кадра.
        //    // 3. Время вышло переходим к следующему кадру.

        //    var lastTime = DateTime.Now;

        //    while (!token.IsCancellationRequested)
        //    {
        //        var currTime = DateTime.Now;
        //        try
        //        {
        //            if (!_buffering.TryGetValue(GetIndex(_lastIndex + 1), out var packet) && currTime - lastTime <= TimeSpan.FromMilliseconds(_delta))
        //            {
        //                await Task.Delay(BUFFER_WAIT, token);
        //                continue;
        //            }

        //            do
        //            {
        //                _lastIndex = GetIndex(_lastIndex + 1);
        //                if (packet == null || token.IsCancellationRequested)
        //                {
        //                    break;
        //                }

        //                _buffering.Remove(GetIndex(packet.SequenceId), out _);

        //                var uncompressed = _codec.Decode(packet.Payload);
        //                _waveProvider.AddSamples(uncompressed, 0, uncompressed.Length);
        //            }
        //            while (_buffering.TryGetValue(GetIndex(_lastIndex + 1), out packet));

        //            lastTime = DateTime.Now;
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            // Ignore.
        //        }
        //    }
        //}

        //private uint GetIndex(uint index)
        //{
        //    return index % BUFFER_LIMIT;
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                //_cancellation.Cancel();
                //_buffering.Clear();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}