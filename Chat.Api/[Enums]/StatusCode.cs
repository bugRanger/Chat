namespace Chat.Api
{
    using System;

    public enum StatusCode
    {
        Success = 0,
        // TODO Add type errors.
        Failure,
        UserNotFound,
        AuthDuplicate,
    }
}
