namespace Chat.Server.Call
{
    using System;

    public interface ICallingController
    {
        #region Events

        event Action<ICallSession> SessionChanged;

        #endregion Events

        #region Methods

        bool TryGet(int callId, out ICallSession session);

        bool TryGetOrAdd(string source, string target, out ICallSession session);

        void Close(int sessionId);

        void Disconnect(string target);

        #endregion Methods
    }
}
