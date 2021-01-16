namespace Chat.Client
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    using Chat.Api;
    using Chat.Api.Messages.Auth;
    using Chat.Api.Messages.Call;
    using Chat.Api.Messages.Text;

    using Chat.Client.Network;
    using Chat.Client.Commander;
    using Chat.Client.Commander.Commands;

    using Chat.Media;
    using Chat.Media.Codecs;
    using Chat.Client.Call;

    class Program
    {
        #region Properties

        static MessageFactory MessageFactory { get; set; }

        static EasySocket ApiSocket { get; set; }

        static EasySocket CallSocket { get; set; }

        static string Me { get; set; }

        static CallSession CallSession { get; set; }
        public static int CallSessionId { get; private set; }

        #endregion Properties

        static void Main(string[] args)
        {
            MessageFactory = new MessageFactory(true);

            ApiSocket = new EasySocket(ApiReceived, () => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            CallSocket = new EasySocket(CallReceived, () => new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));

            var commandParser = new CommandParser('!')
            {
                new CommandBuilder<ConnectCommand>("connect")
                    .Parameter("ip", (cmd, value) => cmd.Address = IPAddress.Parse(value))
                    .Parameter("p", (cmd, value) => cmd.Port = ushort.Parse(value))
                    .Build(ConnectionHandle),

                new CommandBuilder<DisconnectCommand>("disconnect")
                    .Build(DisconnectHandle),

                new CommandBuilder<LoginCommand>("login")
                    .Parameter("u", (cmd, value) => cmd.User = value)
                    .Build(LoginHandle),

                new CommandBuilder<LogoutCommand>("logout")
                    .Build(LogoutHandle),

                new CommandBuilder<MessageCommand>("send")
                    .Parameter("u", (cmd, value) => cmd.Target = value)
                    .Parameter("m", (cmd, value) => cmd.Message = value)
                    .Build(MessageHandle),

                new CommandBuilder<CallCommand>("call")
                    .Parameter("u", (cmd, value) => cmd.Target = value)
                    .Build(CallHandle),

                new CommandBuilder<CallInviteCommand>("invite")
                    //.Parameter("s", (cmd, value) => cmd.SessionId = int.Parse(value))
                    .Build(CallInviteHandle),

                new CommandBuilder<HangUpCommand>("hangup")
                    //.Parameter("u", (cmd, value) => cmd.SessionId = int.Parse(value))
                    .Build(CallHangUpHandle),
            };

            while (true)
            {
                var line = Console.ReadLine();

                _ = commandParser
                    .HandleAsync(line)
                    .ContinueWith(s =>
                    {
                        if (!s.IsFaulted)
                        {
                            return;
                        }

                        Console.WriteLine($"Command is failed: {s.Exception.InnerException}");
                    });
            }
        }

        #region Sockets

        static void CallReceived(byte[] bytes, ref int offset, int count)
        {
            var packet = new AudioPacket();

            while (packet.TryUnpack(bytes, ref offset, count))
            {
                if (CallSession == null)
                    continue;

                CallSession.Handle(packet);
            }
        }

        static void Send(IAudioPacket packet) 
        {
            CallSocket.Send(packet.Pack());
        }

        static void ApiReceived(byte[] bytes, ref int offset, int count)
        {
            while (MessageFactory.TryUnpack(bytes, ref offset, count, out var message))
            {
                Console.WriteLine(" < " + message.Payload);

                switch (message.Payload)
                {
                    case CallResponse response:
                        if (CallSession == null)
                        {
                            CallSession = new CallSession(response.SessionId, response.RouteId, new PcmCodec());
                            CallSession.Prepared += Send;
                        }
                        break;

                    case CallBroadcast broadcast:
                        if (CallSession == null && broadcast.State == CallState.Calling)
                        {
                            CallSessionId = broadcast.SessionId;
                            Send(new CallInviteRequest { SessionId = broadcast.SessionId, RoutePort = CallSocket.Local.Port });
                        }
                        else if (CallSession != null && CallSession.Id == broadcast.SessionId && broadcast.State == CallState.Idle)
                        {
                            CallSession.Prepared -= Send;
                            CallSession.Dispose();
                            CallSession = null;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        static void Send(IMessage message)
        {
            if (!MessageFactory.TryPack(0, message, out byte[] bytes))
                return;

            ApiSocket.Send(bytes);
        }

        #endregion Sockets

        #region Commands

        private static void ConnectionHandle(ConnectCommand command)
        {
            CallSocket.Connection(command.Address, command.Port);
            ApiSocket.Connection(command.Address, command.Port);

            if (string.IsNullOrWhiteSpace(Me))
                return;

            Send(new LoginRequest { User = Me });
        }

        private static void DisconnectHandle(DisconnectCommand command)
        {
            ApiSocket.Disconnect();
            CallSocket.Disconnect();
        }

        static void LoginHandle(LoginCommand command)
        {
            Me = command.User;
            Send(new LoginRequest { User = command.User });
        }

        static void LogoutHandle(LogoutCommand command)
        {
            Send(new LogoutRequest());
        }

        static void MessageHandle(MessageCommand command)
        {
            Send(new MessageBroadcast { Source = Me, Target = command.Target, Message = command.Message });
        }

        #endregion Commands

        #region Calls

        static void CallHandle(CallCommand command)
        {
            Send(new CallRequest { Source = Me, Target = command.Target, RoutePort = CallSocket.Local.Port });
        }

        static void CallInviteHandle(CallInviteCommand command)
        {
            Send(new CallInviteRequest { SessionId = CallSessionId/*command.SessionId*/, RoutePort = CallSocket.Local.Port });
        }

        static void CallHangUpHandle(HangUpCommand command)
        {
            Send(new CallCancelRequest { SessionId = CallSessionId/*command.SessionId*/ });
        }

        #endregion Calls
    }
}
