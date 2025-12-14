using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{

    public enum MineTriggerType { EnemyShot, Disaster }

    public enum MineCategory
    {
        AntiEnemy_Restore,
        AntiEnemy_Ricochet,
        AntiDisaster_Restore,
        AntiDisaster_Ricochet
    }

    // Bridge implement
    public interface ISpecialEffect
    {
        // Execute effect. ownerConnId is the player who owns the mine.
        // instance is server-side game instance (may be null on pure-client preview).
        // activationPoint is where the mine detonated (board cell).
        // Returns an optional set of Points modified by the effect (for synchronisation / UI).
        List<Point> ActivateEffect(GameInstance? instance, string ownerConnId, Point activationPoint);
    }

    // Concrete effects
    public class RestoreShipEffect : ISpecialEffect
    {
        private readonly int tilesToHeal;

        public RestoreShipEffect(int healTiles = 2)
        {
            tilesToHeal = Math.Max(1, healTiles);
        }

        // Heals up to tilesToHeal 'hit' cells on one of the owner's ships containing activationPoint.
        // Server-side: this will remove hit marks from owner's hit set.
        public List<Point> ActivateEffect(GameInstance? instance, string ownerConnId, Point activationPoint)
        {
            var healed = new List<Point>();
            Console.WriteLine($"[RestoreEffect] 🔧 Activating at ({activationPoint.X},{activationPoint.Y}) for owner {ownerConnId}");

            if (instance == null)
            {
                Console.WriteLine($"[RestoreEffect] ❌ No game instance");
                return healed;
            }

            // Find owner's ships and hit set
            var ships = instance.PlayerA == ownerConnId ? instance.ShipsA : instance.ShipsB;
            var hitCells = instance.PlayerA == ownerConnId ? instance.HitCellsA : instance.HitCellsB;

            Console.WriteLine($"[RestoreEffect] 📊 Owner has {ships.Count} ships, {hitCells.Count} hit cells");

            // for debugging
            Console.WriteLine($"[RestoreEffect] 🎯 Hit cells: {string.Join(", ", hitCells.Select(p => $"({p.X},{p.Y})"))}");

            // Find ship that contains activationPoint
            var targetShip = ships.FirstOrDefault(s => s.IsPlaced && s.GetOccupiedCells().Contains(activationPoint));
            if (targetShip == null)
            {
                Console.WriteLine($"[RestoreEffect] ❌ No ship at activation point ({activationPoint.X},{activationPoint.Y})");
                Console.WriteLine($"[RestoreEffect] 🔍 Looking for any damaged ship...");

                // If no ship at the cell, pick the nearest damaged ship (heuristic)
                targetShip = ships.FirstOrDefault(s => s.IsPlaced && s.GetOccupiedCells().Any(hitCells.Contains));
                if (targetShip == null)
                {
                    Console.WriteLine($"[RestoreEffect] ❌ No damaged ships found at all");
                    return healed;
                }
                Console.WriteLine($"[RestoreEffect] ✅ Found damaged ship (not at activation point)");
            }
            else
            {
                Console.WriteLine($"[RestoreEffect] ✅ Found ship at activation point with {targetShip.Length} cells");
            }

            var shipCells = targetShip.GetOccupiedCells();
            var damagedCells = shipCells.Where(hitCells.Contains).ToList();
            Console.WriteLine($"[RestoreEffect] 📊 Ship cells: {string.Join(", ", shipCells.Select(p => $"({p.X},{p.Y})"))}");
            Console.WriteLine($"[RestoreEffect] 🩹 Damaged cells on target ship: {string.Join(", ", damagedCells.Select(p => $"({p.X},{p.Y})"))}");

            // heal up to tilesToHeal
            foreach (var p in damagedCells.Take(tilesToHeal))
            {
                bool removed = hitCells.Remove(p);
                healed.Add(p);
                Console.WriteLine($"[RestoreEffect] ✅ Healed cell ({p.X},{p.Y}) - removed from hitCells: {removed}");
            }

            Console.WriteLine($"[RestoreEffect] 🎉 Total healed: {healed.Count} cells");
            Console.WriteLine($"[RestoreEffect] 📊 Remaining hit cells: {hitCells.Count}");

            return healed;
        }
    }

    public class RicochetEffect : ISpecialEffect
    {
        public List<Point> ActivateEffect(GameInstance? instance, string ownerConnId, Point activationPoint)
        {
            Console.WriteLine("Ricochet mine triggered Ricochet mine triggered Ricochet mine triggered Ricochet mine triggered");
            return new List<Point>();
        }
    }

    // Adapter pattern: Converts Ricochet effect into a Meteor Strike disaster
    public class RicochetToMeteorStrikeAdapter : ISpecialEffect
    {
        private readonly RicochetEffect _ricochetEffect;

        public RicochetToMeteorStrikeAdapter(RicochetEffect ricochetEffect)
        {
            _ricochetEffect = ricochetEffect;
        }

        public List<Point> ActivateEffect(GameInstance? instance, string ownerConnId, Point activationPoint)
        {
            Console.WriteLine($"[Adapter] 🔥 Converting Ricochet to Meteor Strike at ({activationPoint.X},{activationPoint.Y})");

            // execute the original ricochet effect
            var ricochetResult = _ricochetEffect.ActivateEffect(instance, ownerConnId, activationPoint);

            // adapt to meteor strike disaster
            var meteorStrikePoints = ToDisaster(activationPoint);

            Console.WriteLine($"[Adapter] 🌋 Meteor Strike created with {meteorStrikePoints.Count} impact points");

            // Combine both effects
            var combinedResult = new List<Point>();
            combinedResult.AddRange(ricochetResult);
            combinedResult.AddRange(meteorStrikePoints);

            return combinedResult;
        }

        // Adapter method: Convert position to meteor strike disaster area
        public List<Point> ToDisaster(Point centerPoint)
        {
            var meteorStrike = new List<Point>();

            // Create a 3x3 meteor strike pattern around the center point
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var strikePoint = new Point(centerPoint.X + dx, centerPoint.Y + dy);

                    // Check if point is within board bounds
                    if (strikePoint.X >= 0 && strikePoint.X < Board.Size &&
                        strikePoint.Y >= 0 && strikePoint.Y < Board.Size)
                    {
                        meteorStrike.Add(strikePoint);
                        Console.WriteLine($"[Adapter] Meteor strike at ({strikePoint.X},{strikePoint.Y})");
                    }
                }
            }

            return meteorStrike;
        }
    }

    public abstract class NavalMine
    {
        // FLYWEIGHT PATTERN: Extrinsic (unique) data - different for each mine instance
        public Guid Id { get; } = Guid.NewGuid();
        public Point Position { get; }
        public string OwnerConnId { get; }
        public bool IsExploded { get; private set; } = false;
        public DateTime PlacedAt { get; } = DateTime.UtcNow;

        // FLYWEIGHT PATTERN: Reference to shared flyweight containing intrinsic data
        private readonly MineEffectFlyweight _flyweight;

        /// <summary>
        /// FLYWEIGHT PATTERN: Access to shared effect (from flyweight)
        /// </summary>
        public ISpecialEffect Effect => _flyweight.Effect;

        /// <summary>
        /// FLYWEIGHT PATTERN: Access to shared category (from flyweight)
        /// </summary>
        public MineCategory Category => _flyweight.Category;

        protected NavalMine(Point pos, string ownerConnId, MineEffectFlyweight flyweight)
        {
            Position = pos;
            OwnerConnId = ownerConnId;
            _flyweight = flyweight ?? throw new ArgumentNullException(nameof(flyweight));
        }

        /// <summary>
        /// Legacy constructor for backward compatibility (creates flyweight internally)
        /// </summary>
        [Obsolete("Use constructor with MineEffectFlyweight for better memory efficiency")]
        protected NavalMine(Point pos, string ownerConnId, ISpecialEffect effect, MineCategory category)
        {
            Position = pos;
            OwnerConnId = ownerConnId;
            // Create a temporary flyweight (not cached) for backward compatibility
            _flyweight = new MineEffectFlyweight(effect, category, 
                category == MineCategory.AntiEnemy_Restore || category == MineCategory.AntiEnemy_Ricochet);
        }

        // Called to attempt trigger; returns points affected by effect (for networking/UI) or null if not triggered.
        public List<Point>? TryTrigger(GameInstance instance, MineTriggerType triggerType, string triggeringConnId)
        {
            if (IsExploded) return null;

            if (!ShouldTrigger(triggerType, triggeringConnId))
                return null;

            IsExploded = true;

            // FLYWEIGHT PATTERN: Use shared effect from flyweight
            return _flyweight.Effect.ActivateEffect(instance, OwnerConnId, Position);
        }

        protected abstract bool ShouldTrigger(MineTriggerType triggerType, string triggeringConnId);

        /// <summary>
        /// FLYWEIGHT PATTERN: Gets the trigger type this mine responds to (from flyweight)
        /// </summary>
        protected MineTriggerType GetTriggerType() => _flyweight.TriggerType;
    }

    // Triggers only when hit by enemy (EnemyShot)
    public class AntiEnemyMine : NavalMine
    {
        /// <summary>
        /// FLYWEIGHT PATTERN: Constructor using flyweight
        /// </summary>
        public AntiEnemyMine(Point pos, string ownerConnId, MineEffectFlyweight flyweight)
            : base(pos, ownerConnId, flyweight)
        { }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        [Obsolete("Use constructor with MineEffectFlyweight")]
        public AntiEnemyMine(Point pos, string ownerConnId, ISpecialEffect effect, MineCategory category)
            : base(pos, ownerConnId, effect, category)
        { }

        protected override bool ShouldTrigger(MineTriggerType triggerType, string triggeringConnId)
        {
            Console.WriteLine($"[Mine] Checking trigger: type={triggerType}, triggerer={triggeringConnId}, owner={OwnerConnId}");

            // FLYWEIGHT PATTERN: Use trigger type from flyweight
            if (triggerType != GetTriggerType())
            {
                Console.WriteLine($"[Mine] Wrong trigger type (expected {GetTriggerType()}, got {triggerType})");
                return false;
            }
            if (triggeringConnId == OwnerConnId)
            {
                Console.WriteLine($"[Mine] Triggered by owner - ignoring");
                return false;
            }

            Console.WriteLine($"[Mine] ✅ Mine should trigger!");
            return true;
        }
    }

    // Triggers only when hit by disaster (Disaster)
    public class AntiDisasterMine : NavalMine
    {
        /// <summary>
        /// FLYWEIGHT PATTERN: Constructor using flyweight
        /// </summary>
        public AntiDisasterMine(Point pos, string ownerConnId, MineEffectFlyweight flyweight)
            : base(pos, ownerConnId, flyweight)
        { }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        [Obsolete("Use constructor with MineEffectFlyweight")]
        public AntiDisasterMine(Point pos, string ownerConnId, ISpecialEffect effect, MineCategory category)
            : base(pos, ownerConnId, effect, category)
        { }

        protected override bool ShouldTrigger(MineTriggerType triggerType, string triggeringConnId)
        {
            Console.WriteLine($"[AntiDisasterMine] Checking trigger: type={triggerType}, triggerer={triggeringConnId}, owner={OwnerConnId}");

            // FLYWEIGHT PATTERN: Use trigger type from flyweight
            bool shouldTrigger = triggerType == GetTriggerType();
            Console.WriteLine($"[AntiDisasterMine] Should trigger: {shouldTrigger} (expected {GetTriggerType()})");

            return shouldTrigger;
        }
    }

    // Factory to create mines from category chosen by player
    public static class NavalMineFactory
    {
        /// <summary>
        /// FLYWEIGHT PATTERN: Creates a mine using shared flyweight for effects.
        /// Multiple mines of the same category will share the same effect instance.
        /// </summary>
        public static NavalMine CreateMine(Point pos, string ownerConnId, MineCategory category)
        {
            // FLYWEIGHT PATTERN: Get or create shared flyweight (cached by category)
            var flyweight = MineFlyweightFactory.GetFlyweight(category);

            // Create mine with shared flyweight
            return category switch
            {
                MineCategory.AntiEnemy_Restore => new AntiEnemyMine(pos, ownerConnId, flyweight),
                MineCategory.AntiEnemy_Ricochet => new AntiEnemyMine(pos, ownerConnId, flyweight),
                MineCategory.AntiDisaster_Restore => new AntiDisasterMine(pos, ownerConnId, flyweight),
                MineCategory.AntiDisaster_Ricochet => new AntiDisasterMine(pos, ownerConnId, flyweight),
                _ => new AntiEnemyMine(pos, ownerConnId, flyweight),
            };
        }

        /// <summary>
        /// Gets statistics about flyweight usage (for debugging/monitoring)
        /// </summary>
        public static string GetFlyweightStatistics()
        {
            return MineFlyweightFactory.GetCacheStatistics();
        }
    }
}
