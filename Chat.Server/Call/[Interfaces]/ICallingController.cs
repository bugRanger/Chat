namespace Chat.Server.Call
{
    using System;
    using Chat.Server.Auth;

    public interface ICallingController
    {
        #region Events

        event Action<ICallSession> SessionChanged;

        #endregion Events

        #region Methods

        bool TryGet(int sessionId, out ICallSession session);

        bool TryGetOrAdd(string source, string target, out ICallSession session);

        void Disconnect(IUser user);

        #endregion Methods
    }
}
