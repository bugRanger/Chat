namespace Chat.Audio
{
    using System;

    public interface IAudioConsumer
    {
        #region Methods

        void Append(IAudioStream route);

        void Remove(IAudioStream route);

        #endregion Methods
    }
}
