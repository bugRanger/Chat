namespace Chat.Server
{
    using System;
    using System.Net;

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
            Console.WriteLine("Press key:\r\n S - stop\r\n Q - exit");

            var tcpProvider = new TcpProvider(NetworkSocket.Create);
            var udpProvider = new UdpProvider(NetworkSocket.Create);

            var watcher = new ActivityWatcher(tcpProvider)
            {
                Interval = 15000,
            };

            // TODO Impl udp transport layer.
            var audioProvider = new AudioProvider(udpProvider);
            var calls = new CallController((container) => new BridgeRouter(container, audioProvider));
            var authorization = new AuthorizationController();

            var core = new CoreApi(tcpProvider);

            new AuthApi(core, authorization);
            new TextApi(core, authorization);
            new CallApi(core, authorization, calls);

            watcher.Start();

            // TODO Add network interfaces.
            _ = tcpProvider.StartAsync(new IPEndPoint(IPAddress.Any, 30010));
            _ = udpProvider.StartAsync(new IPEndPoint(IPAddress.Any, 30010));

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
