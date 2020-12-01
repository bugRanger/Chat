namespace Chat.Server
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
        private const int ENABLED = 1;
        private const int DISABLE = 0;

        #endregion Constants

        #region Fields

        private readonly ConcurrentDictionary<IPEndPoint, long> _remoteToLastActive;
        private readonly long _interval;

        private CancellationTokenSource _cancellationToken;
        private int _active;

        #endregion Fields

        #region Constructors

        public ActivityWatcher(long interval)
        {
            _remoteToLastActive = new ConcurrentDictionary<IPEndPoint, long>();

            _interval = interval;
            _active = DISABLE;
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
                            item.Key.Disconnect(true);
                        }
                        finally
                        {
                            _remoteToLastActive.TryRemove(item.Key, out _);
                        }
                    }

                    Task.Delay(CHECK_INTERVAL);
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

        public void Update(IPEndPoint remote)
        {
            _remoteToLastActive[remote] = GetTime();
        }

        private long GetTime() => DateTime.Now.ToFileTimeUtc();

        #endregion Methods
    }
}
