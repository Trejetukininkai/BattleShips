using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    /// <summary>
    /// Second handler: Checks for critical hits (first cell of ship)
    /// </summary>
    public sealed class CriticalHitDamageHandler : BaseDamageHandler
    {
        public override DamageResult HandleDamage(IShip ship, int x, int y)
        {
            var occupiedCells = ship.GetOccupiedCells();

            // Check if this is the first cell (critical location)
            var firstCell = occupiedCells.FirstOrDefault();
            if (firstCell.X == x && firstCell.Y == y)
            {
                return CreateResult(
                    handled: true,
                    hit: true,
                    destroyed: false,
                    message: $"CRITICAL HIT on {ship.GetType().Name} at ({x},{y})!",
                    ship: ship
                );
            }

            // Not a critical hit, pass to next
            return base.HandleDamage(ship, x, y);
        }
    }
}
