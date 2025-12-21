using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    /// <summary>
    /// Third handler: Processes regular hits
    /// </summary>
    public sealed class RegularHitDamageHandler : BaseDamageHandler
    {
        public override DamageResult HandleDamage(IShip ship, int x, int y)
        {
            var occupiedCells = ship.GetOccupiedCells();

            // Check if coordinates match any ship cell
            if (occupiedCells.Any(cell => cell.X == x && cell.Y == y))
            {
                return CreateResult(
                    handled: true,
                    hit: true,
                    destroyed: false,
                    message: $"Hit on {ship.GetType().Name} at ({x},{y})",
                    ship: ship
                );
            }

            // Not a hit on this ship, pass to next
            return base.HandleDamage(ship, x, y);
        }
    }
}
