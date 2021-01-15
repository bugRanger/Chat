namespace Chat.Api.Messages.Call
{
    using System;

    using Newtonsoft.Json;

    public class CallInviteRequest : IMessage, IEquatable<CallInviteRequest>
    {
        #region Properties

        [JsonProperty(nameof(SessionId), Required = Required.Always)]
        public int SessionId { get; set; }

        [JsonProperty(nameof(RoutePort), Required = Required.Always)]
        public int RoutePort { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(CallInviteRequest other)
        {
            return
                other != null &&
                SessionId == other.SessionId &&
                RoutePort == other.RoutePort;
        }

        #endregion Methods
    }
}
