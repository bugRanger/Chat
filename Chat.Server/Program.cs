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
        private static NetworkService _provider;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            _provider = new NetworkService();

            new ApiController(_provider, new AuthorizationController());
            new ActivityWatcher(_provider, 10000);

            // TODO Add network interfaces.
            _provider.Start(null);

            Console.ReadKey();
        }
    }
}
