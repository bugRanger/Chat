namespace Chat.Media
{
    using System;

    public interface IAudioReceiver : IDisposable
    {
        event Action<ArraySegment<byte>> Recevied;
    }
}
