namespace Chat.Client.Commander
{
    class MuteCommand : ICommand
    {
        #region Properties

        // TODO: Add mute support for call session.
        public int SessionId { get; internal set; }

        #endregion Properties
    }
}