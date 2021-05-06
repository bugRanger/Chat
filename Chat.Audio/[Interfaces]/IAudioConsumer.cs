namespace Chat.Audio
{
    using System;

    public interface IAudioConsumer
    {
        #region Methods

        void Append(IWaveStream route);

        void Remove(IWaveStream route);

        #endregion Methods
    }
}
