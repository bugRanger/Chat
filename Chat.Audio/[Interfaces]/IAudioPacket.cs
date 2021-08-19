namespace Chat.Audio
{
    using System;

    using Chat.Net.Jitter;

    public interface IAudioPacket : IPacket
    {
        #region Properties

        //int SessionId { get; }

        int RouteId { get; }

        ArraySegment<byte> Payload { get; }

        #endregion Properties

        #region Methods

        ArraySegment<byte> Pack();

        #endregion Methods
    }
}
