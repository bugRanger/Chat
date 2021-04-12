namespace Chat.Server.Watcher
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    using Chat.Net.Socket;

    public class ActivityWatcher : IActivityWatcher
    {
        #region Constants

        private const int CHECK_INTERVAL = 100;

        #endregion Constants

        #region Fields

        private readonly ITcpСontroller _network;
        private readonly ConcurrentDictionary<IPEndPoint, long> _remoteToLastActive;

        private CancellationTokenSource _cancellationToken;

        #endregion Fields

        #region Properties

        public uint? Interval { get; set; }

        #endregion Properties

        #region Constructors

        public ActivityWatcher(ITcpСontroller network)
        {
            _remoteToLastActive = new ConcurrentDictionary<IPEndPoint, long>();

            _network = network;
            _network.ConnectionAccepted += OnConnectionAccepted;
            _network.ConnectionClosing += OnConnectionClosing;
            _network.ReceivedFrom += OnPreparePacket;
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
                        if (item.Value + Interval * TimeSpan.TicksPerMillisecond > time)
                            continue;

                        if (!_remoteToLastActive.TryRemove(item.Key, out _))
                            continue;

                        _network.Disconnect(item.Key);
                    }

                    await Task.Delay(CHECK_INTERVAL);
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

        private void OnConnectionClosing(IPEndPoint remote)
        {
            _remoteToLastActive.TryRemove(remote, out _);
        }

        private void OnPreparePacket(IPEndPoint remote, byte[] bytes, ref int offset, int count)
        {
            _remoteToLastActive[remote] = GetTime();
        }

        private long GetTime() => DateTime.Now.Ticks;

        #endregion Methods
    }
}
