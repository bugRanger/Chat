namespace Chat.Client.Commander
{
    class MessageCommand : ICommand
    {
        #region Properties

        public string Target { get; internal set; }

        public string Message { get; internal set; }

        #endregion Properties
    }
}