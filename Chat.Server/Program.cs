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
            Console.WriteLine("Hello World!");

            var provider = new NetworkService();
            var watcher = new ActivityWatcher(provider, 1000);

            new ApiController(provider, new AuthorizationController());

            watcher.Start();

            // TODO Add network interfaces.
            provider
                .StartAsync(new IPEndPoint(IPAddress.Any, 30010))
                .Wait();

            Console.ReadKey();
        }
    }
}
