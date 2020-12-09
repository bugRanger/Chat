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
        private readonly ICallingController _callController;

        #endregion Fields

        #region Constructors

        public CallApi(ICoreApi core, IUserContainer users, ICallingController calling)
        {
            _core = core;
            _users = users;
            _callController = calling;
            _callController.SessionChanged += OnCallSessionChanged;

            _core.Registration(this);
            _core.Registration<CallRequest>(HandleCall);
            _core.Registration<CallRejectRequest>(HandleCallReject);
        }

        #endregion Constructors

        #region Methods

        private void HandleCall(IPEndPoint remote, int index, CallRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_users.TryGet(remote, out IUser source))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }

            request.Source = source.Name;

            if (_callController.TryGetOrAdd(request.Source, request.Target, out ICallSession session) && session.Source == request.Source)
            {
                status = StatusCode.CallDuplicate;
                reason = "Call exists";
            }

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            var routeId = session.AddRoute(new IPEndPoint(remote.Address, request.MediaPort));

            _core.Send(new CallResponse { CallId = session.Id, MediaId = routeId }, remote);

            session.Open();
        }

        private void HandleCallReject(IPEndPoint remote, int index, CallRejectRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_callController.TryGet(request.CallId, out ICallSession session))
            {
                status = StatusCode.CallNotFound;
                reason = "Call not found";
            }

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            session.Close();
        }

        private void OnCallSessionChanged(ICallSession session)
        {
            _users.TryGet(session.Source, out IUser source);
            _users.TryGet(session.Target, out IUser target);

            if (source == null && 
                target == null)
                return;

            _core.Send(new CallBroadcast
            {
                CallId = session.Id,
                Source = session.Source,
                Target = session.Target,
                State = session.State
            },
            source.Remote,
            target.Remote);
        }

        #endregion Methods
    }
}
