using System;
using System.Collections.Generic;
using System.Drawing;

namespace BattleShips.Core
{


    public interface IShipComponent
    {
        int Id { get; }
        string Name { get; }
        Point Position { get; }
        ShipOrientation Orientation { get; }
        int Length { get; }
        bool IsPlaced { get; }
        ShipClass Class { get; }
        string ShipType { get; }

        List<Point> GetOccupiedCells();
        bool IsSunk();

        // Composite-specific methods
        void AddComponent(IShipComponent component);
        void RemoveComponent(IShipComponent component);
        IEnumerable<IShipComponent> GetChildren();
    }
}