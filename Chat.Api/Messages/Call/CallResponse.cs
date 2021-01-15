namespace Chat.Api.Messages.Call
{
    using System;

    using Newtonsoft.Json;

    public class CallResponse : IMessage, IEquatable<CallResponse>
    {
        #region Properties

        [JsonProperty(nameof(SessionId), Required = Required.Always)]
        public int SessionId { get; set; }

        [JsonProperty(nameof(RouteId), Required = Required.Always)]
        public int RouteId { get; set;  }

        #endregion Properties

        #region Methods

        public bool Equals(CallResponse other)
        {
            return
                other != null &&
                SessionId == other.SessionId &&
                RouteId == other.RouteId;
        }

        #endregion Methods
    }
}
