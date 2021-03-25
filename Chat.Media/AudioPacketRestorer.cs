namespace Chat.Audio
{
    using System;

    using Chat.Net.Jitter;

    public class AudioPacketRestorer : IPacketRestorer<IAudioPacket>
    {
        #region Fields

        private IAudioPacket _lastPacket;

        #endregion Fields

        #region Methods

        public void Append(IAudioPacket packet)
        {
            _lastPacket = packet;
        }

        public IAudioPacket Recovery(uint seq, IAudioPacket peek)
        {
            if (_lastPacket == null && peek == null)
                return null;

            return new AudioPacket
            {
                SequenceId = seq,
                RouteId = peek?.RouteId ?? _lastPacket.RouteId,
                Payload = peek?.Payload ?? null,
                Mark = _lastPacket == null,
            };
        }

        #endregion Methods
    }
}
