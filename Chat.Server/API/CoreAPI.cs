namespace Chat.Server.API
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    using NLog;

    using Chat.Api;
    using Chat.Api.Messages;

    using Chat.Server.Auth;

    public class CoreApi : ICoreApi
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly List<IApiModule> _modules;
        private readonly INetworkСontroller _network;
        private readonly AuthorizationController _authorization;
        private readonly Dictionary<Type, Action<IPEndPoint, IMessage>> _messages;

        #endregion Fields

        #region Constructors

        public CoreApi(INetworkСontroller network)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _messages = new Dictionary<Type, Action<IPEndPoint, IMessage>>();
            _authorization = new AuthorizationController();

            _network = network;
            _network.PreparePacket += OnPreparePacket;
            _network.ConnectionClosing += OnConnectionClosing;

            _modules = new List<IApiModule>
            {
                new AuthApi(this, _authorization),
                new TextApi(this, _authorization),
            };
        }

        #endregion Constructors

        #region Methods

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

        public void Disconnect(IPEndPoint remote)
        {
            _network.Disconnect(remote, false);
        }

        public void Registration<T>(Action<IPEndPoint, T> action)
            where T : IMessage
        {
            _messages.TryAdd(typeof(T), (remote, message) => action(remote, (T)message));
        }

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

        #endregion Methods
    }
}
