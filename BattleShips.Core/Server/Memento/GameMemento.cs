using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core.Server.Memento
{
    /// <summary>
    /// Memento pattern implementation for saving game state.
    /// Stores a complete snapshot of a GameInstance's state.
    /// SECURE: All setters are internal - only GameInstance can modify state.
    /// External classes can only read through IReadOnlyCollection properties.
    /// </summary>
    public class GameMemento
    {
        // Game identifiers - read-only externally
        public string GameId { get; internal set; }
        public string? PlayerAName { get; internal set; }
        public string? PlayerBName { get; internal set; }
        public DateTime SavedAt { get; internal set; }

        // Ship data - private backing fields
        private List<ShipData> _shipsAData = new();
        private List<ShipData> _shipsBData = new();

        // Expose as read-only collections
        public IReadOnlyList<ShipData> ShipsAData => _shipsAData.AsReadOnly();
        public IReadOnlyList<ShipData> ShipsBData => _shipsBData.AsReadOnly();

        // Internal setters for GameInstance
        internal void SetShipsAData(List<ShipData> data) => _shipsAData = new List<ShipData>(data);
        internal void SetShipsBData(List<ShipData> data) => _shipsBData = new List<ShipData>(data);

        // Hit cells - private backing fields
        private List<Point> _hitCellsA = new();
        private List<Point> _hitCellsB = new();

        public IReadOnlyList<Point> HitCellsA => _hitCellsA.AsReadOnly();
        public IReadOnlyList<Point> HitCellsB => _hitCellsB.AsReadOnly();

        internal void SetHitCellsA(List<Point> cells) => _hitCellsA = new List<Point>(cells);
        internal void SetHitCellsB(List<Point> cells) => _hitCellsB = new List<Point>(cells);

        // Mine data - private backing fields
        private List<MineData> _minesAData = new();
        private List<MineData> _minesBData = new();

        public IReadOnlyList<MineData> MinesAData => _minesAData.AsReadOnly();
        public IReadOnlyList<MineData> MinesBData => _minesBData.AsReadOnly();

        internal void SetMinesAData(List<MineData> data) => _minesAData = new List<MineData>(data);
        internal void SetMinesBData(List<MineData> data) => _minesBData = new List<MineData>(data);

        // Game state - internal setters
        public bool ReadyA { get; internal set; }
        public bool ReadyB { get; internal set; }
        public bool Started { get; internal set; }
        public bool ShipsReadyA { get; internal set; }
        public bool ShipsReadyB { get; internal set; }
        public bool MinesReadyA { get; internal set; }
        public bool MinesReadyB { get; internal set; }

        // Turn information - internal setters
        public string? CurrentTurn { get; internal set; }
        public bool IsPlayerATurn { get; internal set; } // Track whose turn it is by player, not connection ID
        public int TurnCount { get; internal set; }

        // Power-ups and special states - internal setters
        public int ActionPointsA { get; internal set; }
        public int ActionPointsB { get; internal set; }
        public bool HasMiniNukeA { get; internal set; }
        public bool HasMiniNukeB { get; internal set; }

        // Game mode data - internal setter
        public GameModeData? GameModeData { get; internal set; }

        internal GameMemento(string gameId)
        {
            GameId = gameId;
            SavedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Serializable ship data for memento
    /// SECURE: Internal setters prevent external modification
    /// </summary>
    public class ShipData
    {
        public int Id { get; internal set; }
        public int Length { get; internal set; }
        public Point Position { get; internal set; }
        public ShipOrientation Orientation { get; internal set; }
        public bool IsPlaced { get; internal set; }
        public ShipClass Class { get; internal set; }
        public string ShipType { get; internal set; } = ""; // "AircraftCarrier", "BattleShip", etc.

        internal static ShipData FromShip(IShip ship)
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

        internal IShip ToShip()
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
    /// SECURE: Internal setters prevent external modification
    /// </summary>
    public class MineData
    {
        public Guid Id { get; internal set; }
        public Point Position { get; internal set; }
        public string OwnerConnId { get; internal set; } = "";
        public MineCategory Category { get; internal set; }
        public bool IsExploded { get; internal set; }

        internal static MineData FromMine(NavalMine mine)
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

        internal NavalMine ToMine()
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
    /// SECURE: Internal setters prevent external modification
    /// </summary>
    public class GameModeData
    {
        public int ShipCount { get; internal set; }
        public int BoardX { get; internal set; }
        public int BoardY { get; internal set; }
        public int DisasterCountdown { get; internal set; }
        public string? CurrentEventName { get; internal set; }
        public int TurnCount { get; internal set; }

        internal static GameModeData? FromGameMode(GameMode? gameMode, int turnCount)
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

        internal GameMode ToGameMode()
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
