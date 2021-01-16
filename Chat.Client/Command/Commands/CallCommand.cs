namespace Chat.Client.Commander
{
    class CallCommand : ICommand
    {
        #region Properties

        public string Target { get; internal set; }

        #endregion Properties
    }
}