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
        private readonly Dictionary<Type, Action<IPEndPoint, IMessage>> _messages;

        #endregion Fields

        #region Constructors

        public ApiController(INetworkСontroller network)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _messages = new Dictionary<Type, Action<IPEndPoint, IMessage>>();

            _network = network;
            _network.ConnectionAccepted += OnConnectionAccepted;
            _network.PreparePacket += OnPreparePacket;
            _network.ConnectionClosing += OnConnectionClosing;

            new AuthModule(this, _network, null);
        }

        #endregion Constructors

        #region Methods

        private void Handle(IPEndPoint remote, IMessage message)
        {
            if (!_messages.TryGetValue(message.GetType(), out var action))
            {
                Send(new MessageResponse { Status = StatusCode.UnknownMessage }, remote);
                _logger.Warn($"Unknown type: {message.GetType()}");
                return;
            }

            action(remote, message);
        }

        public void Registration<T>(params Action<IPEndPoint, T>[] actions)
            where T : IMessage
        {
            foreach (var action in actions)
            {
                _messages.TryAdd(typeof(T), (remote, message) => action(remote, (T)message));
            }
        }

        public void Send(IMessage message, params IPEndPoint[] remotes)
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

        private void OnPreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count)
        {
            if (!PacketFactory.TryUnpack(bytes, ref offset, count, out var request))
            {
                return;
            }

            Handle(remote, (IMessage)request.Payload);
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

            Send(new DisconnectBroadcast { User = user.Name }, remotes);
        }

        // TODO send User detail.
        private string GetUserDetail(IUser user)
        {
            return user.Name;
        }

        #endregion Methods
    }
}
