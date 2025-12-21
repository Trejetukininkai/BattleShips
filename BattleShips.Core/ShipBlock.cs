using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
    public class ShipBlock : IShipComponent, IEnumerable<IShipComponent>
    {
        private static int _nextId = 1000; // Start from high number to avoid conflicts with ShipCell IDs

        public int Id { get; private set; }
        public string Name { get; set; } = "Ship Block";
        public Point Position { get; set; }
        public ShipOrientation Orientation { get; set; }
        public int Length => _components.Count;
        public bool IsPlaced => _components.Count > 0;
        public ShipClass Class { get; set; }
        public string ShipType { get; set; } = "";

        private readonly List<IShipComponent> _components = new();

        public ShipBlock()
        {
            Id = _nextId++;
        }

        public ShipBlock(string name) : this()
        {
            Name = name;
        }

        public void AddComponent(IShipComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            _components.Add(component);
        }

        public void RemoveComponent(IShipComponent component)
        {
            _components.Remove(component);
        }

        public IEnumerable<IShipComponent> GetChildren() => _components;

        public List<Point> GetOccupiedCells()
        {
            var cells = new List<Point>();
            foreach (var component in _components)
            {
                cells.AddRange(component.GetOccupiedCells());
            }
            return cells.Distinct().ToList();
        }

        public bool IsSunk() => _components.All(c => c.IsSunk());

        // Create a ship from points
        public static ShipBlock CreateFromPoints(List<Point> points)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("Points cannot be null or empty");

            var shipBlock = new ShipBlock();
            foreach (var point in points)
            {
                shipBlock.AddComponent(new ShipCell(point));
            }

            // Calculate position and orientation
            if (points.Count > 0)
            {
                shipBlock.Position = points[0];

                if (points.Count > 1)
                {
                    // Determine orientation
                    bool allSameX = points.All(p => p.X == points[0].X);
                    bool allSameY = points.All(p => p.Y == points[0].Y);

                    if (allSameX)
                        shipBlock.Orientation = ShipOrientation.Vertical;
                    else if (allSameY)
                        shipBlock.Orientation = ShipOrientation.Horizontal;
                    else
                        shipBlock.Orientation = ShipOrientation.Horizontal; // Default
                }
                else
                {
                    shipBlock.Orientation = ShipOrientation.Horizontal;
                }
            }

            return shipBlock;
        }

        // Create a ship of specific size at starting point
        public static ShipBlock CreateShip(int size, Point startPoint, bool isHorizontal, string shipType = "")
        {
            var shipBlock = new ShipBlock($"Ship (Size {size})")
            {
                Position = startPoint,
                Orientation = isHorizontal ? ShipOrientation.Horizontal : ShipOrientation.Vertical,
                ShipType = shipType
            };

            for (int i = 0; i < size; i++)
            {
                var cellPos = isHorizontal
                    ? new Point(startPoint.X + i, startPoint.Y)
                    : new Point(startPoint.X, startPoint.Y + i);

                shipBlock.AddComponent(new ShipCell(cellPos));
            }
            return shipBlock;
        }

        // Backward compatibility - create from legacy ship interface
        public static ShipBlock FromLegacyShip(int id, Point position, ShipOrientation orientation, int length,
                                              bool isPlaced, ShipClass shipClass, string shipType, List<Point> occupiedCells)
        {
            var shipBlock = new ShipBlock
            {
                Id = id,
                Position = position,
                Orientation = orientation,
                Class = shipClass,
                ShipType = shipType
            };

            // Add cells based on the occupied cells
            foreach (var cell in occupiedCells)
            {
                shipBlock.AddComponent(new ShipCell(cell));
            }

            return shipBlock;
        }

        // IEnumerable implementation
        public IEnumerator<IShipComponent> GetEnumerator() => _components.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}