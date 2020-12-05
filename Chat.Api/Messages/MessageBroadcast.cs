namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class MessageBroadcast : IMessage, IEquatable<MessageBroadcast>
    {
        #region Properties

        [JsonProperty(nameof(Source))]
        public string Source { get; set; }

        [JsonProperty(nameof(Target))]
        public string Target { get; set; }

        [JsonProperty(nameof(Message))]
        public string Message { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(MessageBroadcast other)
        {
            return 
                other != null &&
                Source == other.Source &&
                Target == other.Target &&
                Message == other.Message;
        }

        #endregion Methods
    }
}
