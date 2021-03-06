﻿namespace Chat.Server.API
{
    using System;
    using System.Net;

    using Chat.Api;
    using Chat.Api.Messages;
    using Chat.Api.Messages.Text;

    using Chat.Server.Auth;

    public class TextApi : IApiModule
    {
        #region Fields

        private readonly ICoreApi _core;
        private readonly IUserContainer _users;

        #endregion Fields

        #region Constructors

        public TextApi(ICoreApi core, IUserContainer users)
        {
            _core = core;
            _users = users;

            _core.Registration(this);
            _core.Registration<MessageBroadcast>(HandleMessage);
        }

        #endregion Constructors

        #region Methods

        private void HandleMessage(IPEndPoint remote, int index, MessageBroadcast message)
        {
            if (string.IsNullOrWhiteSpace(message.Target))
            {
                HandleGroupMessage(remote, index, message);
            }
            else
            {
                HandlePrivateMessage(remote, index, message);
            }
        }

        private void HandlePrivateMessage(IPEndPoint remote, int index, MessageBroadcast message)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            IUser source = null;
            IUser target = null;

            if (!_users.TryGet(remote, out source))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }
            else if (!_users.TryGet(message.Target, out target))
            {
                status = StatusCode.UserNotFound;
                reason = "Target not found";
            }
            else if (string.IsNullOrWhiteSpace(message.Message))
            {
                status = StatusCode.Failure;
                reason = $"Invalid parameters: {nameof(MessageBroadcast.Message)}";
            }

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            message.Source = source.Name;

            _core.Send(message, target.Remote);
        }

        private void HandleGroupMessage(IPEndPoint remote, int index, MessageBroadcast message)
        {
            _core.Send(new MessageResult { Status = StatusCode.Failure, Reason = "Not supported" }, remote, index);
        }

        #endregion Methods
    }
}
