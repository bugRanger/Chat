namespace Chat.Server.Call
{
    using System;
    using System.Collections.Generic;

    using Chat.Api.Messages.Call;

    using Chat.Server.Auth;

    public interface ICallSession : IDisposable
    {
        #region Properties

        int Id { get; }

        int RouteId { get; }

        CallState State { get; }

        #endregion Properties

        #region Fields

        event Action<ICallSession> Notify;

        #endregion Fields

        #region Methods

        void AppendOrUpdate(IUser user, int port = 0);

        void Remove(IUser user);

        bool Contains(IUser user);

        IEnumerable<IUser> GetParticipants();

        void RaiseState();

        #endregion Methods
    }
}
