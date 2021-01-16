namespace Chat.Client.Commander
{
    class HangUpCommand : ICommand
    {
        #region Properties

        public int SessionId { get; internal set; }

        #endregion Properties
    }
}