namespace Chat.Server.API
{
    using System;
    using System.Net;

    using Chat.Api;
    using Chat.Api.Messages;
    using Chat.Api.Messages.Call;

    using Chat.Server.Call;

    //	+--------+			+--------+			+--------+
    //	| Source |			| Server |			| Target |
    //	+--------+			+--------+			+--------+
    //		|					|					|
    //		|----{request}----->|					|
    //		|<---{response}-----|					|
    //		|					|	  Calling		|
    //		|	  Calling		|----{broadcast}--->|
    //		|<---{broadcast}----|					|
    //		|					|					|
    //		|					|<---{request}------|
    //		|					|----{response}---->|
    //		|					|					|
    //		|					|	   Active		|
    //		|	   Active		|----{broadcast}--->|
    //		|<---{broadcast}----|					|
    //		|					|					|
    //		|-----{reject}----->|					|
    //		|<---{response}-----|					|
    //		|					|		Idle		|
    //		|		Idle		|----{broadcast}--->|
    //		|<---{broadcast}----|					|
    //		|					|					|
    //		|					|					|


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

            _core.Registration<CallRequest>(HandleCall);
            _core.Registration<CallRejectRequest>(HandleCallReject);
        }

        #endregion Constructors

        #region Methods

        private void HandleCall(IPEndPoint remote, int index, CallRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            // TODO Impl logic.

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            // TODO Impl send.
        }

        private void HandleCallReject(IPEndPoint remote, int index, CallRejectRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            // TODO Impl logic.

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            // TODO Impl send.
        }

        #endregion Methods
    }
}
