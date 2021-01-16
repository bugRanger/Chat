namespace Chat.Client.Commander
{
    using System;

    class LoginCommand : ICommand
    {
        #region Properties

        public string User { get; set; }

        #endregion Properties
    }
}
