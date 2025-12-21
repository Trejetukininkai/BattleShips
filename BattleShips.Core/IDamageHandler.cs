using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    /// <summary>
    /// Handler interface for Chain of Responsibility pattern
    /// </summary>
    public interface IDamageHandler
    {
        IDamageHandler SetNext(IDamageHandler handler);
        DamageResult HandleDamage(IShip ship, int x, int y);
    }

    /// <summary>
    /// Result of damage processing
    /// </summary>
    public class DamageResult
    {
        public bool WasHandled { get; set; }
        public bool WasHit { get; set; }
        public bool ShipDestroyed { get; set; }
        public string Message { get; set; }
        public IShip AffectedShip { get; set; }
    }
}
