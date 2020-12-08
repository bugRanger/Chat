namespace Chat.Server.API
{
    using System;

    using Chat.Server.Call;

    public class CallApi : IApiModule
    {
        #region Fields

        private readonly ICoreApi _core;
        private readonly IUserContainer _users;
        private readonly ICallingController _calling;

        #endregion Fields

        #region Constructors

        public CallApi(ICoreApi core, IUserContainer users)
        {
            _core = core;
            _users = users;

            //_calling = calling;

            //_core.Registration<CallBroadcast>(HandleAuthorization);
            //_core.Registration<Broadcast>(HandleUnauthorization);
        }

        #endregion Constructors

        #region Methods

        // TODO Create or connect call session.
        // TODO Disconnect call session.

        #endregion Methods
    }
}
