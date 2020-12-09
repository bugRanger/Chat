namespace Chat.Api.Messages.Call
{
    using System;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class CallBroadcast : IMessage, IEquatable<CallBroadcast>
    {
        #region Properties

        [JsonProperty(nameof(SessionId))]
        public int SessionId { get; set; }

        [JsonProperty(nameof(Participants))]
        public string[] Participants { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CallState State { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(CallBroadcast other)
        {
            return
                other != null &&
                State == other.State &&
                SessionId == other.SessionId &&
                Participants.SequenceEqual(other.Participants);
        }

        #endregion Methods
    }
}
