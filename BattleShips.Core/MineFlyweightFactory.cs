using System;
using System.Collections.Generic;

namespace BattleShips.Core
{
    /// <summary>
    /// FLYWEIGHT PATTERN: Factory that creates and caches flyweight instances.
    /// Ensures that mines with the same category share the same effect instance.
    /// </summary>
    public static class MineFlyweightFactory
    {
        // Cache of flyweight instances by category
        private static readonly Dictionary<MineCategory, MineEffectFlyweight> _flyweightCache = new();

        /// <summary>
        /// Gets or creates a flyweight for the specified mine category.
        /// If a flyweight already exists for this category, it is reused.
        /// </summary>
        public static MineEffectFlyweight GetFlyweight(MineCategory category)
        {
            // Check cache first
            if (_flyweightCache.TryGetValue(category, out var cached))
            {
                return cached;
            }

            // Create new flyweight and cache it
            var flyweight = CreateFlyweight(category);
            _flyweightCache[category] = flyweight;
            
            Console.WriteLine($"[FlyweightFactory] Created and cached flyweight for {category}");
            return flyweight;
        }

        /// <summary>
        /// Creates a new flyweight instance for the category.
        /// This is called only when the flyweight doesn't exist in cache.
        /// </summary>
        private static MineEffectFlyweight CreateFlyweight(MineCategory category)
        {
            ISpecialEffect effect = category switch
            {
                MineCategory.AntiEnemy_Restore => new RestoreShipEffect(2),
                MineCategory.AntiEnemy_Ricochet => new RicochetToMeteorStrikeAdapter(new RicochetEffect()),
                MineCategory.AntiDisaster_Restore => new RestoreShipEffect(2),
                MineCategory.AntiDisaster_Ricochet => new RicochetToMeteorStrikeAdapter(new RicochetEffect()),
                _ => new RicochetToMeteorStrikeAdapter(new RicochetEffect())
            };

            bool isAntiEnemy = category switch
            {
                MineCategory.AntiEnemy_Restore => true,
                MineCategory.AntiEnemy_Ricochet => true,
                MineCategory.AntiDisaster_Restore => false,
                MineCategory.AntiDisaster_Ricochet => false,
                _ => true
            };

            return new MineEffectFlyweight(effect, category, isAntiEnemy);
        }

        /// <summary>
        /// Gets the number of cached flyweights (for debugging/monitoring)
        /// </summary>
        public static int CachedFlyweightCount => _flyweightCache.Count;

        /// <summary>
        /// Clears the flyweight cache (useful for testing or reset)
        /// </summary>
        public static void ClearCache()
        {
            _flyweightCache.Clear();
            Console.WriteLine("[FlyweightFactory] Cache cleared");
        }

        /// <summary>
        /// Gets statistics about the flyweight cache
        /// </summary>
        public static string GetCacheStatistics()
        {
            return $"Cached flyweights: {_flyweightCache.Count}";
        }
    }
}

