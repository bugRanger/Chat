namespace Chat.Server
{
    using System;
    using System.Net;

    using NLog;

    using Chat.Api;

    using Chat.Server.API;
    using Chat.Server.Auth;
    using Chat.Server.Call;
    using Chat.Server.Audio;
    using Chat.Server.Network;
    using Chat.Server.Watcher;

    class Program
    {
        static void Main(string[] args)
        {
            LogManager.Configuration ??= new NLog.Config.LoggingConfiguration();

            var tcpProvider = new TcpProvider(NetworkSocket.Create);
            var udpProvider = new UdpProvider(NetworkSocket.Create);

            var watcher = new ActivityWatcher(tcpProvider)
            {
                Interval = 15000,
            };

            // TODO Impl udp transport layer.
            var provider = new AudioProvider(udpProvider);
            var calls = new CallController(new KeyContainer(), () => new BridgeRouter(provider));
            var authorization = new AuthorizationController();

            var core = new CoreApi(tcpProvider, new MessageFactory(true));

            new AuthApi(core, authorization);
            new TextApi(core, authorization);
            new CallApi(core, authorization, calls);

            // TODO Impl ping message.
            //watcher.Start();

            // TODO Add network interfaces.
            _ = tcpProvider.StartAsync(new IPEndPoint(IPAddress.Any, 30010));
            _ = udpProvider.StartAsync(new IPEndPoint(IPAddress.Any, 30010));

            Console.WriteLine("Press key:\r\n S - stop\r\n Q - exit");

            while (true)
            {
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        return;

                    case ConsoleKey.S:
                        tcpProvider.Stop();
                        udpProvider.Stop();
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
