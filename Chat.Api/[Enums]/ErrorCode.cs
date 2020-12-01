namespace Chat.Api
{
    using System;

    public enum ErrorCode
    {
        Success = 0,
        // TODO Add type errors.
        Failure,
        UserNotFound,
        AuthDuplicate,
    }
}
