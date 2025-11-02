using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
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

        private int TurnCount = 0;

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

                
                IShip ship = new BlockyClass().CreateDestroyer(length, ships.Count);
                ship.Position = cell;
                ship.IsPlaced = true;
                ship.Orientation = orientation;

                ships.Add(ship);
            }

            if (PlayerA == connId)
            {
                ShipsA = ships;
                ReadyA = true;
            }
            else if (PlayerB == connId)
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
    }
}
