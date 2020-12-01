using Chat.Api;
using System;

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

            _coreApi = new CoreApi();
            _watcher = new ActivityWatcher(10000);
            _provider = new NetworkService();
            _provider.PreparePacket += HandleMessage;
            _provider.ConnectionAccepted += HandleConnection;

            _provider.Start(null);

            Console.ReadKey();
        }

        private static bool HandleMessage(IConnection connection, byte[] bytes, ref int offset, int count)
        {
            if (!PacketFactory.TryUnpack(bytes, ref offset, count, out var request))
            {
                return false;
            }

            _watcher.Update(connection);
            _coreApi.Handle(connection, (IMessage)request.Payload);

            return true;
        }

        private static void HandleConnection(IConnection connection)
        {
            _watcher.Update(connection);
        }
    }
}
