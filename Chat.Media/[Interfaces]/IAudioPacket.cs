namespace Chat.Media
{
    using System;

    public interface IAudioPacket
    {
        #region Properties

        int SessionId { get; }

        int RouteId { get; }

        uint SequenceId { get; }

        ArraySegment<byte> Payload { get; }

        #endregion Properties

        #region Methods

        ArraySegment<byte> Pack();

        #endregion Methods
    }
}
