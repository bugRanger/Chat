namespace Chat.Media
{
    using System;

    public interface IAudioReceiver
    {
        event Action<ArraySegment<byte>> Received;
    }
}
