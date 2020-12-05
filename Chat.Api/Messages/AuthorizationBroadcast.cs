namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class AuthorizationBroadcast : IMessage, IEquatable<AuthorizationBroadcast>
    {
        #region Properties

        [JsonProperty(nameof(User))]
        public string User { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(AuthorizationBroadcast other)
        {
            return User == other?.User;
        }

        #endregion Methods
    }
}
