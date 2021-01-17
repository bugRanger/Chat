namespace Chat.Media
{
    using System;

    public interface IAudioSender
    {
        void Send(ArraySegment<byte> bytes);
    }
}
