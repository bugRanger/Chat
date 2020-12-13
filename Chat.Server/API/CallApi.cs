namespace Chat.Server.API
{
    using System;
    using System.Linq;
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
            _core.ConnectionClosing += OnConnectionClosing;
            _users = users;
            _callController = calling;
            _callController.SessionChanged += OnCallSessionChanged;

            _core.Registration(this);
            _core.Registration<CallRequest>(HandleCall);
            _core.Registration<CallInviteRequest>(HandleCallInvite);
            _core.Registration<CallCancelRequest>(HandleCallCancel);
        }

        #endregion Constructors

        #region Methods

        private void HandleCall(IPEndPoint remote, int index, CallRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            ICallSession session = null;

            if (!_users.TryGet(remote, out IUser source))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }
            else if (!_callController.TryGetOrAdd(source.Name, request.Target, out session))
            {
                status = StatusCode.CallDuplicate;
                reason = "Call exists";
            }

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            var routeId = session.AppendOrUpdate(source, request.RoutePort);
            if (_users.TryGet(request.Target, out IUser target))
            {
                session.AppendOrUpdate(target);
            }

            _core.Send(new CallResponse { SessionId = session.Id, RouteId = routeId }, remote, index);

            session.RaiseNotify();
        }

        private void HandleCallInvite(IPEndPoint remote, int index, CallInviteRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            ICallSession session = null;

            if (!_users.TryGet(remote, out IUser source))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }
            else if (!_callController.TryGet(request.SessionId, out session))
            {
                status = StatusCode.CallNotFound;
                reason = "Call not found";
            }
            // TODO Impl test.
            else if (!session.Contains(source))
            {
                status = StatusCode.UserNotFound;
                reason = "User not found in call session";
            }

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            var routeId = session.AppendOrUpdate(source, request.RoutePort);
            _core.Send(new CallResponse { SessionId = session.Id, RouteId = routeId }, remote, index);

            session.RaiseNotify();
        }

        private void HandleCallCancel(IPEndPoint remote, int index, CallCancelRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            ICallSession session = null;

            if (!_users.TryGet(remote, out IUser source))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }
            else if (!_callController.TryGet(request.SessionId, out session))
            {
                status = StatusCode.CallNotFound;
                reason = "Call not found";
            }
            else if (!session.Contains(source))
            {
                status = StatusCode.UserNotFound;
                reason = "User not found in call session";
            }

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            session.Remove(source);
            session.RaiseNotify();
        }

        private void OnConnectionClosing(IPEndPoint remote, bool inactive)
        {
            if (!_users.TryGet(remote, out IUser target))
            {
                return;
            }

            _callController.Disconnect(target);
        }

        private void OnCallSessionChanged(ICallSession session)
        {
            var participants = session.GetParticipants();
            if (participants.Count() == 0)
            {
                return;
            }

            _core.Send(new CallBroadcast
            {
                SessionId = session.Id,
                Participants = participants.Select(s => s.Name).ToArray(),
                State = session.State
            },
            participants.Select(s => s.Remote).ToArray());
        }

        #endregion Methods
    }
}
