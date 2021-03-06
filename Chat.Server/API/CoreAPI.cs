﻿namespace Chat.Server.API
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using NLog;
    using Newtonsoft.Json;

    using Chat.Api;
    using Chat.Api.Messages;
    using Chat.Net.Socket;

    delegate void HandleMessage(IPEndPoint remote, int index, IMessage message);

    public class CoreApi : ICoreApi
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly ITcpСontroller _network;
        private readonly IMessageFactory _messageFactory;
        private readonly List<IApiModule> _modules;
        private readonly Dictionary<Type, HandleMessage> _messages;

        #endregion Fields

        #region Events

        public event Action<IPEndPoint> ConnectionClosing;

        #endregion Events

        #region Constructors

        public CoreApi(ITcpСontroller network, IMessageFactory messageFactory)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _messageFactory = messageFactory;
            _messages = new Dictionary<Type, HandleMessage>();
            _modules = new List<IApiModule>();

            _network = network;
            _network.PreparePacket += OnPreparePacket;
            _network.ConnectionClosing += OnConnectionClosing;
        }

        #endregion Constructors

        #region Methods

        public void Registration(IApiModule module)
        {
            _modules.Add(module);
        }

        public void Send(IMessage message, IPEndPoint remote, int index)
        {
            if (!_messageFactory.TryPack(index, message, out var bytes))
            {
                _logger.Error("Failed to pack");
                return;
            }

            _network.Send(remote, bytes);
        }

        public void Send(IMessage message, params IPEndPoint[] remotes)
        {
            if (!_messageFactory.TryPack(0, message, out var bytes))
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
            _network.Disconnect(remote);
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
            try
            {
                while (_messageFactory.TryUnpack(bytes, ref offset, count, out var request))
                {
                    Handle(remote, request.Id, (IMessage)request.Payload);
                }
            }
            catch (JsonException ex)
            {
                _logger.Warn(ex);
                Send(new MessageResult { Status = StatusCode.Failure, Reason = ex.Message }, remote);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private void OnConnectionClosing(IPEndPoint remote)
        {
            ConnectionClosing?.Invoke(remote);
        }

        #endregion Methods
    }
}
