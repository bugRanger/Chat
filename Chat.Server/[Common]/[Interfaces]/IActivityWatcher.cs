namespace Chat.Server
{
    using System;
    using System.Net;

    public interface IActivityWatcher
    {
        public void Update(IPEndPoint remote);
    }
}
