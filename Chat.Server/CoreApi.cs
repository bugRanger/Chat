namespace Chat.Server
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    using NLog;

    using Chat.Api;
    using Chat.Api.Messages;

    public class CoreApi : ICoreApi
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, IConnection> _users;

        private readonly Dictionary<Type, Action<IConnection, IMessage>> _messages;

        #endregion Fields

        #region Constructors

        public CoreApi()
        {
            _logger = LogManager.GetCurrentClassLogger();

            _users = new ConcurrentDictionary<string, IConnection>();
            _messages = new Dictionary<Type, Action<IConnection, IMessage>>();

            Registration<AuthorizationBroadcast>(HandleAuthorization);
            Registration<UnauthorizationBroadcast>(HandleUnauthorization);
            Registration<MessageBroadcast>(HandleMessage);
        }
        #endregion Constructors

        #region Methods

        public void Handle(IConnection client, IMessage message)
        {
            if (!_messages.TryGetValue(message.GetType(), out var action))
            {
                _logger.Warn($"Unknown type: {message.GetType()}");
                return;
            }

            action(client, message);
        }

        private void Registration<T>(Action<IConnection, T> action)
            where T : IMessage
        {
            _messages.TryAdd(typeof(T), (client, message) => action(client, (T)message));
        }

        private void HandleAuthorization(IConnection client, AuthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_users.TryAdd(request.User, client))
            {
                status = StatusCode.AuthDuplicate;
                reason = "User exists";
            }

            Send(new MessageResponse { Status = status, Reason = reason }, client);
            if (status != StatusCode.Success)
                return;

            client.Closing += (s, e) => HandleDisconnect(request.User);

            Send(new UsersBroadcast { Users = _users.Keys.ToArray() }, client);
            Send(request, GetGroupClients());
        }

        private void HandleUnauthorization(IConnection client, UnauthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;
            
            //if (!_users.TryGetValue(request.User, out IClientConnection userClient))
            //{
            //}

            Send(new MessageResponse { Status = status, Reason = reason }, client);
            if (status != StatusCode.Success)
                return;

            Send(request, GetGroupClients());
        }

        private void HandleDisconnect(string userId)
        {
            if (!_users.TryRemove(userId, out _))
            {
                _logger.Error("User not found for disconnect.");
                return;
            }

            Send(new DisconnectBroadcast { User = userId }, GetGroupClients());
        }

        private void HandleMessage(IConnection client, MessageBroadcast message)
        {
            if (IsGroupTarget(message.Target))
            {
                HandleGroupMessage(client, message);
            }
            else
            {
                HandlePrivateMessage(client, message);
            }
        }

        private void HandlePrivateMessage(IConnection client, MessageBroadcast message)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_users.TryGetValue(message.Target, out var target) ||
                !_users.TryGetValue(message.Source, out _))
            {
                status = StatusCode.UserNotFound;
                reason = "Target not found.";
            }

            Send(new MessageResponse { Status = status, Reason = reason }, client);
            if (status != StatusCode.Success)
                return;

            Send(message, target);
        }

        private void HandleGroupMessage(IConnection client, MessageBroadcast message)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!IsGroupTarget(message.Target) ||
                !_users.TryGetValue(message.Source, out _))
            {
                status = StatusCode.UserNotFound;
                reason = "Target not found.";
            }

            Send(new MessageResponse { Status = status, Reason = reason }, client);
            if (status != StatusCode.Success)
                return;

            Send(message, GetGroupClients(message.Target));
        }

        private void Send(IMessage message, params IConnection[] clients)
        {
            if (!PacketFactory.TryPack(message, out var bytes))
            {
                _logger.Error("Failed to pack");
                return;
            }

            foreach (IConnection target in clients)
            {
                target.Send(bytes);
            }
        }

        // TODO Impl groups.
        private bool IsGroupTarget(string target)
        {
            return string.IsNullOrWhiteSpace(target);
        }

        private IConnection[] GetGroupClients(string targetGroup = null)
        {
            return _users.Values.ToArray();
        }

        #endregion Methods
    }
}
