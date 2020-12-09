namespace Chat.Api.Messages.Call
{
    using System;

    using Newtonsoft.Json;

    public class CallCancelRequest : IMessage, IEquatable<CallCancelRequest>
    {
        #region Properties

        [JsonProperty(nameof(SessionId))]
        public int SessionId { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(CallCancelRequest other)
        {
            return
                other != null &&
                SessionId == other.SessionId;
        }

        #endregion Methods
    }
}
