using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmarterConstruction.Core
{
    public static class ClosedRegionDetector
    {
        public static readonly int MaxRegionSize = 50;

        private static readonly Dictionary<Thing, CachedEncloseThingsResult> WouldEncloseThingsCache = new Dictionary<Thing, CachedEncloseThingsResult>();
        private static readonly int EncloseThingCacheTicks = 5;

        private static readonly int TicksBetweenLogs = 50;
        private static int totalChecks = 0;
        private static int totalCacheHits = 0;

        public static EncloseThingsResult WouldEncloseThings(Thing target, Pawn ___pawn)
        {
            if (target?.Position == null || target?.Map?.pathGrid == null || target?.def == null) return new EncloseThingsResult();
            if (WouldEncloseThingsCache.ContainsKey(target))
            {
                var cachedResult = WouldEncloseThingsCache[target];
                if (cachedResult.ExpiresAtTick > Find.TickManager.TicksGame)
                {
                    //if (++totalCacheHits % TicksBetweenLogs == 0) DebugUtils.DebugLog("Cache hit #" + totalCacheHits);
                    return cachedResult.EncloseThingsResult;
                }
                WouldEncloseThingsCache.Remove(target);
            }

            //if (++totalChecks % TicksBetweenLogs == 0) DebugUtils.DebugLog("Enclose check #" + totalChecks);
            var retValue = new EncloseThingsResult();
            var blockedPositions = GenAdj.CellsOccupiedBy(target.Position, target.Rotation, target.def.Size).ToHashSet();
            var closedRegion = ClosedRegionCreatedByAddingImpassable(new PathGridWrapper(target.Map.pathGrid), blockedPositions);
            if (closedRegion.Count > 0)
            {
                var enclosedThings = closedRegion.SelectMany(p => p.GetThingList(target.Map)).ToList();
                var enclosedUnacceptable = enclosedThings.Where(t => t is Blueprint || t is Frame).ToList();
                var enclosedPlayerPawns = enclosedThings.Where(t => t is Pawn && t.Faction != null && t.Faction.IsPlayer).ToList();

                retValue.EnclosesRegion = true;
                retValue.EnclosesThings = enclosedUnacceptable.Count > 0 || enclosedPlayerPawns.Count > 0;
            }
            WouldEncloseThingsCache[target] = new CachedEncloseThingsResult
            {
                EncloseThingsResult = retValue,
                ExpiresAtTick = Find.TickManager.TicksGame + EncloseThingCacheTicks
            };
            return retValue;
        }

        public static HashSet<IntVec3> ClosedRegionCreatedByAddingImpassable(IPathGrid pathGrid, HashSet<IntVec3> addedBlockers)
        {
            var neighbors = NeighborCounter.GetCardinalNeighbors(addedBlockers);
            var closedRegion = new HashSet<IntVec3>();
            var curRegion = new HashSet<IntVec3>();
            foreach (var pos in neighbors)
            {
                if (curRegion.Contains(pos)) continue;
                curRegion = FloodFill(pathGrid, pos, addedBlockers);
                if (curRegion.Count > 0 && curRegion.Count < MaxRegionSize)
                {
                    closedRegion.AddRange(curRegion);
                }
            }
            return closedRegion;
        }

        private static HashSet<IntVec3> FloodFill(IPathGrid pathGrid, IntVec3 start, HashSet<IntVec3> addedBlockers)
        {
            var region = new HashSet<IntVec3>();
            if (!pathGrid.WalkableFast(start)) return region;

            var queuedPositions = new Queue<IntVec3>();
            queuedPositions.Enqueue(start);
            while (queuedPositions.Count > 0)
            {
                if (region.Count >= MaxRegionSize) break;

                var pos = queuedPositions.Dequeue();
                if (!region.Contains(pos) && pathGrid.WalkableFast(pos) && !addedBlockers.Contains(pos))
                {
                    region.Add(pos);
                    var neighbors = NeighborCounter.GetCardinalNeighbors(pos);
                    foreach (var n in neighbors) queuedPositions.Enqueue(n);
                }
            }
            return region;
        }

        public static HashSet<IntVec3> FindSafeConstructionSpots(IPathGrid pathGrid, Thing target)
        {
            var blockedPositions = GenAdj.CellsOccupiedBy(target.Position, target.Rotation, target.def.Size).ToHashSet();
            return FindSafeConstructionSpots(pathGrid, blockedPositions);
        }

        public static HashSet<IntVec3> FindSafeConstructionSpots(IPathGrid pathGrid, HashSet<IntVec3> addedBlockers)
        {
            var possiblePositions = NeighborCounter.GetAllNeighbors(addedBlockers);
            var enclosedPositions = ClosedRegionCreatedByAddingImpassable(pathGrid, addedBlockers);
            possiblePositions.RemoveWhere(pos =>
            {
                if (enclosedPositions.Contains(pos)) return true;
                if (!pathGrid.WalkableFast(pos)) return true;
                return false;
            });
            return possiblePositions;
        }

        private class CachedEncloseThingsResult
        {
            public EncloseThingsResult EncloseThingsResult { get; set; }
            public int ExpiresAtTick { get; set; }

        }
    }

    public class EncloseThingsResult
    {
        public bool EnclosesRegion { get; set; }
        public bool EnclosesThings { get; set; }
    }
}
