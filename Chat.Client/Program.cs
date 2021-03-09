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

    using Chat.Audio;
    using Chat.Audio.Codecs;
    using Chat.Client.Call;
    using Chat.Client.Audio;

    class Program
    {
        #region Properties

        static AudioController AudioController { get; set; }

        static MessageFactory MessageFactory { get; set; }

        static EasySocket ApiSocket { get; set; }

        static EasySocket CallSocket { get; set; }

        static CallSession CallSession { get; set; }

        static int CallSessionId { get; set; }

        static string Me { get; set; }

        #endregion Properties

        static void Main(string[] args)
        {
            MessageFactory = new MessageFactory(true);

            CallSocket = new EasySocket(() => new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));
            ApiSocket = new EasySocket(() => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            ApiSocket.PreparePacket += ApiReceived;

            AudioController = new AudioController(new AudioFormat(48000, 1, 16), CallSocket, format => new PcmCodec(format));
            AudioController.Registration(format => new AudioPlayback(format));
            AudioController.Registration(format => new AudioCapture(format));

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
                            CallSessionId = response.SessionId;
                            CallSession = new CallSession(AudioController)
                            {
                                Id = response.SessionId,
                                RouteId = response.RouteId,
                            };
                            CallSession.ChangeState += ChangeState;
                        }
                        break;

                    case CallBroadcast broadcast:
                        if (CallSession == null && broadcast.State == CallState.Calling)
                        {
                            CallSessionId = broadcast.SessionId;
                            Send(new CallInviteRequest { SessionId = broadcast.SessionId, RoutePort = CallSocket.Local.Port });
                        }
                        CallSession?.RaiseState(broadcast.State);
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
            CallSession?.Dispose();
            Send(new CallCancelRequest { SessionId = CallSessionId/*command.SessionId*/ });
        }

        static void ChangeState(CallState state)
        {
            if (state != CallState.Idle || CallSession == null)
                return;

            CallSession.ChangeState -= ChangeState;
            CallSession.Dispose();
            CallSession = null;
        }

        #endregion Calls
    }
}
