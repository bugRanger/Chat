namespace Chat.Server.API
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;

    using NLog;

    using Chat.Api;
    using Chat.Api.Messages;

    public class AuthModule
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly IApiController _controller;
        private readonly INetworkСontroller _network;
        private readonly IAuthorizationController _authorization;

        #endregion Fields

        #region Constructors

        public AuthModule(IApiController controller, IAuthorizationController authorization)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _authorization = authorization;
            _controller = controller;
        }

        #endregion Constructors

        #region Methods

        private void HandleAuthorization(IPEndPoint remote, AuthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            IUser[] users = null;
            IPEndPoint[] remotes = null;

            if (_authorization.TryGet(remote, out _))
            {
                status = StatusCode.AuthDuplicate;
                reason = "User exists";
            }
            else
            {
                users = _authorization.GetUsers();
                remotes = users
                    .Select(s => s.Remote)
                    .ToArray();

                _authorization.AddOrUpdate(remote, s =>
                {
                    s.Remote = remote;
                    s.Name = request.User;
                });
            }

            _controller.Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            _controller.Send(new UsersBroadcast { Users = users.Select(GetUserDetail).ToArray() }, remote);
            _controller.Send(request, remotes);
        }

        private void HandleUnauthorization(IPEndPoint remote, UnauthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_authorization.TryGet(remote, out _))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }

            if (status == StatusCode.Success)
            {
                _network.Disconnect(remote, false);
            }

            _controller.Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            var remotes = _authorization
                .GetUsers()
                .Select(s => s.Remote)
                .ToArray();

            _controller.Send(request, remotes);
        }

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

            if (!_authorization.TryGet(remote, out source))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }
            else if (!_authorization.TryGet(message.Target, out target))
            {
                status = StatusCode.UserNotFound;
                reason = "Target not found.";
            }

            _controller.Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            message.Source = source.Name;

            _controller.Send(message, target.Remote);
        }

        private void HandleGroupMessage(IPEndPoint remote, MessageBroadcast message)
        {
            _controller.Send(new MessageResponse { Status = StatusCode.Failure, Reason = "Not supported" }, remote);
        }

        private void OnConnectionAccepted(IPEndPoint remote)
        {
        }


        private void OnConnectionClosing(IPEndPoint remote, bool inactive)
        {
            if (!_authorization.TryRemove(remote, out IUser user))
            {
                _logger.Error("User not found for disconnect.");
                return;
            }

            var remotes = _authorization
                .GetUsers()
                .Select(s => s.Remote)
                .ToArray();

            _controller.Send(new DisconnectBroadcast { User = user.Name }, remotes);
        }

        #endregion Methods
    }
}
