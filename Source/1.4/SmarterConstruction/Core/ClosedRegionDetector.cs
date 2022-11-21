using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmarterConstruction.Core
{
    public static class ClosedRegionDetector
    {
        private static readonly int TicksBetweenLogs = 100;

        private static readonly EncloseThingsCache cache = new EncloseThingsCache();
        private static int lastLogTick = 0;
        private static int totalChecks = 0;
        private static int totalCacheHits = 0;

        public static EncloseThingsResult WouldEncloseThings(Thing target, Pawn ___pawn, int maxCacheLength)
        {
            /*if (Find.TickManager.TicksGame >= lastLogTick + TicksBetweenLogs)
            {
                lastLogTick = Find.TickManager.TicksGame;
                DebugUtils.DebugLog($"Cache hits {100 * totalCacheHits / (float)(totalChecks + totalCacheHits):F0}%");
            }*/
            if (target?.Position == null || target?.Map?.pathing?.Normal?.pathGrid == null || target?.def == null) return new EncloseThingsResult();

            var cachedResult = cache.GetIfAvailable(target, maxCacheLength);
            if (cachedResult != null)
            {
                totalCacheHits++;
                return cachedResult;
            }

            //totalChecks++;
            var retValue = new EncloseThingsResult();
            var blockedPositions = GenAdj.CellsOccupiedBy(target.Position, target.Rotation, target.def.Size).ToHashSet();
            var closedRegion = ClosedRegionCreatedByAddingImpassable(new PathGridWrapper(target.Map.pathing.Normal.pathGrid), blockedPositions);
            if (closedRegion.Count > 0)
            {
                var enclosedThings = closedRegion.SelectMany(p => p.GetThingList(target.Map)).ToList();
                var hasEnclosedUnacceptable = enclosedThings.Any(t => t is Blueprint || t is Frame);
                var enclosedPlayerPawns = enclosedThings.Where(t => t is Pawn && t.Faction != null && t.Faction.IsPlayer).ToList();

                retValue.EnclosesRegion = true;
                retValue.EnclosesThings = hasEnclosedUnacceptable || enclosedPlayerPawns.Count(p => p != ___pawn) > 0;
                retValue.EnclosesSelf = enclosedPlayerPawns.Any(p => p == ___pawn);
            }
            cache.Add(target, retValue);
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
                if (curRegion.Count > 0 && curRegion.Count < SmarterConstruction.Settings.MaxRegionSize)
                {
                    closedRegion.AddRange(curRegion);
                }
            }
            return closedRegion;
        }

        private static HashSet<IntVec3> FloodFill(IPathGrid pathGrid, IntVec3 start, HashSet<IntVec3> addedBlockers)
        {
            var region = new HashSet<IntVec3>();
            if (!pathGrid.Walkable(start))
            {
                if (NeighborCounter.GetCardinalNeighbors(start).All(p => addedBlockers.Contains(p) || !pathGrid.Walkable(p)))
                {
                    region.Add(start); // Treat a blocked-off unwalkable adjacent cell as its own region, just to check it
                }
                return region;
            }
            var queuedPositions = new Queue<IntVec3>();
            queuedPositions.Enqueue(start);
            while (queuedPositions.Count > 0)
            {
                if (region.Count >= SmarterConstruction.Settings.MaxRegionSize) break;

                var pos = queuedPositions.Dequeue();
                if (!region.Contains(pos) && pathGrid.Walkable(pos) && !addedBlockers.Contains(pos))
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
                if (!pathGrid.Walkable(pos)) return true;
                return false;
            });
            return possiblePositions;
        }
    }
}
