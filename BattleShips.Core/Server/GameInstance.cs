// --- Server side game instance ---
using System.Drawing;

namespace BattleShips.Core
{
    public class GameInstance
    {
        public string Id { get; }
        public string? PlayerA { get; set; }
        public string? PlayerB { get; set; }
        public List<Ship> ShipsA { get; set; } = new();
        public List<Ship> ShipsB { get; set; } = new();
        public HashSet<Point> HitCellsA { get; set; } = new(); // Cells hit on Player A's board
        public HashSet<Point> HitCellsB { get; set; } = new(); // Cells hit on Player B's board
        public bool ReadyA { get; set; } // when true, player A has placed ships
        public bool ReadyB { get; set; } // when true, player B has placed ships
        public bool Started { get; set; }
        public DateTime PlacementDeadline { get; set; }
        public string? CurrentTurn { get; set; }
        public GameMode? GameMode { get; set; }

        // when true, game should be cancelled on next hub action (due to timeout)
        public bool ShouldCancelOnNextAction { get; set; } = false;

        // when true, server will reject MakeMove requests until cleared
        public bool EventInProgress { get; set; } = false;

        public GameInstance(string id) { Id = id; }

        public int PlayerCount => (HasFirstPlayer ? 1 : 0) + (HasSecondPlayer ? 1 : 0);
        public bool HasFirstPlayer => PlayerA != null;
        public bool HasSecondPlayer => PlayerB != null;
        private int TurnCount = 0;

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

        public void SetPlayerShips(string connId, List<Point> shipCells)
        {
            // Convert ship cells back to Ship objects
            var ships = new List<Ship>();
            var placedCells = new HashSet<Point>(shipCells);
            
            // Create ships from the placed cells (simplified - assumes ships are placed correctly)
            foreach (var cell in shipCells)
            {
                if (placedCells.Contains(cell))
                {
                    // Find ship length by counting consecutive cells
                    var length = 1;
                    
                    // Check horizontal
                    for (int i = 1; i < 6; i++)
                    {
                        if (placedCells.Contains(new Point(cell.X + i, cell.Y)))
                            length++;
                        else break;
                    }
                    
                    // If horizontal length is 1, check vertical
                    if (length == 1)
                    {
                        for (int i = 1; i < 6; i++)
                        {
                            if (placedCells.Contains(new Point(cell.X, cell.Y + i)))
                                length++;
                            else break;
                        }
                    }
                    
                    // Create ship
                    var ship = new Ship(length, ships.Count)
                    {
                        Position = cell,
                        IsPlaced = true
                    };
                    
                    // Set orientation
                    if (length > 1)
                    {
                        ship.Orientation = placedCells.Contains(new Point(cell.X + 1, cell.Y)) 
                            ? ShipOrientation.Horizontal 
                            : ShipOrientation.Vertical;
                    }
                    
                    ships.Add(ship);
                    
                    // Remove this ship's cells from consideration
                    var occupiedCells = ship.GetOccupiedCells();
                    foreach (var shipCell in occupiedCells)
                        placedCells.Remove(shipCell);
                }
            }
            
            if (PlayerA == connId) { ShipsA = ships; ReadyA = true; }
            else if (PlayerB == connId) { ShipsB = ships; ReadyB = true; }
        }

        public int GetRemainingShips(string connId)
        {
            var ships = PlayerA == connId ? ShipsA : ShipsB;
            var hitCells = PlayerA == connId ? HitCellsA : HitCellsB;
            
            int remainingShips = 0;
            foreach (var ship in ships.Where(s => s.IsPlaced))
            {
                var shipCells = ship.GetOccupiedCells();
                var isShipDestroyed = shipCells.All(cell => hitCells.Contains(cell));
                
                if (!isShipDestroyed)
                {
                    remainingShips++;
                }
            }
            
            return remainingShips;
        }

        // Register shot on opponent; returns hit, and out opponentLost
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
            {
                hitCells.Add(shot);
            }
            
            // Check if opponent lost (all ships destroyed)
            opponentLost = AreAllShipsDestroyed(ships, hitCells);
            
            return hit;
        }

        // Register disaster hits on a player's board
        public void RegisterDisasterHits(string playerConnId, List<Point> hitPoints)
        {
            var hitCells = PlayerA == playerConnId ? HitCellsA : HitCellsB;
            
            foreach (var point in hitPoints)
            {
                hitCells.Add(point);
            }
        }

        // Check if all ships are destroyed
        private bool AreAllShipsDestroyed(List<Ship> ships, HashSet<Point> hitCells)
        {
            foreach (var ship in ships.Where(s => s.IsPlaced))
            {
                var shipCells = ship.GetOccupiedCells();
                var shipHit = shipCells.All(cell => hitCells.Contains(cell));
                
                if (!shipHit) // If any ship is not fully destroyed, game continues
                {
                    return false;
                }
            }
            
            return true; // All ships are destroyed
        }

        public void SwitchTurn()
        {
            if (CurrentTurn == PlayerA) CurrentTurn = PlayerB;
            else CurrentTurn = PlayerA;
        }
    }
}
