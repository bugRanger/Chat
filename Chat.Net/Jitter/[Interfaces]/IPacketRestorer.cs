namespace Chat.Net.Jitter
{
    public interface IPacketRestorer<T>
        where T : IPacket
    {
        #region Methods

        void Append(T packet);

        T Recovery(uint seq, T peek);

        #endregion Methods
    }
}