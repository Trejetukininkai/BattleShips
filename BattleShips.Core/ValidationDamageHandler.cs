using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    /// <summary>
    /// First handler: Validates if coordinates are within ship bounds
    /// </summary>
    public sealed class ValidationDamageHandler : BaseDamageHandler
    {
        public override DamageResult HandleDamage(IShip ship, int x, int y)
        {
            // Validate ship exists
            if (ship == null)
            {
                return CreateResult(true, false, false, "No ship at target location", null);
            }

            // Check if coordinates are within ship's occupied cells
            var occupiedCells = ship.GetOccupiedCells();
            if (!occupiedCells.Any(cell => cell.X == x && cell.Y == y))
            {
                // Not this ship's coordinates, pass to next
                return base.HandleDamage(ship, x, y);
            }

            // Coordinates valid, pass to next handler
            return base.HandleDamage(ship, x, y);
        }
    }
}
