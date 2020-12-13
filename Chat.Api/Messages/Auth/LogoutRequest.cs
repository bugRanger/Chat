namespace Chat.Api.Messages.login
{
    using System;


    public class LogoutRequest : IMessage, IEquatable<LogoutRequest>
    {
        #region Methods

        public bool Equals(LogoutRequest other)
        {
            return true;
        }

        #endregion Methods
    }
}
