using System;

namespace BattleShips.Core
{
    /// <summary>
    /// Example demonstrating how to use the Console Interpreter Pattern
    /// </summary>
    public class InterpreterExample
    {
        public static void Main(string[] args)
        {
            // Initialize the game components
            var gameModel = new GameModel();
            var gameClient = new GameClient();

            // Create the interpreter context
            var context = new CommandContext(gameModel, gameClient);

            // Create the interpreter
            var interpreter = new CommandInterpreter(context);

            // Create the console UI with event handlers wired up
            var consoleUI = new ConsoleUI(interpreter, context);

            // Run the interactive console
            consoleUI.RunInteractive();
        }

        /// <summary>
        /// Example of programmatic command execution
        /// </summary>
        public static void ProgrammaticExample()
        {
            var gameModel = new GameModel();
            var gameClient = new GameClient();
            var context = new CommandContext(gameModel, gameClient);
            var interpreter = new CommandInterpreter(context);
            var consoleUI = new ConsoleUI(interpreter, context);

            // Execute commands programmatically
            Console.WriteLine("=== Programmatic Command Execution ===\n");

            // Show help
            consoleUI.ProcessCommand("help");
            Console.WriteLine();

            // Connect to server
            consoleUI.ProcessCommand("connect http://localhost:5000");
            Console.WriteLine();

            // Check status
            consoleUI.ProcessCommand("status");
            Console.WriteLine();

            // Auto-place ships
            consoleUI.ProcessCommand("place auto");
            Console.WriteLine();

            // Fire at target
            consoleUI.ProcessCommand("fire 5 3");
            Console.WriteLine();

            // Activate power-up
            consoleUI.ProcessCommand("powerup MiniNuke");
            Console.WriteLine();
        }

        /// <summary>
        /// Example of custom command registration
        /// </summary>
        public static void CustomCommandExample()
        {
            var gameModel = new GameModel();
            var gameClient = new GameClient();
            var context = new CommandContext(gameModel, gameClient);
            var interpreter = new CommandInterpreter(context);

            // Register a custom command
            interpreter.GetParser().RegisterCommand("greet", args =>
            {
                return new GreetExpression(args.Length > 0 ? args[0] : "Player");
            });

            var consoleUI = new ConsoleUI(interpreter, context);

            // Test the custom command
            consoleUI.ProcessCommand("greet Captain");
        }
    }

    /// <summary>
    /// Example custom expression
    /// </summary>
    public class GreetExpression : IExpression
    {
        private string _name;

        public GreetExpression(string name)
        {
            _name = name;
        }

        public string Interpret(CommandContext ctx)
        {
            return $"Hello, {_name}! Welcome to BattleShips!";
        }

        public string GetHelp()
        {
            return "greet [name] - Greet a player";
        }
    }

    /// <summary>
    /// Example of testing individual expressions
    /// </summary>
    public class ExpressionTestExample
    {
        public static void TestExpressions()
        {
            var gameModel = new GameModel();
            var gameClient = new GameClient();
            var context = new CommandContext(gameModel, gameClient);

            // Test Help Expression
            var helpExpr = new HelpExpression();
            Console.WriteLine(helpExpr.Interpret(context));
            Console.WriteLine();

            // Test Status Expression
            var statusExpr = new StatusExpression();
            Console.WriteLine(statusExpr.Interpret(context));
            Console.WriteLine();

            // Test Fire Expression
            var fireExpr = new FireExpression(5, 3);
            Console.WriteLine(fireExpr.Interpret(context));
            Console.WriteLine();
        }
    }
}
