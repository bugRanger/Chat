namespace Chat.Api.Messages.login
{
    using System;
    using System.Linq;
    using Newtonsoft.Json;

    public class UsersBroadcast : IMessage, IEquatable<UsersBroadcast>
    {
        #region Properties

        [JsonProperty(nameof(Users))]
        public string[] Users { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(UsersBroadcast other)
        {
            return
                other != null &&
                Users == other.Users ||
                Users.SequenceEqual(other.Users);
        }

        #endregion Methods
    }
}
