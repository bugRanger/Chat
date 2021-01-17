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
        private readonly Dictionary<IUser, int> _participants;
        private bool _disposing;

        #endregion Fields

        #region Properties

        public int Id { get; set; }

        public CallState State { get; private set; }

        #endregion Properties

        #region Fields

        public event Action<ICallSession> Notify;

        #endregion Fields

        #region Constructors

        public CallSession(IAudioRouter router)
        {
            _locker = new object();
            _participants = new Dictionary<IUser, int>();

            _router = router;

            State = CallState.Created;
        }

        ~CallSession()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public int AppendOrUpdate(IUser user, int port = 0)
        {
            lock (_locker)
            {
                if (_participants.TryGetValue(user, out int routeId) && routeId != 0)
                {
                    return routeId;
                }

                if (port != 0)
                {
                    // TODO Impl check invalid params => double port for route.
                    routeId = _router.AddRoute(new IPEndPoint(user.Remote.Address, port));
                }

                _participants[user] = routeId;
                RefreshState();

                return routeId;
            }
        }

        public void Remove(IUser user)
        {
            lock (_locker)
            {
                if (!_participants.Remove(user, out int routeId))
                {
                    return;
                }

                _router.DelRoute(routeId);
                RefreshState();
            }
        }

        public bool Contains(IUser user)
        {
            return _participants.TryGetValue(user, out _);
        }

        public IEnumerable<IUser> GetParticipants()
        {
            lock (_locker)
            {
                return new List<IUser>(_participants.Keys);
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
            if (_disposing)
            {
                return;
            }

            _router.Dispose();
            _disposing = true;
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

                case CallState.Calling when _participants.Count < 2:
                case CallState.Active when _participants.Count < 2:
                    State = CallState.Idle;
                    break;

                default:
                    break;
            }
        }

        #endregion Methods
    }
}
