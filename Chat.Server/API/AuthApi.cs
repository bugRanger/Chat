namespace Chat.Server.API
{
    using System;
    using System.Net;
    using System.Linq;

    using Chat.Api;
    using Chat.Api.Messages;

    public class AuthApi : IApiModule
    {
        #region Fields

        private readonly ICoreApi _core;
        private readonly IAuthorizationController _authorization;

        #endregion Fields

        #region Constructors

        public AuthApi(ICoreApi core, IAuthorizationController authorization)
        {
            _core = core;
            _authorization = authorization;

            _core.Append(this);
            _core.Registration<AuthorizationBroadcast>(HandleAuthorization);
            _core.Registration<UnauthorizationBroadcast>(HandleUnauthorization);
        }

        #endregion Constructors

        #region Methods

        private void HandleAuthorization(IPEndPoint remote, AuthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            IUser[] users = null;
            IPEndPoint[] remotes = null;

            if (_authorization.TryGet(remote, out _))
            {
                status = StatusCode.AuthDuplicate;
                reason = "User exists";
            }
            else
            {
                users = _authorization.GetUsers();
                remotes = users
                    .Select(s => s.Remote)
                    .ToArray();

                _authorization.AddOrUpdate(remote, s =>
                {
                    s.Remote = remote;
                    s.Name = request.User;
                });
            }

            _core.Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            _core.Send(new UsersBroadcast { Users = users.Select(GetUserDetail).ToArray() }, remote);
            _core.Send(request, remotes);
        }

        private void HandleUnauthorization(IPEndPoint remote, UnauthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_authorization.TryGet(remote, out _))
            {
                status = StatusCode.NotAuthorized;
                reason = "User is not logged in";
            }

            if (status == StatusCode.Success)
            {
                _core.Disconnect(remote);
            }

            _core.Send(new MessageResponse { Status = status, Reason = reason }, remote);
            if (status != StatusCode.Success)
            {
                return;
            }

            var remotes = _authorization
                .GetUsers()
                .Select(s => s.Remote)
                .ToArray();

            _core.Send(request, remotes);
        }

        // TODO send User detail.
        private string GetUserDetail(IUser user)
        {
            return user.Name;
        }

        #endregion Methods
    }
}
