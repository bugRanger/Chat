namespace Chat.Api.Messages.Auth
{
    using System;

    using Newtonsoft.Json;

    public class LoginRequest : IMessage, IEquatable<LoginRequest>
    {
        #region Properties

        [JsonProperty(nameof(User), Required = Required.Always)]
        public string User { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(LoginRequest other)
        {
            return User == other?.User;
        }

        #endregion Methods
    }
}
