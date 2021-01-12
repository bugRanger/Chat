namespace Chat.Server.Call
{
    using System;
    using System.Collections.Generic;

    using Chat.Api.Messages.Call;

    using Chat.Server.Auth;

    public interface ICallSession
    {
        #region Properties

        int Id { get; }

        CallState State { get; }

        #endregion Properties

        #region Fields

        event Action<ICallSession> Notify;

        #endregion Fields

        #region Methods

        int AppendOrUpdate(IUser user, int port = 0);

        void Remove(IUser user);

        bool Contains(IUser user);

        IEnumerable<IUser> GetParticipants();

        void RaiseState();

        #endregion Methods
    }
}
