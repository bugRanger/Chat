namespace Chat.Server.Call
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using Chat.Api.Messages.Call;

    public class CallSession : ICallSession
    {
        #region Fields

        private readonly object _locker;
        private readonly IAudioRouter _router;
        private readonly Dictionary<IUser, int> _participants;

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
                    routeId = _router.AddRoute(new IPEndPoint(user.Remote.Address, port));
                }

                _participants[user] = routeId;
                Refresh();

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
                Refresh();
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

        public void RaiseNotify()
        {
            Notify?.Invoke(this);
        }

        private void Refresh()
        {
            switch (State)
            {
                case CallState.Created when _router.Count == 1:
                    State = CallState.Calling;
                    break;

                case CallState.Calling when _router.Count > 1:
                    State = CallState.Active;
                    break;

                case CallState.Active when _router.Count < 2:
                    State = CallState.Idle;
                    break;

                default:
                    break;
            }
        }

        #endregion Methods
    }
}
