namespace Chat.Api.Messages
{
    using System;


    public class UnauthorizationBroadcast : IMessage, IEquatable<UnauthorizationBroadcast>
    {
        #region Methods

        public bool Equals(UnauthorizationBroadcast other)
        {
            return true;
        }

        #endregion Methods
    }
}
