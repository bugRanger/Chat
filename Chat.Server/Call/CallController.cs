namespace Chat.Server.Call
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;

    public class CallController : ICallingController
    {
        #region Fields

        private readonly INetworkСontroller _network;
        private readonly ConcurrentDictionary<int, ICallSession> _sessions;

        #endregion Fields

        #region Events

        public event Action<ICallSession> SessionChanged;

        #endregion Events

        #region Constructors

        public CallController(INetworkСontroller network)
        {
            _network = network;
            _sessions = new ConcurrentDictionary<int, ICallSession>();
        }

        #endregion Constructors

        #region Methods

        public bool TryGetOrAdd(string source, string target, out ICallSession session)
        {
            throw new NotImplementedException();
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

        #endregion Methods
    }
}
