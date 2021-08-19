namespace Chat.Audio
{
    using System;

    public interface IAudioController
    {
        #region Methods

        void Append(int routeId);

        void Remove(int routeId);

        #endregion Methods
    }
}
