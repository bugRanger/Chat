namespace Chat.Server.API
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Collections.Generic;

    using Chat.Api;
    using Chat.Api.Messages;
    using Chat.Api.Messages.Auth;

    public class AuthApi : IApiModule
    {
        #region Fields

        private readonly ICoreApi _core;
        private readonly IAuthorizationController _authorization;

        #endregion Fields

        #region Constructors

        public AuthApi(ICoreApi core, IAuthorizationController authorization)
        {
            _authorization = authorization;
            _core = core;
            _core.ConnectionClosing += OnConnectionClosing;

            _core.Registration(this);
            _core.Registration<AuthorizationRequest>(HandleAuthorization);
            _core.Registration<UnauthorizationRequest>(HandleUnauthorization);
        }

        #endregion Constructors

        #region Methods

        private void HandleAuthorization(IPEndPoint remote, int index, AuthorizationRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            IUser user = null;
            IEnumerable<IUser> users = null;
            IEnumerable<IPEndPoint> remotes = null;

            if (_authorization.TryGet(request.User, out _) 
                || (_authorization.TryGet(remote, out user) && user.Name == request.User))
            {
                status = StatusCode.AuthDuplicate;
                reason = "User exists";
            }
            else
            {
                users = _authorization.GetUsers().Where(s => s.Remote != remote);
                remotes = users.Select(s => s.Remote);

                _authorization.AddOrUpdate(remote, s =>
                {
                    s.Remote = remote;
                    s.Name = request.User;

                    user = s;
                });
            }

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            _core.Send(new UsersBroadcast { Users = users.Select(GetUserDetail).ToArray() }, remote);
            _core.Send(new UsersBroadcast { Users = new[] { GetUserDetail(user) } }, remotes.ToArray());
        }

        private void HandleUnauthorization(IPEndPoint remote, int index, UnauthorizationRequest request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_authorization.TryRemove(remote, out IUser user))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }

            _core.Send(new MessageResult { Status = status, Reason = reason }, remote, index);
            if (status != StatusCode.Success)
            {
                return;
            }

            _core.Disconnect(remote);

            var remotes = _authorization
                .GetUsers()
                .Select(s => s.Remote)
                .ToArray();

            _core.Send(new UserOfflineBroadcast { User = user.Name }, remotes);
        }

        private void OnConnectionClosing(IPEndPoint remote, bool inactive)
        {
            if (!_authorization.TryRemove(remote, out IUser user))
            {
                return;
            }

            var remotes = _authorization
                .GetUsers()
                .Select(s => s.Remote)
                .ToArray();

            _core.Send(new UserOfflineBroadcast { User = user.Name }, remotes);
        }

        // TODO send User detail.
        private string GetUserDetail(IUser user)
        {
            return user.Name;
        }

        #endregion Methods
    }
}
