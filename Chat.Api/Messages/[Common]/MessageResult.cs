namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class MessageResult : IMessage, IEquatable<MessageResult>
    {
        #region Properties

        [JsonProperty(nameof(Status))]
        public StatusCode Status { get; set; }

        [JsonProperty(nameof(Reason))]
        public string Reason { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(MessageResult other)
        {
            return
                other != null &&
                Status == other.Status &&
                Reason == other.Reason;
        }

        #endregion Methods
    }
}
