namespace Chat.Server.API
{
    using System;
    using System.Net;

    using Chat.Api;
    using Chat.Api.Messages;

    public class TextApi : IApiModule
    {
        #region Fields

        private readonly ICoreApi _core;
        private readonly IUserContainer _users;

        #endregion Fields

        #region Constructors

        public TextApi(ICoreApi core, IUserContainer users)
        {
            _users = users;
            _core = core;
            _core.Registration<MessageBroadcast>(HandleMessage);
        }

        #endregion Constructors

        #region Methods

        private void HandleMessage(IPEndPoint remote, MessageBroadcast message)
        {
            if (string.IsNullOrWhiteSpace(message.Target))
            {
                HandleGroupMessage(remote, message);
            }
            else
            {
                HandlePrivateMessage(remote, message);
            }
        }

        private void HandlePrivateMessage(IPEndPoint remote, MessageBroadcast message)
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

            _core.Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            message.Source = source.Name;

            _core.Send(message, target.Remote);
        }

        private void HandleGroupMessage(IPEndPoint remote, MessageBroadcast message)
        {
            _core.Send(new MessageResponse { Status = StatusCode.Failure, Reason = "Not supported" }, remote);
        }
        
        #endregion Methods
    }
}
