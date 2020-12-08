namespace Chat.Api.Messages.Call
{
    using System;

    using Newtonsoft.Json;

    public class CallRequest : IMessage, IEquatable<CallRequest>
    {
        #region Properties

        [JsonProperty(nameof(Source))]
        public string Source { get; set; }

        [JsonProperty(nameof(Target))]
        public string Target { get; set; }

        [JsonProperty(nameof(MediaPort))]
        public int MediaPort { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(CallRequest other)
        {
            return
                other != null &&
                Source == other.Source &&
                Target == other.Target &&
                MediaPort == other.MediaPort;
        }

        #endregion Methods
    }
}
