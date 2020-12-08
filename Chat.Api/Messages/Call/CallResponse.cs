namespace Chat.Api.Messages.Call
{
    using System;

    using Newtonsoft.Json;

    public class CallResponse : IMessage, IEquatable<CallResponse>
    {
        #region Properties

        [JsonProperty(nameof(CallId))]
        public int CallId { get; set; }

        [JsonProperty(nameof(MediaId))]
        public int MediaId { get; set;  }

        #endregion Properties

        #region Methods

        public bool Equals(CallResponse other)
        {
            return
                other != null &&
                CallId == other.CallId &&
                MediaId == other.MediaId;
        }

        #endregion Methods
    }
}
