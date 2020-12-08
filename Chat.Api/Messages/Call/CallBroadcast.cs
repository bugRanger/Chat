namespace Chat.Api.Messages.Call
{
    using System;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class CallBroadcast : IMessage, IEquatable<CallBroadcast>
    {
        #region Properties

        [JsonProperty(nameof(CallId))]
        public int CallId { get; set; }

        [JsonProperty(nameof(Source))]
        public string Source { get; set; }

        [JsonProperty(nameof(Target))]
        public string Target { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CallState State { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(CallBroadcast other)
        {
            return
                other != null &&
                Source == other.Source &&
                Target == other.Target &&
                State == other.State &&
                CallId == other.CallId;
        }

        #endregion Methods
    }
}
