﻿namespace Chat.Client.Audio
{
    using System;

    using Chat.Audio;

    using NAudio.Wave;

    public class AudioRoute : IAudioStream, IDisposable
    {
        #region Fields

        private readonly AudioBuffer _buffer;
        private readonly IAudioCodec _codec;
        private readonly IAudioTransport _transport;

        private uint _sequenceId;
        private bool _disposed;
        private bool _first;

        #endregion Fields

        #region Properties

        public int Id { get; set; }

        public WaveFormat WaveFormat { get; }

        #endregion Properties

        #region Constructors

        public AudioRoute(IAudioCodec codec, IAudioTransport transport)
        {
            _codec = codec;
            _transport = transport;

            _buffer = new AudioBuffer(codec);
            _first = true;

            WaveFormat = _codec.Format.ToWaveFormat();
        }

        #endregion Constructors

        #region Methods

        public void Handle(IAudioPacket packet)
        {
            _buffer.Enqueue(packet);
        }

        public void Write(ArraySegment<byte> bytes)
        {
            // TODO thread queued.
            var compressed = _codec.Encode(bytes);

            var packet = new AudioPacket
            {
                Mark = _first,
                SequenceId = ++_sequenceId,
                RouteId = Id,
                Payload = compressed,
            };

            _first = false;

            _transport.Send(packet);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _buffer.Read(buffer, offset, count);
        }

        public void Flush()
        {
            _first = true;
            _sequenceId = 0;
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
                _buffer.Dispose();
                _codec.Dispose();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
