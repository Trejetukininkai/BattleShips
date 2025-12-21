using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    /// <summary>
    /// Fourth handler: Checks if ship is destroyed
    /// </summary>
    public sealed class ShipDestroyedHandler : BaseDamageHandler
    {
        private readonly HashSet<(IShip ship, Point cell)> _hitCells = new();

        public override DamageResult HandleDamage(IShip ship, int x, int y)
        {
            var occupiedCells = ship.GetOccupiedCells();

            // Record the hit
            if (occupiedCells.Any(cell => cell.X == x && cell.Y == y))
            {
                _hitCells.Add((ship, new Point(x, y)));

                // Check if all cells are hit
                bool allCellsHit = occupiedCells.All(cell =>
                    _hitCells.Any(hit => hit.ship == ship && hit.cell.X == cell.X && hit.cell.Y == cell.Y));

                if (allCellsHit)
                {
                    return CreateResult(
                        handled: true,
                        hit: true,
                        destroyed: true,
                        message: $"Ship {ship.GetType().Name} DESTROYED at ({x},{y})!",
                        ship: ship
                    );
                }
            }

            return base.HandleDamage(ship, x, y);
        }
    }
}
