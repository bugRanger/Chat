namespace Chat.Api
{
    using Messages;

    public interface IMessageFactory
    {
        bool TryPack(int index, IMessage payload, out byte[] buffer);

        bool TryUnpack(byte[] buffer, ref int offset, int count, out MessageContainer request);
    }
}