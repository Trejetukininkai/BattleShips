using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    /// <summary>
    /// Builds and manages the Chain of Responsibility
    /// </summary>
    public sealed class DamageChainDirector
    {
        private IDamageHandler _chain;

        public DamageChainDirector()
        {
            BuildChain();
        }

        private void BuildChain()
        {
            // Create handlers
            var validation = new ValidationDamageHandler();
            var criticalHit = new CriticalHitDamageHandler();
            var regularHit = new RegularHitDamageHandler();
            var shipDestroyed = new ShipDestroyedHandler();
            var miss = new MissDamageHandler();

            // Build the chain: Validation -> Critical Hit -> Regular Hit -> Ship Destroyed -> Miss
            validation.SetNext(criticalHit)
                     .SetNext(regularHit)
                     .SetNext(shipDestroyed)
                     .SetNext(miss);

            _chain = validation;
        }

        public DamageResult ProcessAttack(List<IShip> ships, int targetX, int targetY)
        {
            DamageResult finalResult = null;

            // Try each ship in the chain
            foreach (var ship in ships)
            {
                var result = _chain.HandleDamage(ship, targetX, targetY);

                // If we got a hit (handled with WasHit = true), return it
                if (result.WasHandled && result.WasHit)
                {
                    return result;
                }

                // Keep track of the last result (usually a miss)
                finalResult = result;
            }

            // Return final result (should be a miss if no hits)
            return finalResult ?? new DamageResult
            {
                WasHandled = true,
                WasHit = false,
                Message = $"Miss at ({targetX},{targetY})"
            };
        }

        public void Reset()
        {
            BuildChain(); // Rebuild chain to reset state
        }
    }
}
