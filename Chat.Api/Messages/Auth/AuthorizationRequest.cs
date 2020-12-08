namespace Chat.Api.Messages.Auth
{
    using System;

    using Newtonsoft.Json;

    public class AuthorizationRequest : IMessage, IEquatable<AuthorizationRequest>
    {
        #region Properties

        [JsonProperty(nameof(User))]
        public string User { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(AuthorizationRequest other)
        {
            return User == other?.User;
        }

        #endregion Methods
    }
}
