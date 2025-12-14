using System;

namespace BattleShips.Core
{
    /// <summary>
    /// FLYWEIGHT PATTERN: Stores intrinsic (shared) data for mines.
    /// Multiple mines can share the same flyweight instance to reduce memory usage.
    /// 
    /// Intrinsic (shared) data:
    /// - ISpecialEffect: The effect instance (can be shared across many mines)
    /// - MineCategory: The category type
    /// - Trigger type logic: Whether it's AntiEnemy or AntiDisaster
    /// </summary>
    public class MineEffectFlyweight
    {
        /// <summary>
        /// Shared effect instance - can be reused by many mines
        /// </summary>
        public ISpecialEffect Effect { get; }

        /// <summary>
        /// Mine category (shared across mines of same type)
        /// </summary>
        public MineCategory Category { get; }

        /// <summary>
        /// Whether this mine triggers on enemy shots (true) or disasters (false)
        /// </summary>
        public bool IsAntiEnemy { get; }

        /// <summary>
        /// Creates a flyweight with shared mine effect data
        /// </summary>
        public MineEffectFlyweight(ISpecialEffect effect, MineCategory category, bool isAntiEnemy)
        {
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            Category = category;
            IsAntiEnemy = isAntiEnemy;
        }

        /// <summary>
        /// Gets the trigger type this mine responds to
        /// </summary>
        public MineTriggerType TriggerType => IsAntiEnemy ? MineTriggerType.EnemyShot : MineTriggerType.Disaster;

        public override string ToString()
        {
            return $"Flyweight[{Category}, Trigger={TriggerType}]";
        }
    }
}

