using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    /// <summary>
    /// Final handler: Processes misses
    /// </summary>
    public sealed class MissDamageHandler : BaseDamageHandler
    {
        public override DamageResult HandleDamage(IShip ship, int x, int y)
        {
            // This is the end of the chain - it's a miss
            return CreateResult(
                handled: true,
                hit: false,
                destroyed: false,
                message: $"Miss at ({x},{y})",
                ship: null
            );
        }
    }
}
