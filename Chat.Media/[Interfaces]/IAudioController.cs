namespace Chat.Media
{
    using System;

    using NAudio.Wave;

    public interface IAudioController 
    {
        #region Fields

        WaveFormat Format { get; }

        #endregion Fields

        #region Events

        event Action<ArraySegment<byte>> Received;

        #endregion Events

        #region Methods

        public void Append(ISampleProvider provider);

        public void Remove(ISampleProvider provider);

        #endregion Methods
    }
}
