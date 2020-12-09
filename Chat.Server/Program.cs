namespace Chat.Server
{
    using System;
    using System.Net;

    using Chat.Server.API;
    using Chat.Server.Auth;
    using Chat.Server.Network;
    using Chat.Server.Watcher;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press key:\r\n S - stop\r\n Q - exit");

            var provider = new TcpProvider(NetworkSocket.Create);

            var watcher = new ActivityWatcher(provider)
            {
                Interval = 15000,
            };

            var authorization = new AuthorizationController();

            var core = new CoreApi(provider);

            new AuthApi(core, authorization);
            new TextApi(core, authorization);
            new CallApi(core, null);

            watcher.Start();

            // TODO Add network interfaces.
            _ = provider.StartAsync(new IPEndPoint(IPAddress.Any, 30010));

            while (true)
            {
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        return;

                    case ConsoleKey.S:
                        provider.Stop();
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
