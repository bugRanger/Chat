namespace Chat.Audio
{
    using System;

    public interface IAudioTransport
    {
        #region Methods

        void Send(IAudioPacket packet);

        #endregion Methods
    }
}
