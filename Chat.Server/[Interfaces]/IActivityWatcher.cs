namespace Chat.Server
{
    using System;

    public interface IActivityWatcher
    {
        public void Update(IConnection connection);
    }
}
