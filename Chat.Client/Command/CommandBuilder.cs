namespace Chat.Client.Commander.Commands
{
    using System;
    using System.Collections.Generic;

    public class CommandBuilder<T> 
        where T : ICommand, new ()
    {
        #region Fields

        private readonly Dictionary<string, Action<T, string>> _parameters;

        #endregion Fields

        #region Properties

        public string Name { get; }

        #endregion Properties

        #region Constructors

        public CommandBuilder(string name)
        {
            Name = name;
            _parameters = new Dictionary<string, Action<T, string>>();
        }

        #endregion Constructors

        #region Methods

        public CommandBuilder<T> Parameter(string name, Action<T, string> action) 
        {
            _parameters.Add(name, action);
            return this;
        }

        public CommandRunner Build(Action<T> action) 
        {
            return new CommandRunner(Name, (parameters) => action(Build(parameters)));
        }

        private T Build(string parameters)
        {
            var command = new T();
            var paramList = parameters.Split('-', StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in paramList)
            {
                var tokens = param.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                if (!_parameters.TryGetValue(tokens[0], out var prepare))
                {
                    continue;
                }

                prepare(command, tokens.Length == 1 ? string.Empty : tokens[1].TrimEnd());
            }

            return command;
        }

        #endregion Methods
    }
}