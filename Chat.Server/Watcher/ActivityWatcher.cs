namespace Chat.Server.Watcher
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    public class ActivityWatcher : IActivityWatcher
    {
        #region Constants

        private const int CHECK_INTERVAL = 100;

        #endregion Constants

        #region Fields

        private readonly INetworkСontroller _network;
        private readonly ConcurrentDictionary<IPEndPoint, long> _remoteToLastActive;
        private readonly long _interval;

        private CancellationTokenSource _cancellationToken;

        #endregion Fields

        #region Properties

        public uint? Interval { get; set; }

        #endregion Properties

        #region Constructors

        public ActivityWatcher(INetworkСontroller network)
        {
            _remoteToLastActive = new ConcurrentDictionary<IPEndPoint, long>();

            _network = network;
            _network.ConnectionAccepted += OnConnectionAccepted;
            _network.ConnectionClosing += OnConnectionClosing;
            _network.PreparePacket += OnPreparePacket;
        }

        #endregion Constructors

        #region Methods

        public void Start() 
        {
            _ = StartAsync();
        }

        public async Task StartAsync()
        {
            _cancellationToken = new CancellationTokenSource();

            var token = _cancellationToken.Token;
            await Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var time = GetTime();
                    var connections = _remoteToLastActive.ToDictionary(k => k.Key, v => v.Value);

                    foreach (var item in connections)
                    {
                        if (item.Value + _interval > time)
                            continue;

                        try
                        {
                            _network.Disconnect(item.Key, true);
                        }
                        finally
                        {
                            _remoteToLastActive.TryRemove(item.Key, out _);
                        }
                    }

                    await Task.Delay((int)(Interval ?? CHECK_INTERVAL));
                }
            },
            token);
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
        }

        private void OnConnectionAccepted(IPEndPoint remote)
        {
            _remoteToLastActive[remote] = GetTime();
        }

        private void OnConnectionClosing(IPEndPoint remote, bool inactive)
        {
            if (inactive)
                return;

            _remoteToLastActive.TryRemove(remote, out _);
        }

        private void OnPreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count)
        {
            OnConnectionAccepted(remote);
        }

        private long GetTime() => DateTime.Now.ToFileTimeUtc();

        #endregion Methods
    }
}
