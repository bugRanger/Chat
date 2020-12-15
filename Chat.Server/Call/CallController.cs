namespace Chat.Server.Call
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Concurrent;
    using System.Security.Cryptography;

    using Chat.Api.Messages.Call;
    using Chat.Server.Audio;

    public class CallController : ICallingController
    {
        #region Fields

        private readonly Func<KeyContainer, IAudioRouter> _routerFactory;
        private readonly ConcurrentDictionary<int, ICallSession> _sessions;
        private readonly KeyContainer _container;

        #endregion Fields

        #region Events

        public event Action<ICallSession> SessionChanged;

        #endregion Events

        #region Constructors

        public CallController(Func<KeyContainer, IAudioRouter> routerFactory)
        {
            _routerFactory = routerFactory;

            _container = new KeyContainer();
            _sessions = new ConcurrentDictionary<int, ICallSession>();
        }

        #endregion Constructors

        #region Methods

        public bool TryGetOrAdd(string source, string target, out ICallSession session)
        {
            int sessionId = GetSessionId(source, target);

            if (_sessions.TryGetValue(sessionId, out session) ||
                _sessions.TryGetValue(GetSessionId(target, source), out session))
            {
                return false;
            }

            var router = _routerFactory(_container);
            session = new CallSession(router)
            {
                Id = sessionId,
            };
            session.Notify += OnSessionNotify;

            _sessions[sessionId] = session;

            return true;
        }

        public bool TryGet(int sessionId, out ICallSession session)
        {
            return _sessions.TryGetValue(sessionId, out session);
        }

        public void Disconnect(IUser user)
        {
            var sessions = _sessions.Values.ToArray();
            foreach (var session in sessions)
            {
                if (!session.Contains(user))
                {
                    continue;
                }

                session.Remove(user);
            }
        }

        private int GetSessionId(string source, string target)
        {
            using var sha = MD5.Create();

            int hash = 17;
            hash = hash * 31 + BitConverter.ToInt32(sha.ComputeHash(Encoding.UTF8.GetBytes(source)), 0);
            hash = hash * 31 + BitConverter.ToInt32(sha.ComputeHash(Encoding.UTF8.GetBytes(target)), 0);

            return hash;
        }

        private void OnSessionNotify(ICallSession session) 
        {
            if (session.State == CallState.Idle)
            {
                _sessions.TryRemove(session.Id, out _);
            }

            SessionChanged?.Invoke(session);
        }

        #endregion Methods
    }
}
