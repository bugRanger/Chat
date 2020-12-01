namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class MessageRequest
    {
        [JsonProperty(nameof(Type))]
        public string Type { get; set; }

        [JsonProperty(nameof(Payload))]
        public object Payload { get; set; }
    }
}
