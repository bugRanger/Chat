namespace Chat.Server.Call
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using Chat.Api.Messages.Call;

    public interface ICallSession
    {
        #region Properties

        int Id { get; }

        CallState State { get; }

        #endregion Properties

        #region Methods

        int AppendOrUpdate(IUser user, int port = 0);

        void Remove(IUser user);

        bool Contains(IUser user);

        IEnumerable<IUser> GetParticipants();

        void RaiseNotify();

        #endregion Methods
    }
}
