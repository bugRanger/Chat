namespace Chat.Client.Call
{
    using System;

    using Audio;
    using Chat.Audio;

    using Chat.Api.Messages.Call;

    class CallSession : IDisposable
    {
        #region Fields

        private readonly IAudioCodec _codec;
        private readonly IAudioController _controller;

        private bool _disposed;

        #endregion Fields

        #region Properties

        public int Id { get; set; }

        public int RouteId { get; set; }

        public CallState State { get; private set; }

        #endregion Properties

        #region Events

        public event Action Closed;

        #endregion Events

        #region Constructors

        public CallSession(IAudioController controller, Func<AudioFormat, IAudioCodec> codecFactory)
        {
            _controller = controller;
            _codec = codecFactory(controller.Format);
        }

        ~CallSession()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Methods

        public void RaiseState(CallState state) 
        {
            switch (state)
            {
                case CallState.Active:
                case CallState.Created:
                case CallState.Calling:
                    _controller.Append(RouteId, _codec);
                    break;

                case CallState.Idle:
                    _controller.Remove(RouteId);
                    Closed?.Invoke();
                    break;
            }

            State = state;
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
                _controller.Remove(RouteId);
                _codec.Dispose();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
