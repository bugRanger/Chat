namespace Chat.Media
{
    using System;

    using NAudio.Wave;

    public interface IAudioCodec : IDisposable
    {
        WaveFormat Format { get; }

        byte[] Encode(ArraySegment<byte> compressed);

        byte[] Decode(ArraySegment<byte> uncompressed);
    }
}
