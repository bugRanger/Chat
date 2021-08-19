namespace Chat.Server.Call
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using Chat.Api.Messages.Call;

    using Chat.Server.Audio;
    using Chat.Server.Auth;

    public class CallSession : ICallSession
    {
        #region Fields

        private readonly object _locker;
        private readonly IAudioRouter _router;
        private readonly Dictionary<IUser, int> _userToPort;

        private bool _disposed;

        #endregion Fields

        #region Properties

        public int Id { get; set; }

        public int RouteId { get; set; }

        public CallState State { get; private set; }

        #endregion Properties

        #region Fields

        public event Action<ICallSession> Notify;

        #endregion Fields

        #region Constructors

        public CallSession(IAudioRouter router)
        {
            _locker = new object();
            _userToPort = new Dictionary<IUser, int>();

            _router = router;

            State = CallState.Created;
        }

        ~CallSession()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public void AppendOrUpdate(IUser user, int port = 0)
        {
            lock (_locker)
            {
                if (port != 0)
                {
                    if (_userToPort.TryGetValue(user, out int routePort) && routePort != 0)
                    {
                        _router.Remove(new IPEndPoint(user.Remote.Address, routePort));
                    }

                    _router.Append(new IPEndPoint(user.Remote.Address, port), RouteId);
                }

                _userToPort[user] = port;
                RefreshState();
            }
        }

        public void Remove(IUser user)
        {
            lock (_locker)
            {
                if (!_userToPort.Remove(user, out int remotePort))
                {
                    return;
                }

                _router.Remove(new IPEndPoint(user.Remote.Address, remotePort));
                RefreshState();
            }
        }

        public bool Contains(IUser user)
        {
            return _userToPort.TryGetValue(user, out _);
        }

        public IEnumerable<IUser> GetParticipants()
        {
            lock (_locker)
            {
                return new List<IUser>(_userToPort.Keys);
            }
        }

        public void RaiseState()
        {
            Notify?.Invoke(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _userToPort.Clear();
                _router.Dispose();
            }

            _disposed = true;
        }

        private void RefreshState()
        {
            switch (State)
            {
                case CallState.Created:
                    State = CallState.Calling;
                    break;

                case CallState.Calling when _router.Count > 1:
                    State = CallState.Active;
                    break;

                case CallState.Calling when _userToPort.Count < 2:
                case CallState.Active when _userToPort.Count < 2:
                    State = CallState.Idle;
                    break;

                default:
                    break;
            }
        }

        #endregion Methods
    }
}
