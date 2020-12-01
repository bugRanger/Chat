namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class MessageBroadcast : IMessage
    {
        [JsonProperty(nameof(Source))]
        public string Source { get; set; }

        [JsonProperty(nameof(Target))]
        public string Target { get; set; }

        [JsonProperty(nameof(Message))]
        public string Message { get; set; }
    }
}
