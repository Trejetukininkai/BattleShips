using System;

namespace BattleShips.Core
{
    public class ConsoleUI
    {
        private CommandInterpreter _interpreter;
        private string _currentInput = string.Empty;
        private CommandContext? _context;

        public ConsoleUI(CommandInterpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public ConsoleUI(CommandInterpreter interpreter, CommandContext context) : this(interpreter)
        {
            _context = context;
            WireEventHandlers();
        }

        private void WireEventHandlers()
        {
            if (_context?.GameClient == null || _context?.GameModel == null)
                return;

            _context.GameClient.WaitingForOpponent += msg =>
            {
                Console.WriteLine($"\n[Server] {msg}");
            };

            _context.GameClient.StartPlacement += seconds =>
            {
                Console.WriteLine($"\n[Game] Ship placement phase started! You have {seconds} seconds.");
                Console.WriteLine("Use 'place ship <type> <x> <y> <orientation>' or 'place auto' to place ships.");
                _context.GameModel.State = AppState.Placement;
            };

            _context.GameClient.PlacementAck += count =>
            {
                Console.WriteLine($"\n[Server] {count} ship(s) placed successfully!");
            };

            _context.GameClient.StartMinePlacement += seconds =>
            {
                Console.WriteLine($"\n[Game] Mine placement phase started! You have {seconds} seconds.");
                Console.WriteLine("Use 'place mine <x> <y>' to place mines (3 required).");
                _context.GameModel.State = AppState.MineSelection;
            };

            _context.GameClient.MinesAck += count =>
            {
                Console.WriteLine($"\n[Server] {count} mine(s) acknowledged!");
            };

            _context.GameClient.GameStarted += youStart =>
            {
                _context.GameModel.State = AppState.Playing;
                _context.GameModel.IsMyTurn = youStart;
                Console.WriteLine($"\n[Game] Battle has begun! {(youStart ? "It's YOUR turn!" : "Opponent goes first.")}");
                if (youStart)
                {
                    Console.WriteLine("Use 'fire <x> <y>' to attack (e.g., 'fire A 5')");
                }
            };

            _context.GameClient.YourTurn += () =>
            {
                _context.GameModel.IsMyTurn = true;
                Console.WriteLine("\n[Game] It's YOUR turn! Use 'fire <x> <y>' to attack.");
            };

            _context.GameClient.OpponentTurn += () =>
            {
                _context.GameModel.IsMyTurn = false;
                Console.WriteLine("\n[Game] Opponent's turn. Waiting...");
            };

            _context.GameClient.MoveResult += (col, row, hit, remaining) =>
            {
                char colLetter = (char)('A' + col);
                int rowNum = row + 1;
                Console.WriteLine($"\n[Result] {colLetter}{rowNum}: {(hit ? "HIT!" : "Miss")} - Opponent has {remaining} ship cell(s) remaining");
            };

            _context.GameClient.OpponentMoved += (col, row, hit) =>
            {
                char colLetter = (char)('A' + col);
                int rowNum = row + 1;
                Console.WriteLine($"\n[Opponent] Fired at {colLetter}{rowNum}: {(hit ? "HIT your ship!" : "Missed")}");
            };

            _context.GameClient.GameOver += msg =>
            {
                _context.GameModel.State = AppState.GameOver;
                Console.WriteLine($"\n[GAME OVER] {msg}");
            };

            _context.GameClient.DisasterOccurred += (affected, hits, disasterType) =>
            {
                Console.WriteLine($"\n[DISASTER] {disasterType ?? "Unknown"} occurred! {affected.Count} cells affected.");
            };

            _context.GameClient.DisasterCountdownChanged += countdown =>
            {
                _context.GameModel.DisasterCountdown = countdown;
                if (countdown > 0)
                {
                    Console.WriteLine($"\n[Warning] Disaster incoming in {countdown} turns!");
                }
            };

            _context.GameClient.MineTriggered += (id, points, category) =>
            {
                Console.WriteLine($"\n[Mine] {category} mine triggered! {points.Count} cells affected.");
            };

            _context.GameClient.ActionPointsUpdated += ap =>
            {
                _context.GameModel.ActionPoints = ap;
                Console.WriteLine($"\n[Action Points] You now have {ap} action points.");
            };

            _context.GameClient.PowerUpActivated += powerUp =>
            {
                Console.WriteLine($"\n[PowerUp] {powerUp} activated!");
            };

            _context.GameClient.Error += msg =>
            {
                Console.WriteLine($"\n[Error] {msg}");
            };

            _context.GameClient.OpponentDisconnected += msg =>
            {
                Console.WriteLine($"\n[Server] {msg}");
            };

            _context.GameClient.GameCancelled += msg =>
            {
                Console.WriteLine($"\n[Server] Game cancelled: {msg}");
                _context.GameModel.State = AppState.Menu;
            };
        }

        public void ProcessCommand(string input)
        {
            string result = _interpreter.Execute(input);
            if (!string.IsNullOrEmpty(result))
            {
                Console.WriteLine(result);
            }
        }

        public void HandleUpArrow()
        {
            var previousCommand = _interpreter.GetPreviousCommand();
            if (previousCommand != null)
            {
                _currentInput = previousCommand;
                Console.Write($"\r> {_currentInput}");
            }
        }

        public void HandleDownArrow()
        {
            var nextCommand = _interpreter.GetNextCommand();
            if (nextCommand != null)
            {
                _currentInput = nextCommand;
                Console.Write($"\r> {_currentInput}");
            }
            else
            {
                _currentInput = string.Empty;
                Console.Write("\r> ");
            }
        }

        public void DisplayPrompt()
        {
            Console.Write("> ");
        }

        public void DisplayWelcome()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("   BattleShips Console Interface");
            Console.WriteLine("========================================");
            Console.WriteLine("Type 'help' for available commands");
            Console.WriteLine("Type 'quit' to exit");
            Console.WriteLine("========================================");
        }

        public void Run()
        {
            DisplayWelcome();

            while (true)
            {
                try
                {
                    DisplayPrompt();
                    var input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    ProcessCommand(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
        }

        public void RunInteractive()
        {
            DisplayWelcome();

            while (true)
            {
                try
                {
                    DisplayPrompt();
                    _currentInput = string.Empty;

                    // Read input with support for arrow keys
                    while (true)
                    {
                        var keyInfo = Console.ReadKey(intercept: true);

                        if (keyInfo.Key == ConsoleKey.Enter)
                        {
                            Console.WriteLine();
                            break;
                        }
                        else if (keyInfo.Key == ConsoleKey.UpArrow)
                        {
                            HandleUpArrow();
                        }
                        else if (keyInfo.Key == ConsoleKey.DownArrow)
                        {
                            HandleDownArrow();
                        }
                        else if (keyInfo.Key == ConsoleKey.Backspace)
                        {
                            if (_currentInput.Length > 0)
                            {
                                _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
                                Console.Write("\b \b");
                            }
                        }
                        else if (!char.IsControl(keyInfo.KeyChar))
                        {
                            _currentInput += keyInfo.KeyChar;
                            Console.Write(keyInfo.KeyChar);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(_currentInput))
                        continue;

                    ProcessCommand(_currentInput);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
        }
    }
}
