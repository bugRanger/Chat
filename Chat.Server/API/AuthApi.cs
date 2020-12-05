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

            _core.Registration<AuthorizationBroadcast>(HandleAuthorization);
            _core.Registration<UnauthorizationBroadcast>(HandleUnauthorization);
        }

        #endregion Constructors

        #region Methods

        private void HandleAuthorization(IPEndPoint remote, int index, AuthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            IUser user = null;
            IUser[] users = null;
            IPEndPoint[] remotes = null;

            if (_authorization.TryGet(request.User, out _) 
                || (_authorization.TryGet(remote, out user) && user.Name == request.User))
            {
                status = StatusCode.AuthDuplicate;
                reason = "User exists";
            }
            else
            {
                users = _authorization.GetUsers(s => s.Remote != remote);
                remotes = users
                    .Select(s => s.Remote)
                    .ToArray();

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
            _core.Send(new UsersBroadcast { Users = new[] { GetUserDetail(user) } }, remotes);
        }

        private void HandleUnauthorization(IPEndPoint remote, int index, UnauthorizationBroadcast request)
        {
            var status = StatusCode.Success;
            var reason = string.Empty;

            if (!_authorization.TryGet(remote, out _))
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
