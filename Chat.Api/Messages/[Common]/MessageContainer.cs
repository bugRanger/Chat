namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class MessageContainer
    {
        [JsonProperty(nameof(Id))]
        public int Id { get; set; }

        [JsonProperty(nameof(Type))]
        public string Type { get; set; }

        [JsonProperty(nameof(Payload))]
        public object Payload { get; set; }
    }
}
