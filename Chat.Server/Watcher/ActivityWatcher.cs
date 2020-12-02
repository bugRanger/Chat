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

        private const int ENABLED = 1;
        private const int DISABLE = 0;

        #endregion Constants

        #region Fields

        private readonly INetworkСontroller _network;
        private readonly ConcurrentDictionary<IPEndPoint, long> _remoteToLastActive;
        private readonly long _interval;

        private CancellationTokenSource _cancellationToken;
        private int _active;

        #endregion Fields

        #region Properties

        public uint Interval { get; set; }

        #endregion Properties

        #region Constructors

        public ActivityWatcher(INetworkСontroller network, long interval)
        {
            _remoteToLastActive = new ConcurrentDictionary<IPEndPoint, long>();

            _network = network;
            _network.PreparePacket += OnPreparePacket;
            _network.ConnectionClosing += OnConnectionClosing;

            _interval = interval;
            _active = DISABLE;
        }

        private bool OnPreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count)
        {
            _remoteToLastActive[remote] = GetTime();
            return true;
        }

        #endregion Constructors

        #region Methods

        public async void Start()
        {
            if (Interlocked.CompareExchange(ref _active, ENABLED, DISABLE) == ENABLED)
                return;

            _cancellationToken = new CancellationTokenSource();

            var token = _cancellationToken.Token;
            await Task.Run(() =>
            {
                while (token.IsCancellationRequested)
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

                    Task.Delay((int)Interval);
                }
            },
            token);
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _active, DISABLE, ENABLED) == DISABLE)
                return;

            _cancellationToken.Cancel();
        }

        private void OnConnectionClosing(IPEndPoint remote, bool inactive)
        {
            if (inactive)
                return;

            _remoteToLastActive.TryRemove(remote, out _);
        }

        private long GetTime() => DateTime.Now.ToFileTimeUtc();

        #endregion Methods
    }
}
