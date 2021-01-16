namespace Chat.Client.Commander
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class CommandParser : IEnumerable<CommandRunner>
    {
        #region Constants

        private const string SEPARATOR = " ";

        #endregion Constants

        #region Fields

        private readonly char _commandPrefix;
        private readonly Dictionary<string, CommandRunner> _commandRunners;

        #endregion Fields

        #region Constructors

        public CommandParser(char commandPrefix)
        {
            _commandRunners = new Dictionary<string, CommandRunner>();
            _commandPrefix = commandPrefix;
        }

        #endregion Constructors

        #region Methods

        public void Add(CommandRunner runner)
        {
            _commandRunners[_commandPrefix + runner.Name] = runner;
        }

        public async Task HandleAsync(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line[0] != _commandPrefix)
            {
                return;
            }

            var tokens = line.Split(SEPARATOR, 2, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return;
            }

            if (!_commandRunners.TryGetValue(tokens[0], out CommandRunner runner))
            {
                throw new NotSupportedException($"Not supported command {tokens[0]}");
            }

            var parameters = tokens.Length == 1 ? string.Empty : tokens[1];

            await Task.Run(() => runner.Handle(parameters));
        }

        public IEnumerator<CommandRunner> GetEnumerator()
        {
            return _commandRunners.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion Methods
    }
}
