namespace Chat.Server.Audio
{
    using System;
    using System.Net;

    public class GroupRouter : IAudioRouter
    {
        #region Fields

        private bool _disposed;

        #endregion Fields

        #region Properties

        public int Count => throw new NotImplementedException();

        #endregion Properties

        #region Methods

        public bool Contains(IPEndPoint route)
        {
            throw new NotImplementedException();
        }

        public void Append(IPEndPoint route)
        {
            throw new NotImplementedException();
        }

        public void Remove(IPEndPoint route)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // TODO: Impl.
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
