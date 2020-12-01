using Chat.Api;
using System;
using System.Net;

namespace Chat.Server
{
    class Program
    {
        private static NetworkService _provider;
        private static ActivityWatcher _watcher;
        private static CoreApi _coreApi;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            _provider = new NetworkService();
            _provider.PreparePacket += HandleMessage;
            _provider.ConnectionAccepted += HandleConnection;

            _coreApi = new CoreApi(_provider);
            _watcher = new ActivityWatcher(_provider, 10000);

            // TODO Add network interfaces.
            _provider.Start(null);

            Console.ReadKey();
        }

        private static bool HandleMessage(IPEndPoint remote, byte[] bytes, ref int offset, int count)
        {
            if (!PacketFactory.TryUnpack(bytes, ref offset, count, out var request))
            {
                return false;
            }

            _watcher.Update(remote);
            _coreApi.Handle(remote, (IMessage)request.Payload);

            return true;
        }

        private static void HandleConnection(IPEndPoint remote)
        {
            _watcher.Update(remote);
        }
    }
}
