namespace Chat.Server
{
    public interface IActivityWatcher
    {
        #region Properties

        uint? Interval { get; set; }

        #endregion Properties

        #region Methods

        void Start();

        void Stop();

        #endregion Methods
    }
}