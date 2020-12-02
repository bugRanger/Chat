namespace Chat.Server
{
    public interface IActivityWatcher
    {
        #region Properties

        uint? Interval { get; set; }

        #endregion Properties

        #region Methods

        public void Start();

        public void Stop();

        #endregion Methods
    }
}