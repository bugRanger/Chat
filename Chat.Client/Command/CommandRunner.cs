namespace Chat.Client.Commander
{
    using System;

    public class CommandRunner
    {
        #region Properties

        public string Name { get; }

        public Action<string> Handle { get; }

        #endregion Properties

        #region Constructors

        public CommandRunner(string name, Action<string> handle)
        {
            Name = name;
            Handle = handle;
        }

        #endregion Constructors
    }
}