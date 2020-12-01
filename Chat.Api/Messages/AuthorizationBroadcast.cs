namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class AuthorizationBroadcast : IMessage
    {
        [JsonProperty(nameof(User))]
        public string User { get; set; }
    }
}
