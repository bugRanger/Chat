namespace Chat.Server.Call_
{
    using System;
    using System.Net;

    public interface ICallRoute
    {
        IPEndPoint Remote { get; }
    }
}
