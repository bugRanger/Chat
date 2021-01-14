namespace Chat.Media
{
    using System;

    public interface IAudioPacket
    {
        #region Properties

        int RouteId { get; }

        int Timestamp { get; }

        ArraySegment<byte> Payload { get; }

        #endregion Properties

        #region Methods

        ArraySegment<byte> Pack();

        #endregion Methods
    }
}
