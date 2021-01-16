namespace Chat.Client.Commander
{
    using System.Net;

    class ConnectCommand : ICommand
    {
        #region Properties

        public IPAddress Address { get; internal set; }

        public ushort Port { get; internal set; }

        #endregion Properties
    }
}