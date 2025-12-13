using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleShips.Core
{
    public class CommandParser
    {
        private Dictionary<string, Func<string[], IExpression>> _commandRegistry = new();

        public CommandParser()
        {
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            _commandRegistry = new Dictionary<string, Func<string[], IExpression>>(StringComparer.OrdinalIgnoreCase)
            {
                ["help"] = args => new HelpExpression(),
                ["quit"] = args => new QuitExpression(),
                ["exit"] = args => new QuitExpression(),
                ["connect"] = args =>
                {
                    if (args.Length == 0)
                        throw new ArgumentException("Usage: connect <url>");
                    return new ConnectExpression(args[0]);
                },
                ["status"] = args => new StatusExpression(),
                ["fire"] = args =>
                {
                    if (args.Length < 2)
                        throw new ArgumentException("Usage: fire <x> <y> (e.g., fire A 5 or fire a 10)");

                    // Parse X coordinate (A-J)
                    int x;
                    string xInput = args[0].ToUpper();
                    if (xInput.Length == 1 && xInput[0] >= 'A' && xInput[0] <= 'J')
                    {
                        x = xInput[0] - 'A'; // Convert A-J to 0-9
                    }
                    else
                    {
                        throw new ArgumentException("X coordinate must be a letter A-J");
                    }

                    // Parse Y coordinate (1-10)
                    if (!int.TryParse(args[1], out int y) || y < 1 || y > 10)
                    {
                        throw new ArgumentException("Y coordinate must be a number 1-10");
                    }
                    y = y - 1; // Convert 1-10 to 0-9

                    return new FireExpression(x, y);
                },
                ["place"] = args =>
                {
                    if (args.Length == 0)
                        throw new ArgumentException("Usage: place <ship|mine|auto> [args]");
                    return new PlaceExpression(args[0], args.Skip(1).ToArray());
                },
                ["powerup"] = args =>
                {
                    if (args.Length == 0)
                        throw new ArgumentException("Usage: powerup <name>");
                    return new PowerUpExpression(args[0]);
                },
                ["submit"] = args => new SubmitExpression()
            };
        }

        public IExpression? Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var tokens = Tokenize(input);
            if (tokens.Length == 0)
                return null;

            string command = tokens[0].ToLower();
            string[] args = tokens.Skip(1).ToArray();

            if (_commandRegistry.TryGetValue(command, out var factory))
            {
                return factory(args);
            }

            throw new Exception($"Unknown command: '{command}'. Type 'help' for available commands.");
        }

        private string[] Tokenize(string input)
        {
            // Simple tokenization by spaces
            // Could be enhanced to handle quoted strings if needed
            return input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public void RegisterCommand(string name, Func<string[], IExpression> factory)
        {
            _commandRegistry[name] = factory;
        }

        public IEnumerable<string> GetCommandNames()
        {
            return _commandRegistry.Keys;
        }
    }
}
