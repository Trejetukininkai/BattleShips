using System;
using System.Collections.Generic;
using System.Drawing;

namespace BattleShips.Core
{
    public class ShipCell : IShipComponent
    {
        private static int _nextId = 1;

        public int Id { get; private set; }
        public string Name => $"Ship Cell";
        public Point Position { get; set; }
        public ShipOrientation Orientation => ShipOrientation.Horizontal; // Single cell has no meaningful orientation
        public int Length => 1;
        public bool IsPlaced => true;
        public ShipClass Class => ShipClass.Blocky; // Default
        public string ShipType => "Cell";
        public bool IsHit { get; private set; }

        public ShipCell(Point position)
        {
            Id = _nextId++;
            Position = position;
            IsHit = false;
        }

        public List<Point> GetOccupiedCells() => new() { Position };

        public bool IsSunk() => IsHit;

        public void Hit() => IsHit = true;

        // Leaf implementations
        public void AddComponent(IShipComponent component)
            => throw new InvalidOperationException("Cannot add components to a leaf cell");

        public void RemoveComponent(IShipComponent component)
            => throw new InvalidOperationException("Cannot remove components from a leaf cell");

        public IEnumerable<IShipComponent> GetChildren()
            => Array.Empty<IShipComponent>();
    }
}