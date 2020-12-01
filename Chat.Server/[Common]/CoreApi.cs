namespace Chat.Server
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    using NLog;

    using Chat.Api;
    using Chat.Api.Messages;

    public class CoreApi : ICoreApi
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly INetworkСontroller _network;
        private readonly IAuthorizationController _authorization;

        private readonly Dictionary<Type, Action<IPEndPoint, IMessage>> _messages;

        #endregion Fields

        #region Constructors

        public CoreApi(INetworkСontroller network, IAuthorizationController authorization)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _messages = new Dictionary<Type, Action<IPEndPoint, IMessage>>();

            _authorization = authorization;
            _network = network;
            _network.Closing += OnNetworkClosing;

            Registration<AuthorizationBroadcast>(HandleAuthorization);
            Registration<UnauthorizationBroadcast>(HandleUnauthorization);
            Registration<MessageBroadcast>(HandleMessage);
        }

        #endregion Constructors

        #region Methods

        public void Handle(IPEndPoint remote, IMessage message)
        {
            if (!_messages.TryGetValue(message.GetType(), out var action))
            {
                Send(new MessageResponse { Status = StatusCode.UnknownMessage }, remote);
                _logger.Warn($"Unknown type: {message.GetType()}");
                return;
            }

            action(remote, message);
        }

        private void Registration<T>(Action<IPEndPoint, T> action)
            where T : IMessage
        {
            _messages.TryAdd(typeof(T), (remote, message) => action(remote, (T)message));
        }

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

                if (!_authorization.TryAddOrUpdate(request.User, remote))
                {
                    status = StatusCode.Failure;
                    reason = "Server internal error";
                }
            }

            Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            // TODO send User detail.
            Send(new UsersBroadcast { Users = users.Select(s => s.Name).ToArray() }, remote);
            Send(request, remotes);
        }

        private void HandleUnauthorization(IPEndPoint remote, UnauthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_authorization.TryGet(remote, out _))
            {
                status = StatusCode.Failure;
                reason = "User is not logged in";
            }

            if (status == StatusCode.Success)
            {
                _network.Disconnect(remote, false);
            }

            Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            var remotes = _authorization
                .GetUsers()
                .Select(s => s.Remote)
                .ToArray();

            Send(request, remotes);
        }

        private void HandleMessage(IPEndPoint remote, MessageBroadcast message)
        {
            if (IsGroupTarget(message.Target))
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

            if (!_authorization.TryGet(message.Target, out _) ||
                !_authorization.TryGet(message.Source, out _))
            {
                status = StatusCode.UserNotFound;
                reason = "Source or target not found.";
            }

            Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            Send(message, remote);
        }

        private void HandleGroupMessage(IPEndPoint remote, MessageBroadcast message)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!IsGroupTarget(message.Target) ||
                !_authorization.TryGet(message.Source, out _))
            {
                status = StatusCode.UserNotFound;
                reason = "Source or target not found.";
            }

            Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            // TODO Impl group service.
            var remotes = GetGroupUsers(message.Target)
                .Select(s => s.Remote)
                .ToArray();

            Send(message, remotes);
        }

        private void OnNetworkClosing(IPEndPoint remote, bool inactive)
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

            Send(new DisconnectBroadcast { User = user.Name }, remotes);
        }

        private void Send(IMessage message, params IPEndPoint[] remotes)
        {
            if (!PacketFactory.TryPack(message, out var bytes))
            {
                _logger.Error("Failed to pack");
                return;
            }

            foreach (IPEndPoint target in remotes)
            {
                _network.Send(target, bytes);
            }
        }

        // TODO Impl group service.
        private bool IsGroupTarget(string target)
        {
            return string.IsNullOrWhiteSpace(target);
        }

        // TODO Impl group service.
        private IUser[] GetGroupUsers(string groupId) 
        {
            return _authorization.GetUsers();
        }

        #endregion Methods
    }
}
