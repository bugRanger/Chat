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
    using Chat.Net.Socket;

    class Program
    {
        #region Properties

        static CallController CallController { get; set; } 

        static AudioController AudioController { get; set; }

        static AudioCapture AudioCapture { get; set; }

        static MessageFactory MessageFactory { get; set; }

        static ClientSocket ApiSocket { get; set; }

        static ClientSocket CallSocket { get; set; }

        static string Me { get; set; }

        [Obsolete("Removed")]
        static int SessionId { get; set; }

        #endregion Properties

        static void Main(string[] args)
        {
            MessageFactory = new MessageFactory(true);

            CallSocket = new ClientSocket(() => NetworkSocket.Create(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));
            ApiSocket = new ClientSocket(() => NetworkSocket.Create(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            ApiSocket.Received += ApiReceived;

            AudioController = new AudioController(new AudioFormat(), CallSocket, format => new PcmCodec(format));
            AudioController.Registration(format => new AudioPlayback(format));
            AudioController.Registration(format => AudioCapture = new AudioCapture(format));

            CallController = new CallController(AudioController);

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
                    .Parameter("s", (cmd, value) => cmd.SessionId = int.Parse(value))
                    .Build(CallInviteHandle),

                new CommandBuilder<HangUpCommand>("hangup")
                    .Parameter("s", (cmd, value) => cmd.SessionId = int.Parse(value))
                    .Build(CallHangUpHandle),

                new CommandBuilder<MuteCommand>("mute")
                    .Parameter("r", (cmd, value) => cmd.RouteId = int.Parse(value))
                    .Build(MuteHandle),

                new CommandBuilder<UnmuteCommand>("unmute")
                    .Parameter("r", (cmd, value) => cmd.RouteId = int.Parse(value))
                    .Build(UnMuteHandle),
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
                        if (!CallController.TryGet(response.SessionId, out _))
                        {
                            SessionId = response.SessionId;

                            CallController.Append(response.SessionId, response.RouteId);
                            Console.WriteLine($" < Call routeID: {response.RouteId}");
                        }
                        break;

                    case CallBroadcast broadcast:
                        if (!CallController.TryGet(broadcast.SessionId, out var callSession) && broadcast.State == CallState.Calling)
                        {
                            SessionId = broadcast.SessionId;

                            Send(new CallInviteRequest { SessionId = broadcast.SessionId, RoutePort = CallSocket.Local.Port });
                            Console.WriteLine($" < Call sessionID: {broadcast.SessionId}");
                        }
                        callSession?.RaiseState(broadcast.State);
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

        static void ConnectionHandle(ConnectCommand command)
        {
            CallSocket.Connection(new IPEndPoint(command.Address, command.Port));
            ApiSocket.Connection(new IPEndPoint(command.Address, command.Port));

            if (string.IsNullOrWhiteSpace(Me))
                return;

            Send(new LoginRequest { User = Me });
        }

        static void DisconnectHandle(DisconnectCommand command)
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
            command.SessionId = SessionId;

            Send(new CallInviteRequest { SessionId = command.SessionId, RoutePort = CallSocket.Local.Port });
        }

        static void CallHangUpHandle(HangUpCommand command)
        {
            command.SessionId = SessionId;

            if (!CallController.Remove(command.SessionId))
                return;

            Send(new CallCancelRequest { SessionId = command.SessionId });
        }

        static void MuteHandle(MuteCommand command)
        {
            if (!AudioController.TryGet(command.RouteId, out IAudioStream stream))
            {
                return;
            }

            AudioCapture.Remove(stream);
        }

        static void UnMuteHandle(UnmuteCommand command)
        {
            if (!AudioController.TryGet(command.RouteId, out IAudioStream stream))
            {
                return;
            }

            AudioCapture.Append(stream);
        }

        #endregion Calls
    }
}
