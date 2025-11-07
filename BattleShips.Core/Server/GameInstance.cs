using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
    // --- Server side game instance ---
    public class GameInstance
    {
        public string Id { get; }
        public string? PlayerA { get; set; }
        public string? PlayerB { get; set; }
        public List<IShip> ShipsA { get; set; } = new();
        public List<IShip> ShipsB { get; set; } = new();
        public ShipClass? ClassA { get; set; }
        public ShipClass? ClassB { get; set; }
        public HashSet<Point> HitCellsA { get; set; } = new();
        public HashSet<Point> HitCellsB { get; set; } = new();
        public bool ReadyA { get; set; }
        public bool ReadyB { get; set; }
        public bool Started { get; set; }
        public DateTime PlacementDeadline { get; set; }
        public string? CurrentTurn { get; set; }
        public GameMode? GameMode { get; set; }
        public bool ShouldCancelOnNextAction { get; set; } = false;
        public bool EventInProgress { get; set; } = false;
        public bool ShipsReadyA { get; set; }
        public bool ShipsReadyB { get; set; }

        public bool AllShipsPlaced => ShipsReadyA && ShipsReadyB;
        public bool AllMinesPlaced => MinesReadyA && MinesReadyB;



        private int TurnCount = 0;


        public List<NavalMine> MinesA { get; set; } = new();
        public List<NavalMine> MinesB { get; set; } = new();

        public bool MinesReadyA { get; set; } = false;
        public bool MinesReadyB { get; set; } = false;


        public void SetPlayerMines(string connId, List<NavalMine> mines)
        {
            if (connId == PlayerA)
            {
                MinesA = mines;
                MinesReadyA = true;
            }
            else if (connId == PlayerB)
            {
                MinesB = mines;
                MinesReadyB = true;
            }
        }


        public bool MinePlacementStarted { get; set; } = false;


        public GameInstance(string id)
        {
            Id = id;
        }

        public int PlayerCount => (HasFirstPlayer ? 1 : 0) + (HasSecondPlayer ? 1 : 0);
        public bool HasFirstPlayer => PlayerA != null;
        public bool HasSecondPlayer => PlayerB != null;

        public void IncrementTurnCount() => TurnCount++;
        public int GetTurnCount() => TurnCount;

        public void RemovePlayer(string connId)
        {
            if (PlayerA == connId) PlayerA = null;
            if (PlayerB == connId) PlayerB = null;
        }

        public string? Other(string connId)
        {
            if (PlayerA == connId) return PlayerB;
            if (PlayerB == connId) return PlayerA;
            return null;
        }

        /// <summary>
        /// Converts placed ship cells into IShip objects.
        /// This simplified version groups contiguous cells into ships.
        /// </summary>
        public void SetPlayerShips(string connId, List<Point> shipCells)
        {
            var ships = new List<IShip>();
            var placedCells = new HashSet<Point>(shipCells);

            while (placedCells.Any())
            {
                var cell = placedCells.First();
                placedCells.Remove(cell);

                // Determine orientation and collect contiguous cells
                var horizontalCells = new List<Point> { cell };
                for (int i = 1; i < 6; i++)
                {
                    var next = new Point(cell.X + i, cell.Y);
                    if (placedCells.Remove(next))
                        horizontalCells.Add(next);
                    else break;
                }

                if (horizontalCells.Count == 1)
                {
                    for (int i = 1; i < 6; i++)
                    {
                        var next = new Point(cell.X, cell.Y + i);
                        if (placedCells.Remove(next))
                            horizontalCells.Add(next);
                        else break;
                    }
                }

                int length = horizontalCells.Count;
                ShipOrientation orientation = horizontalCells.Count > 1 && horizontalCells[1].X != horizontalCells[0].X
                    ? ShipOrientation.Horizontal
                    : ShipOrientation.Vertical;

                // Create a generic Blocky ship
                IShip ship = new BlockyClass().CreateDestroyer(length, ships.Count);
                ship.Position = cell;
                ship.IsPlaced = true;
                ship.Orientation = orientation;

                ships.Add(ship);
            }

            if (connId == PlayerA)
            {
                ShipsA = ships;
                ReadyA = true; 
            }
            else if (connId == PlayerB)
            {
                ShipsB = ships;
                ReadyB = true; 
            }
        }

        public int GetRemainingShips(string connId)
        {
            var ships = PlayerA == connId ? ShipsA : ShipsB;
            var hitCells = PlayerA == connId ? HitCellsA : HitCellsB;

            return ships.Count(ship =>
            {
                var cells = ship.GetOccupiedCells();
                return !cells.All(c => hitCells.Contains(c));
            });
        }

        // Register shot on opponent; returns hit and out opponentLost
        public bool RegisterShot(string opponentConnId, Point shot, out bool opponentLost)
        {
            var hit = false;
            var ships = PlayerA == opponentConnId ? ShipsA : ShipsB;
            var hitCells = PlayerA == opponentConnId ? HitCellsA : HitCellsB;

            // Check if shot hits any ship
            foreach (var ship in ships.Where(s => s.IsPlaced))
            {
                if (ship.GetOccupiedCells().Contains(shot))
                {
                    hit = true;
                    break;
                }
            }

            // Record the hit
            if (hit)
                hitCells.Add(shot);

            // Check if opponent lost
            opponentLost = AreAllShipsDestroyed(ships, hitCells);

            return hit;
        }

        public void RegisterDisasterHits(string playerConnId, List<Point> hitPoints)
        {
            var hitCells = PlayerA == playerConnId ? HitCellsA : HitCellsB;
            foreach (var point in hitPoints)
                hitCells.Add(point);
        }

        private bool AreAllShipsDestroyed(List<IShip> ships, HashSet<Point> hitCells)
        {
            return ships.Where(s => s.IsPlaced)
                        .All(ship => ship.GetOccupiedCells().All(cell => hitCells.Contains(cell)));
        }

        public void SwitchTurn()
        {
            if (CurrentTurn == PlayerA)
                CurrentTurn = PlayerB;
            else
                CurrentTurn = PlayerA;
        }


        // Call when a shot happens: returns whether it was a hit AND any mine triggered info
        // Note: this wraps RegisterShot: it checks for mine triggers before/after recording hits as you prefer.
        // This implementation triggers anti-enemy mines if shot lands on mine's position.
        public bool RegisterShotWithMines(string opponentConnId, Point shot, out bool opponentLost, out List<(Guid mineId, MineCategory category, List<Point> effectPoints)> triggeredMines)
        {
            triggeredMines = new List<(Guid, MineCategory, List<Point>)>();
            opponentLost = false;

            Console.WriteLine($"[Server] 🔍 Checking mines at shot position ({shot.X},{shot.Y})");

            var opponentMines = PlayerA == opponentConnId ? MinesA : MinesB;
            var opponentHitCells = PlayerA == opponentConnId ? HitCellsA : HitCellsB;
            var opponentShips = PlayerA == opponentConnId ? ShipsA : ShipsB;

            Console.WriteLine($"[Server] Opponent has {opponentMines.Count} mines, {opponentMines.Count(m => !m.IsExploded)} active");

            var minesAtPoint = opponentMines.Where(m => !m.IsExploded && m.Position == shot).ToList();
            Console.WriteLine($"[Server] Found {minesAtPoint.Count} mines at shot position");

            foreach (var mine in minesAtPoint)
            {
                Console.WriteLine($"[Server] Checking mine {mine.Id} ({mine.Category}) at ({mine.Position.X},{mine.Position.Y})");
                var result = mine.TryTrigger(this, MineTriggerType.EnemyShot, Other(opponentConnId) ?? "");
                if (result != null)
                {
                    Console.WriteLine($"[Server] 💥 Mine triggered! Effect points: {result.Count}");
                    triggeredMines.Add((mine.Id, mine.Category, result));
                }
                else
                {
                    Console.WriteLine($"[Server] Mine did not trigger (should not happen for enemy shot on mine position)");
                }
            }

            // Remove exploded mines from list
            opponentMines.RemoveAll(m => m.IsExploded);

            var hit = false;
            foreach (var ship in opponentShips.Where(s => s.IsPlaced))
            {
                if (ship.GetOccupiedCells().Contains(shot))
                {
                    hit = true;
                    break;
                }
            }

            if (hit)
            {
                opponentHitCells.Add(shot);
                Console.WriteLine($"[Server] 🎯 Shot hit a ship!");
            }
            else
            {
                Console.WriteLine($"[Server] 💧 Shot missed ships");
            }

            opponentLost = AreAllShipsDestroyed(opponentShips, opponentHitCells);

            if (opponentLost)
                Console.WriteLine($"[Server] 🏁 Opponent lost all ships!");

            return hit;
        }

        public void DebugShipPositions()
        {
            Console.WriteLine($"[GameInstance] 🚢 PlayerA ships:");
            foreach (var ship in ShipsA.Where(s => s.IsPlaced))
            {
                var cells = ship.GetOccupiedCells();
                Console.WriteLine($"[GameInstance]   - Ship {ship.Id}: {string.Join(", ", cells.Select(p => $"({p.X},{p.Y})"))}");
            }

            Console.WriteLine($"[GameInstance] 🚢 PlayerB ships:");
            foreach (var ship in ShipsB.Where(s => s.IsPlaced))
            {
                var cells = ship.GetOccupiedCells();
                Console.WriteLine($"[GameInstance]   - Ship {ship.Id}: {string.Join(", ", cells.Select(p => $"({p.X},{p.Y})"))}");
            }
        }

        // Call when a disaster applies hits on player's board (server side).
        // It should check anti-disaster mines that might trigger for those points.
        public void ApplyDisasterWithMines(string playerConnId, List<Point> hitPoints, out List<(Guid mineId, MineCategory category, List<Point> effectPoints)> triggeredMines)
        {
            triggeredMines = new List<(Guid, MineCategory, List<Point>)>();

            Console.WriteLine($"[GameInstance] ApplyDisasterWithMines for {playerConnId} with {hitPoints.Count} hit points");

            // Get the correct mines and hit cells for this player
            var playerMines = PlayerA == playerConnId ? MinesA : MinesB;
            var playerHitCells = PlayerA == playerConnId ? HitCellsA : HitCellsB;

            Console.WriteLine($"[GameInstance] Checking {playerMines.Count} mines for player {playerConnId}");

            // Log all mine positions
            Console.WriteLine($"[GameInstance] All mine positions for player {playerConnId}:");
            foreach (var mine in playerMines)
            {
                Console.WriteLine($"[GameInstance] - Mine {mine.Category} at ({mine.Position.X},{mine.Position.Y}) - Exploded: {mine.IsExploded}");
            }

            // Log all disaster hit points
            foreach (var p in hitPoints)
            {
                Console.WriteLine($"[GameInstance] Disaster hit at ({p.X},{p.Y})");
                playerHitCells.Add(p);
            }

            // Check mines that are on the points
            var minesTriggered = playerMines.Where(m => !m.IsExploded && hitPoints.Contains(m.Position)).ToList();
            Console.WriteLine($"[GameInstance] Found {minesTriggered.Count} mines at exact disaster hit points");

            foreach (var mine in minesTriggered)
            {
                Console.WriteLine($"[GameInstance] Checking mine at ({mine.Position.X},{mine.Position.Y}) - {mine.Category}");
                var res = mine.TryTrigger(this, MineTriggerType.Disaster, triggeringConnId: playerConnId);
                if (res != null)
                {
                    Console.WriteLine($"[GameInstance] Mine triggered with {res.Count} effect points");
                    triggeredMines.Add((mine.Id, mine.Category, res));
                }
                else
                {
                    Console.WriteLine($"[GameInstance] Mine did not trigger");
                }
            }

            playerMines.RemoveAll(m => m.IsExploded);
            Console.WriteLine($"[GameInstance] Total mines triggered: {triggeredMines.Count}");
        }


    }
}
