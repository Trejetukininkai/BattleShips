using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core.Server.Memento
{
    /// <summary>
    /// Memento pattern implementation for saving game state.
    /// Stores a complete snapshot of a GameInstance's state.
    /// </summary>
    public class GameMemento
    {
        // Game identifiers
        public string GameId { get; set; }
        public string? PlayerAName { get; set; }
        public string? PlayerBName { get; set; }
        public DateTime SavedAt { get; set; }

        // Ship data - serializable format
        public List<ShipData> ShipsAData { get; set; } = new();
        public List<ShipData> ShipsBData { get; set; } = new();

        // Hit cells
        public List<Point> HitCellsA { get; set; } = new();
        public List<Point> HitCellsB { get; set; } = new();

        // Mine data
        public List<MineData> MinesAData { get; set; } = new();
        public List<MineData> MinesBData { get; set; } = new();

        // Game state
        public bool ReadyA { get; set; }
        public bool ReadyB { get; set; }
        public bool Started { get; set; }
        public bool ShipsReadyA { get; set; }
        public bool ShipsReadyB { get; set; }
        public bool MinesReadyA { get; set; }
        public bool MinesReadyB { get; set; }

        // Turn information
        public string? CurrentTurn { get; set; }
        public bool IsPlayerATurn { get; set; } // Track whose turn it is by player, not connection ID
        public int TurnCount { get; set; }

        // Power-ups and special states
        public int ActionPointsA { get; set; }
        public int ActionPointsB { get; set; }
        public bool HasMiniNukeA { get; set; }
        public bool HasMiniNukeB { get; set; }

        // Game mode data
        public GameModeData? GameModeData { get; set; }

        public GameMemento(string gameId)
        {
            GameId = gameId;
            SavedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Serializable ship data for memento
    /// </summary>
    public class ShipData
    {
        public int Id { get; set; }
        public int Length { get; set; }
        public Point Position { get; set; }
        public ShipOrientation Orientation { get; set; }
        public bool IsPlaced { get; set; }
        public ShipClass Class { get; set; }
        public string ShipType { get; set; } = ""; // "AircraftCarrier", "BattleShip", etc.

        public static ShipData FromShip(IShip ship)
        {
            return new ShipData
            {
                Id = ship.Id,
                Length = ship.Length,
                Position = ship.Position,
                Orientation = ship.Orientation,
                IsPlaced = ship.IsPlaced,
                Class = (ship as BaseShip)?.Class ?? ShipClass.Blocky,
                ShipType = ship.GetType().Name
            };
        }

        public IShip ToShip()
        {
            var factory = Class == ShipClass.Curvy ? new CurvyClass() : (IClass)new BlockyClass();

            IShip ship = ShipType switch
            {
                "AircraftCarrier" => factory.CreateAircraftCarrier(Length, Id),
                "BattleShip" => factory.CreateBattleShip(Length, Id),
                "Cruiser" => factory.CreateCruiser(Length, Id),
                "Destroyer" => factory.CreateDestroyer(Length, Id),
                _ => factory.CreateDestroyer(Length, Id)
            };

            ship.Position = Position;
            ship.Orientation = Orientation;
            ship.IsPlaced = IsPlaced;

            return ship;
        }
    }

    /// <summary>
    /// Serializable mine data for memento
    /// </summary>
    public class MineData
    {
        public Guid Id { get; set; }
        public Point Position { get; set; }
        public string OwnerConnId { get; set; } = "";
        public MineCategory Category { get; set; }
        public bool IsExploded { get; set; }

        public static MineData FromMine(NavalMine mine)
        {
            return new MineData
            {
                Id = mine.Id,
                Position = mine.Position,
                OwnerConnId = mine.OwnerConnId,
                Category = mine.Category,
                IsExploded = mine.IsExploded
            };
        }

        public NavalMine ToMine()
        {
            // Create mine using factory
            var mine = NavalMineFactory.CreateMine(Position, OwnerConnId, Category);

            // Restore exploded state using reflection (since IsExploded is private)
            if (IsExploded)
            {
                // Trigger the mine to mark it as exploded
                // We need to set this via a dummy trigger that won't actually execute
                var field = typeof(NavalMine).GetField("IsExploded",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(mine, true);
            }

            return mine;
        }
    }

    /// <summary>
    /// Serializable game mode data for memento
    /// </summary>
    public class GameModeData
    {
        public int ShipCount { get; set; }
        public int BoardX { get; set; }
        public int BoardY { get; set; }
        public int DisasterCountdown { get; set; }
        public string? CurrentEventName { get; set; }
        public int TurnCount { get; set; }

        public static GameModeData? FromGameMode(GameMode? gameMode, int turnCount)
        {
            if (gameMode == null) return null;

            return new GameModeData
            {
                ShipCount = gameMode.ShipCount,
                BoardX = gameMode.BoardX,
                BoardY = gameMode.BoardY,
                DisasterCountdown = gameMode.EventGenerator?.GetDisasterCountdown() ?? -1,
                CurrentEventName = gameMode.EventGenerator?.GetEventName(),
                TurnCount = turnCount
            };
        }

        public GameMode ToGameMode()
        {
            var gameMode = new GameMode(ShipCount, BoardX, BoardY);

            // Restore EventGenerator if we had one
            if (!string.IsNullOrEmpty(CurrentEventName) && DisasterCountdown >= 0)
            {
                Console.WriteLine($"[GameModeData] Restoring EventGenerator: EventName={CurrentEventName}, Countdown={DisasterCountdown}, TurnCount={TurnCount}");

                // Recreate the EventGenerator based on turn count
                // This will create the appropriate event with decorators based on difficulty scaling
                gameMode.SelectEventBasedOnTurn(TurnCount);

                // Now restore the countdown value using reflection
                if (gameMode.EventGenerator != null)
                {
                    var initialCountdown = gameMode.EventGenerator.GetDisasterCountdown();
                    Console.WriteLine($"[GameModeData] EventGenerator created with initial countdown: {initialCountdown}");

                    var countdownField = typeof(EventGenerator).GetField("DisasterCountdown",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (countdownField != null)
                    {
                        // Unwrap decorators to get to the base EventGenerator
                        var generator = gameMode.EventGenerator;
                        var wrappedField = typeof(EventDecorator).GetField("_wrapped",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        int unwrapCount = 0;
                        while (generator is EventDecorator && wrappedField != null)
                        {
                            generator = (IEventGenerator)wrappedField.GetValue(generator)!;
                            unwrapCount++;
                        }

                        Console.WriteLine($"[GameModeData] Unwrapped {unwrapCount} decorator(s) to reach base EventGenerator: {generator.GetType().Name}");

                        countdownField.SetValue(generator, DisasterCountdown);
                        var verifyCountdown = gameMode.EventGenerator.GetDisasterCountdown();
                        Console.WriteLine($"[GameModeData] Set countdown to {DisasterCountdown}, verified value: {verifyCountdown}");
                    }
                    else
                    {
                        Console.WriteLine($"[GameModeData] ERROR: Could not find DisasterCountdown field via reflection!");
                    }
                }
                else
                {
                    Console.WriteLine($"[GameModeData] ERROR: EventGenerator is null after SelectEventBasedOnTurn!");
                }
            }
            else
            {
                Console.WriteLine($"[GameModeData] Skipping EventGenerator restoration: EventName={CurrentEventName}, Countdown={DisasterCountdown}");
            }

            return gameMode;
        }
    }
}
