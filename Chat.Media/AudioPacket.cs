namespace Chat.Audio
{
    using System;

    public class AudioPacket : IAudioPacket
    {
        #region Constants

        private const int HEADER_LENGTH = 2;
        private const int PACKET_LENGTH = 9;

        #endregion Constants

        #region Properties

        /// TODO Impl multiple combine packet.
        public int RouteId { get; set; }

        public bool Mark { get; set; }

        public uint SequenceId { get; set; }

        public ArraySegment<byte> Payload { get; set; }

        #endregion Properties

        #region Methods

        public bool TryUnpack(byte[] buffer, ref int offset, int count)
        {
            if (count - offset < HEADER_LENGTH + PACKET_LENGTH)
            {
                return false;
            }

            var tmpOffset = offset;

            var length = BitConverter.ToInt16(buffer, tmpOffset);
            tmpOffset += 2;

            if (length < PACKET_LENGTH)
            {
                return false;
            }

            RouteId = BitConverter.ToInt32(buffer, tmpOffset);
            tmpOffset += 4;
            Mark = BitConverter.ToBoolean(buffer, tmpOffset);
            tmpOffset += 1;
            SequenceId = BitConverter.ToUInt32(buffer, tmpOffset);
            tmpOffset += 4;

            Payload = new ArraySegment<byte>(buffer, tmpOffset, length - tmpOffset);
            tmpOffset += Payload.Count;

            offset = tmpOffset;
            return true;
        }

        public ArraySegment<byte> Pack()
        {
            var buffer = new byte[HEADER_LENGTH + PACKET_LENGTH + Payload.Count];
            var offset = 0;

            var span = new Span<byte>(buffer);

            BitConverter.TryWriteBytes(span.Slice(offset), buffer.Length);
            offset += 2;

            BitConverter.TryWriteBytes(span.Slice(offset), RouteId);
            offset += 4;
            BitConverter.TryWriteBytes(span.Slice(offset), Mark);
            offset += 1;
            BitConverter.TryWriteBytes(span.Slice(offset), SequenceId);
            offset += 4;

            Buffer.BlockCopy(Payload.Array, Payload.Offset, buffer, offset, Payload.Count);

            return new ArraySegment<byte>(buffer);
        }

        #endregion Methods
    }
}
