namespace Chat.Client.Audio
{
    using System;

    using Chat.Audio;

    public interface IAudioController
    {
        #region Properties

        public AudioFormat Format { get; }

        #endregion Properties

        #region Methods

        public void Append(int routeId, IAudioCodec codec);

        public void Remove(int routeId);

        #endregion Methods
    }
}
