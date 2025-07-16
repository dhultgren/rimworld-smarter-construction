using System;
using System.Collections.Generic;
using Verse;

namespace SmarterConstruction.Core
{
    public class PawnPositionCache : MapComponent
    {
        private const int PawnStuckAfter = 300;
        private const int CacheCleanupFrequency = 2000;

        private Dictionary<Pawn, Tuple<IntVec3, int>> _positionCache = new Dictionary<Pawn, Tuple<IntVec3, int>>();
        private int _lastCacheCleanup;

        public PawnPositionCache(Map map) : base(map)
        {
            _lastCacheCleanup = Find.TickManager.TicksGame;
        }

        public bool IsPawnStuck(Pawn pawn)
        {
            if (!SmarterConstruction.Settings.EnableCaching || pawn?.Position == null) return false;
            CleanCache();

            if (_positionCache.ContainsKey(pawn))
            {
                var data = _positionCache[pawn];
                if (data.Item1 == pawn.Position)
                {
                    if (data.Item2 <= Find.TickManager.TicksGame)
                    {
                        _positionCache.Remove(pawn);
                        return true;
                    }
                    return false;
                }
            }
            _positionCache[pawn] = new Tuple<IntVec3, int>(pawn.Position, Find.TickManager.TicksGame + PawnStuckAfter);
            return false;
        }

        private void CleanCache()
        {
            if (_lastCacheCleanup + CacheCleanupFrequency < Find.TickManager.TicksGame)
            {
                _positionCache.RemoveAll(d => d.Value.Item2 + CacheCleanupFrequency < Find.TickManager.TicksGame);
                _lastCacheCleanup = Find.TickManager.TicksGame;
            }
        }

        public static PawnPositionCache GetCache(Map map)
        {
            return map.GetComponent<PawnPositionCache>();
        }
    }
}
