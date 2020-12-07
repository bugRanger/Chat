namespace Chat.Media
{
    using System;

    public interface IAudioSender : IDisposable
    {
        void Send(ArraySegment<byte> bytes);
    }
}
