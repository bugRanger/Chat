namespace Chat.Api.Messages.Auth
{
    using System;

    using Newtonsoft.Json;

    public class DisconnectRequest : IMessage, IEquatable<DisconnectRequest>
    {
        #region Properties

        [JsonProperty(nameof(User))]
        public string User { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(DisconnectRequest other)
        {
            return User == other?.User;
        }

        #endregion Methods
    }
}
