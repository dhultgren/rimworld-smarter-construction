using System;
using System.Collections.Generic;
using Verse;

namespace SmarterConstruction.Core
{
    class PawnPositionCache
    {
        private static readonly int PawnStuckAfter = 300;
        private static readonly int CacheCleanupFrequency = 2000;

        private static readonly Dictionary<Pawn, Tuple<IntVec3, int>> positionCache = new Dictionary<Pawn, Tuple<IntVec3, int>>();
        private static int lastCacheCleanup = Find.TickManager.TicksGame;

        public static bool IsPawnStuck(Pawn pawn)
        {
            if (pawn?.Position == null) return false;
            CleanCache();

            if (positionCache.ContainsKey(pawn))
            {
                var data = positionCache[pawn];
                if (data.Item1 == pawn.Position)
                {
                    if (data.Item2 <= Find.TickManager.TicksGame)
                    {
                        positionCache.Remove(pawn);
                        return true;
                    }
                    return false;
                }
            }
            positionCache[pawn] = new Tuple<IntVec3, int>(pawn.Position, Find.TickManager.TicksGame + PawnStuckAfter);
            return false;
        }

        private static void CleanCache()
        {
            if (lastCacheCleanup + CacheCleanupFrequency < Find.TickManager.TicksGame)
            {
                positionCache.RemoveAll(d => d.Value.Item2 + CacheCleanupFrequency < Find.TickManager.TicksGame);
                lastCacheCleanup = Find.TickManager.TicksGame;
            }
        }
    }
}
