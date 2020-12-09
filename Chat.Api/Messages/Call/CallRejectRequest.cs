namespace Chat.Api.Messages.Call
{
    using System;

    using Newtonsoft.Json;

    public class CallRejectRequest : IMessage, IEquatable<CallRejectRequest>
    {
        #region Properties

        [JsonProperty(nameof(CallId))]
        public int CallId { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(CallRejectRequest other)
        {
            return
                other != null &&
                CallId == other.CallId;
        }

        #endregion Methods
    }
}
