namespace Chat.Api.Messages.Call
{
    using System;

    using Newtonsoft.Json;

    public class CallRequest : IMessage, IEquatable<CallRequest>
    {
        #region Properties

        [JsonProperty(nameof(Source), Required = Required.Always)]
        public string Source { get; set; }

        [JsonProperty(nameof(Target), Required = Required.Always)]
        public string Target { get; set; }

        [JsonProperty(nameof(RoutePort), Required = Required.Always)]
        public int RoutePort { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(CallRequest other)
        {
            return
                other != null &&
                Source == other.Source &&
                Target == other.Target &&
                RoutePort == other.RoutePort;
        }

        #endregion Methods
    }
}
