namespace Chat.Audio
{
    using System;

    public class AudioChunk
    {
        #region Fields

        private readonly IAudioStream _stream;

        #endregion Fields

        #region Properties

        public ArraySegment<byte> Samples { get; }

        #endregion Properties

        #region Constructors

        public AudioChunk(IAudioStream stream, ArraySegment<byte> samples) 
        {
            _stream = stream;
            Samples = samples;
        }

        #endregion Constructors

        #region Methods

        public void Write()
        {
            _stream.Write(Samples);
        }

        #endregion Methods
    }
}
