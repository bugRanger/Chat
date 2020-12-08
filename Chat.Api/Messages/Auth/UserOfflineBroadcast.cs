namespace Chat.Api.Messages.Auth
{
    using System;

    using Newtonsoft.Json;

    // TODO Replace user detail change offline, reason - kick server.
    public class UserOfflineBroadcast : IMessage, IEquatable<UserOfflineBroadcast>
    {
        #region Properties

        [JsonProperty(nameof(User))]
        public string User { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(UserOfflineBroadcast other)
        {
            return User == other?.User;
        }

        #endregion Methods
    }
}
