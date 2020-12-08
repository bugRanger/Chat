namespace Chat.Api.Messages.Auth
{
    using System;


    public class UnauthorizationRequest : IMessage, IEquatable<UnauthorizationRequest>
    {
        #region Methods

        public bool Equals(UnauthorizationRequest other)
        {
            return true;
        }

        #endregion Methods
    }
}
