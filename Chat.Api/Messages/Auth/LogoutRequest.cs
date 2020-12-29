namespace Chat.Api.Messages.Auth
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
