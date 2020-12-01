namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class UsersBroadcast : IMessage
    {
        [JsonProperty(nameof(Users))]
        public string[] Users { get; set; }
    }
}
