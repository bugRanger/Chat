﻿namespace Chat.Api.Messages
{
    using System;

    using Newtonsoft.Json;

    public class DisconnectBroadcast : IMessage, IEquatable<DisconnectBroadcast>
    {
        #region Properties

        [JsonProperty(nameof(User))]
        public string User { get; set; }

        #endregion Properties

        #region Methods

        public bool Equals(DisconnectBroadcast other)
        {
            return User == other?.User;
        }

        #endregion Methods
    }
}
