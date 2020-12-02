namespace Chat.Server.API
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    using NLog;

    using Chat.Api;
    using Chat.Api.Messages;

    public class ApiController : IApiController
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly INetworkСontroller _network;
        private readonly IAuthorizationController _authorization;

        private readonly Dictionary<Type, Action<IPEndPoint, IMessage>> _messages;

        #endregion Fields

        #region Constructors

        public ApiController(INetworkСontroller network, IAuthorizationController authorization)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _messages = new Dictionary<Type, Action<IPEndPoint, IMessage>>();

            _network = network;
            _authorization = authorization;
            _network.PreparePacket += OnPreparePacket;
            _network.ConnectionClosing += OnConnectionClosing;

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

                _authorization.AddOrUpdate(remote, s =>
                {
                    s.Remote = remote;
                    s.Name = request.User;
                });
            }

            Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            Send(new UsersBroadcast { Users = users.Select(GetUserDetail).ToArray() }, remote);
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

            if (!_authorization.TryGet(message.Target, out _) ||
                !_authorization.TryGet(message.Source, out source))
            {
                status = StatusCode.UserNotFound;
                reason = "Source or target not found.";
            }

            Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            message.Source = source.Name;

            Send(message, remote);
        }

        private void HandleGroupMessage(IPEndPoint remote, MessageBroadcast message)
        {
            Send(new MessageResponse { Status = StatusCode.Failure, Reason = "Not supported" }, remote);
        }

        private void OnPreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count)
        {
            if (!PacketFactory.TryUnpack(bytes, ref offset, count, out var request))
            {
                return;
            }

            Handle(remote, (IMessage)request.Payload);
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

        // TODO send User detail.
        private string GetUserDetail(IUser user)
        {
            return user.Name;
        }

        #endregion Methods
    }
}
