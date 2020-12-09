namespace Chat.Server.Call
{
    using System;
    using System.Net;

    using Chat.Api.Messages.Call;

    public interface ICallSession
    {
        #region Properties

        int Id { get; }

        string Source { get; }

        string Target { get; }

        CallState State { get; }

        #endregion Properties

        #region Methods

        int AddRoute(IPEndPoint iPEndPoint);

        void Open();

        void Close();

        #endregion Methods
    }
}
