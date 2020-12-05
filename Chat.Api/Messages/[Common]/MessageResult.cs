namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class MessageResult : IMessage
    {
        [JsonProperty(nameof(Status))]
        public StatusCode Status { get; set; }

        [JsonProperty(nameof(Reason))]
        public string Reason { get; set; }
    }
}
