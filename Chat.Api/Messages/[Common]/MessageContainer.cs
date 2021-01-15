namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class MessageContainer
    {
        [JsonProperty(nameof(Id), Required = Required.Always)]
        public int Id { get; set; }

        [JsonProperty(nameof(Type), Required = Required.Always)]
        public string Type { get; set; }

        [JsonProperty(nameof(Payload), Required = Required.Always)]
        public object Payload { get; set; }
    }
}
