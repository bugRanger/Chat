namespace Chat.Client.Commander
{
    class UnmuteCommand : ICommand
    {
        #region Properties

        // TODO: Add unmute support for call session.
        public int SessionId { get; internal set; }

        #endregion Properties
    }
}