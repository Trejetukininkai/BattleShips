using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    /// <summary>
    /// Base class for Chain of Responsibility handlers
    /// </summary>
    public abstract class BaseDamageHandler : IDamageHandler
    {
        private IDamageHandler _nextHandler;

        public IDamageHandler SetNext(IDamageHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }

        public virtual DamageResult HandleDamage(IShip ship, int x, int y)
        {
            // Pass to next handler in chain
            if (_nextHandler != null)
            {
                return _nextHandler.HandleDamage(ship, x, y);
            }

            // End of chain - no one handled it
            return new DamageResult
            {
                WasHandled = false,
                Message = "No damage handler could process the attack"
            };
        }

        protected DamageResult CreateResult(bool handled, bool hit, bool destroyed, string message, IShip ship)
        {
            return new DamageResult
            {
                WasHandled = handled,
                WasHit = hit,
                ShipDestroyed = destroyed,
                Message = message,
                AffectedShip = ship
            };
        }
    }
}
