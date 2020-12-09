namespace Chat.Api
{
    using System;

    public enum StatusCode
    {
        Success = 0,
        Failure,

        UnknownMessage,

        UserNotFound,

        AuthDuplicate,
        NotAuthorized,

        CallNotFound,

        CallDuplicate,
    }
}
