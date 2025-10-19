using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
    public enum AppState { Menu, Waiting, Placement, Playing, GameOver }

    public class GameModel
    {
        public List<Ship> YourShips { get; } = new();
        public HashSet<Point> YourHitsByOpponent { get; } = new();
        public HashSet<Point> YourFired { get; } = new();
        public HashSet<Point> YourFiredHits { get; } = new();
        public HashSet<Point> AnimatedCells { get; } = new();

        // current disaster info (set while animating)
        public string? CurrentDisasterName { get; set; }
        public bool IsDisasterAnimating { get; set; }

        public AppState State { get; set; } = AppState.Menu;
        public bool IsMyTurn { get; set; }
        public int PlacementSecondsLeft { get; set; }
        public int DisasterCountdown { get; set; } = -1;

        // Drag and drop state
        public Ship? DraggedShip { get; set; }
        public Point DragOffset { get; set; }

        public void Reset()
        {
            YourShips.Clear();
            YourHitsByOpponent.Clear();
            YourFired.Clear();
            YourFiredHits.Clear();
            AnimatedCells.Clear();
            IsMyTurn = false;
            PlacementSecondsLeft = 0;
            DisasterCountdown = -1;
            CurrentDisasterName = null;
            IsDisasterAnimating = false;
            DraggedShip = null;
            DragOffset = Point.Empty;
            State = AppState.Menu;
        }

        public bool CanPlaceShip(Ship ship, Point position)
        {
            ship.Position = position;
            
            // Check if ship is within board bounds
            if (!ship.IsValidPosition(Board.Size))
                return false;
                
            // Check for collisions with existing ships
            var newCells = ship.GetOccupiedCells();
            foreach (var existingShip in YourShips.Where(s => s.IsPlaced))
            {
                if (existingShip == ship) continue;
                
                var existingCells = existingShip.GetOccupiedCells();
                if (newCells.Any(cell => existingCells.Contains(cell)))
                    return false;
            }
            
            return true;
        }

        public void PlaceShip(Ship ship, Point position)
        {
            if (CanPlaceShip(ship, position))
            {
                ship.Position = position;
                ship.IsPlaced = true;
            }
        }

        public void RemoveShip(Ship ship)
        {
            ship.IsPlaced = false;
            ship.Position = Point.Empty;
        }

        public List<Point> GetAllShipCells()
        {
            return YourShips.Where(s => s.IsPlaced)
                           .SelectMany(s => s.GetOccupiedCells())
                           .ToList();
        }

        public void ApplyMoveResult(Point p, bool hit)
        {
            YourFired.Add(p);
            if (hit) YourFiredHits.Add(p);
        }

        public void ApplyOpponentMove(Point p, bool hit)
        {
            YourHitsByOpponent.Add(p);
            if (hit)
            {
                // Mark the hit cell but don't remove from ship list
                // The server will handle ship destruction logic
            }
        }

        public void ApplyOpponentHitByDisaster(Point p)
        {
            // This represents a hit on the opponent's board that you can see (disaster hit)
            // Add it to YourFiredHits so it shows up on the opponent board (right side)
            YourFired.Add(p);
            YourFiredHits.Add(p);
        }
    }
}