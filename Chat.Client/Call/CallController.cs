namespace Chat.Client.Call
{
    using System;
    using System.Collections.Generic;

    using Audio;

    using Chat.Audio;
    using Chat.Api.Messages.Call;

    public class CallController : IDisposable
    {
        #region Fields

        private readonly object _locker;

        private readonly Dictionary<int, CallSession> _sessions;
        private readonly AudioController _audioController;

        private bool _disposed;

        #endregion Fields

        #region Constructors

        public CallController(AudioController audioController)
        {
            _locker = new object();
            _sessions = new Dictionary<int, CallSession>();

            _audioController = audioController;
        }

        ~CallController()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public bool TryGet(int sessionId, out CallSession callSession)
        {
            lock (_locker) 
            {
                return _sessions.TryGetValue(sessionId, out callSession);
            }
        }

        public void Append(int sessionId, int routeId)
        {
            lock (_locker)
            {
                var session = new CallSession(_audioController)
                {
                    Id = sessionId,
                    RouteId = routeId,
                };
                session.ChangeState += ChangeState;

                _sessions[sessionId] = session;
            }
        }

        public bool Remove(int sessionId)
        {
            lock (_locker)
            {
                if (!_sessions.Remove(sessionId, out var callSession))
                    return false;

                callSession.ChangeState -= ChangeState;
                callSession.Dispose();

                return true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ChangeState(int sessionId, CallState state)
        {
            if (state != CallState.Idle)
                return;

            Remove(sessionId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                lock (_locker)
                {
                    _sessions.Clear();
                }
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
