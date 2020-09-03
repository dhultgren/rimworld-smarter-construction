using System.Collections.Generic;
using Verse;

namespace SmarterConstruction.Core
{
    class EncloseThingsCache
    {
        private readonly Dictionary<Thing, CachedEncloseThingsResult> cache = new Dictionary<Thing, CachedEncloseThingsResult>();

        public EncloseThingsResult GetIfAvailable(Thing target, int maxCacheLength)
        {
            if (cache.TryGetValue(target, out var cachedResult))
            {
                if (cachedResult.CachedAtTick + maxCacheLength > Find.TickManager.TicksGame)
                {
                    return cachedResult.EncloseThingsResult;
                }
                cache.Remove(target);
            }
            return null;
        }

        public void Add(Thing target, EncloseThingsResult result)
        {
            cache[target] = new CachedEncloseThingsResult
            {
                EncloseThingsResult = result,
                CachedAtTick = Find.TickManager.TicksGame
            };
        }

        private class CachedEncloseThingsResult
        {
            public EncloseThingsResult EncloseThingsResult { get; set; }
            public int CachedAtTick { get; set; }
        }
    }

    public class EncloseThingsResult
    {
        public bool EnclosesRegion { get; set; }
        public bool EnclosesThings { get; set; }
        public bool EnclosesSelf { get; set; }
    }
}
