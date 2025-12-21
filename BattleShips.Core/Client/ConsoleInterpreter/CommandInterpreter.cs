using System;
using System.Collections.Generic;

namespace BattleShips.Core
{
    public class CommandInterpreter
    {
        private CommandParser _parser;
        private CommandContext _context;
        private List<string> _commandHistory;
        private int _historyIndex;

        public CommandInterpreter(CommandContext context)
        {
            _parser = new CommandParser();
            _context = context;
            _commandHistory = new List<string>();
            _historyIndex = -1;
        }

        public string Execute(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return string.Empty;

                // Add to history
                _commandHistory.Add(input);
                _historyIndex = _commandHistory.Count;

                // Parse and execute
                var expression = _parser.Parse(input);
                if (expression == null)
                    return "Empty command";

                return expression.Interpret(_context);
            }
            catch (ArgumentException argEx)
            {
                return $"Invalid arguments: {argEx.Message}";
            }
            catch (FormatException)
            {
                return "Invalid number format. Please check your input.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public List<string> GetCommandHistory()
        {
            return new List<string>(_commandHistory);
        }

        public string? GetPreviousCommand()
        {
            if (_commandHistory.Count == 0)
                return null;

            if (_historyIndex > 0)
                _historyIndex--;

            return _historyIndex >= 0 && _historyIndex < _commandHistory.Count
                ? _commandHistory[_historyIndex]
                : null;
        }

        public string? GetNextCommand()
        {
            if (_commandHistory.Count == 0)
                return null;

            if (_historyIndex < _commandHistory.Count - 1)
                _historyIndex++;
            else
                _historyIndex = _commandHistory.Count;

            return _historyIndex >= 0 && _historyIndex < _commandHistory.Count
                ? _commandHistory[_historyIndex]
                : null;
        }

        public void ClearHistory()
        {
            _commandHistory.Clear();
            _historyIndex = -1;
        }

        public CommandParser GetParser()
        {
            return _parser;
        }
    }
}
