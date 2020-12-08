namespace Chat.Server.Call
{
    using System;

    public interface ICallSession
    {
        #region Properties

        int CallId { get; }

        #endregion Properties
    }
}
