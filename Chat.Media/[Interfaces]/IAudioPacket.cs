namespace Chat.Audio
{
    using System;
    
    public interface IAudioPacket
    {
        #region Properties

        //int SessionId { get; }

        int RouteId { get; }

        bool Mark { get; }

        uint SequenceId { get; }

        ArraySegment<byte> Payload { get; }

        #endregion Properties

        #region Methods

        ArraySegment<byte> Pack();

        #endregion Methods
    }
}
