namespace Chat.Server.API
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    using NLog;

    using Chat.Api;
    using Chat.Api.Messages;
    using Chat.Api.Messages.Auth;


    delegate void HandleMessage(IPEndPoint remote, int index, IMessage message);

    public class CoreApi : ICoreApi
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly List<IApiModule> _modules;
        private readonly INetworkСontroller _network;
        private readonly IAuthorizationController _authorization;
        private readonly Dictionary<Type, HandleMessage> _messages;

        #endregion Fields

        #region Constructors

        public CoreApi(INetworkСontroller network, IAuthorizationController authorization)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _messages = new Dictionary<Type, HandleMessage>();
            _modules = new List<IApiModule>
            {
                new AuthApi(this, authorization),
                new TextApi(this, authorization),
            };

            _authorization = authorization;
            _network = network;
            _network.PreparePacket += OnPreparePacket;
            _network.ConnectionClosing += OnConnectionClosing;
        }

        #endregion Constructors

        #region Methods


        public void Send(IMessage message, IPEndPoint remote, int index)
        {
            if (!PacketFactory.TryPack(index, message, out var bytes))
            {
                _logger.Error("Failed to pack");
                return;
            }

            _network.Send(remote, bytes);
        }

        public void Send(IMessage message, params IPEndPoint[] remotes)
        {
            if (!PacketFactory.TryPack(0, message, out var bytes))
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

        public void Registration<T>(Action<IPEndPoint, int, T> action)
            where T : IMessage
        {
            _messages.TryAdd(typeof(T), (remote, id, message) => action(remote, id, (T)message));
        }

        private void Handle(IPEndPoint remote, int id, IMessage message)
        {
            if (!_messages.TryGetValue(message.GetType(), out var action))
            {
                Send(new MessageResult { Status = StatusCode.UnknownMessage }, remote);
                _logger.Warn($"Unknown type: {message.GetType()}");
                return;
            }

            action(remote, id, message);
        }

        private void OnPreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count)
        {
            if (!PacketFactory.TryUnpack(bytes, ref offset, count, out var request))
            {
                return;
            }

            Handle(remote, request.Id, (IMessage)request.Payload);
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

            Send(new DisconnectRequest { User = user.Name }, remotes);
        }

        #endregion Methods
    }
}
