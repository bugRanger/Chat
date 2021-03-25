namespace Chat.Net.Jitter
{
    public interface IPacket
    {
        #region Properties

        bool Mark { get; }

        uint SequenceId { get; }

        #endregion Properties
    }
}
