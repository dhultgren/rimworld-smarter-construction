using System.Collections.Generic;
using Verse;

namespace SmarterConstruction.Core
{
    public class EncloseThingsCache : MapComponent
    {
        private readonly Dictionary<Thing, CachedEncloseThingsResult> _cache = new Dictionary<Thing, CachedEncloseThingsResult>();

        public EncloseThingsCache(Map map) : base(map)
        {
        }

        public EncloseThingsResult GetIfAvailable(Thing target, int maxCacheLength)
        {
            if (SmarterConstruction.Settings.EnableCaching && _cache.TryGetValue(target, out var cachedResult))
            {
                if (cachedResult.CachedAtTick + maxCacheLength > Find.TickManager.TicksGame)
                {
                    if (maxCacheLength == 0) DebugUtils.ErrorLog($"Cache length is 0 for {target.Label} at {target.Position}. This should not happen.");
                    return cachedResult.EncloseThingsResult;
                }
                _cache.Remove(target);
            }
            return null;
        }

        public void Add(Thing target, EncloseThingsResult result)
        {
            if (!SmarterConstruction.Settings.EnableCaching) return;
            _cache[target] = new CachedEncloseThingsResult
            {
                EncloseThingsResult = result,
                CachedAtTick = Find.TickManager.TicksGame
            };
        }

        public static EncloseThingsCache GetCache(Map map)
        {
            return map.GetComponent<EncloseThingsCache>();
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
