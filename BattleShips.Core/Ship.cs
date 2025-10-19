using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace BattleShips.Core
{
    public enum ShipOrientation
    {
        Horizontal,
        Vertical
    }

    public class Ship
    {
        public int Length { get; }
        public Point Position { get; set; }
        public ShipOrientation Orientation { get; set; }
        public bool IsPlaced { get; set; }
        public int Id { get; }

        public Ship(int length, int id)
        {
            Length = length;
            Id = id;
            Position = Point.Empty;
            Orientation = ShipOrientation.Horizontal;
            IsPlaced = false;
        }

        public List<Point> GetOccupiedCells()
        {
            var cells = new List<Point>();
            for (int i = 0; i < Length; i++)
            {
                if (Orientation == ShipOrientation.Horizontal)
                    cells.Add(new Point(Position.X + i, Position.Y));
                else
                    cells.Add(new Point(Position.X, Position.Y + i));
            }
            return cells;
        }

        public void Rotate()
        {
            Orientation = Orientation == ShipOrientation.Horizontal 
                ? ShipOrientation.Vertical 
                : ShipOrientation.Horizontal;
        }

        public bool IsValidPosition(int boardSize)
        {
            var cells = GetOccupiedCells();
            return cells.All(cell => 
                cell.X >= 0 && cell.X < boardSize && 
                cell.Y >= 0 && cell.Y < boardSize);
        }
    }

    public static class FleetConfiguration
    {
        public static readonly List<int> StandardFleet = new() { 5, 4, 3, 3, 2 }; // Carrier, Battleship, 2x Cruiser, Destroyer
        
        public static List<Ship> CreateStandardFleet()
        {
            return StandardFleet.Select((length, index) => new Ship(length, index)).ToList();
        }
    }
}
