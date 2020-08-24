using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmarterConstruction.Core
{
    public static class ClosedRegionDetector
    {
        public static readonly int MaxRegionSize = 50;

        private static readonly Dictionary<Thing, Tuple<bool, int>> WouldEncloseThingsCache = new Dictionary<Thing, Tuple<bool, int>>();
        private static readonly int EncloseThingCacheTicks = 5;

        private static readonly int TicksBetweenLogs = 50;
        private static int totalChecks = 0;
        private static int totalCacheHits = 0;

        public static bool WouldEncloseThings(Thing target, Pawn ___pawn)
        {
            if (target?.Position == null || target?.Map?.pathGrid == null || target?.def == null) return false;
            if (WouldEncloseThingsCache.ContainsKey(target))
            {
                var tuple = WouldEncloseThingsCache[target];
                if (tuple.Item2 > Find.TickManager.TicksGame)
                {
                    if (++totalCacheHits % TicksBetweenLogs == 0) DebugLog("Cache hit #" + totalCacheHits);
                    return tuple.Item1;
                }
                WouldEncloseThingsCache.Remove(target);
            }

            if (++totalChecks % TicksBetweenLogs == 0) DebugLog("Enclose check #" + totalChecks);
            var retValue = false;
            var blockedPositions = GenAdj.CellsOccupiedBy(target.Position, target.Rotation, target.def.Size).ToHashSet();
            var closedRegion = ClosedRegionCreatedByAddingImpassable(new PathGridWrapper(target.Map.pathGrid), blockedPositions);
            if (closedRegion.Count > 0)
            {
                var enclosedThings = closedRegion.SelectMany(p => p.GetThingList(target.Map)).ToList();
                var enclosedUnacceptable = enclosedThings.Where(t => t is Blueprint || t is Frame).ToList();
                var enclosedPlayerPawns = enclosedThings.Where(t => t is Pawn && t.Faction != null && t.Faction.IsPlayer).ToList();
                // TODO: move pawn if it detects that it's going to enclose itself?
                /*if (enclosedUnacceptable.Count == 0 && enclosedPlayerPawns.Count == 1 && enclosedPlayerPawns[0] == ___pawn)
                {
                    var pos = FindSafeConstructionSpot(target);
                    if (pos.IsValid)
                    {
                        ___pawn.SetPositionDirect(pos); // TODO: actually walk to safe position
                    }
                }*/

                retValue = enclosedUnacceptable.Count > 0 || enclosedPlayerPawns.Count > 0;
            }
            WouldEncloseThingsCache[target] = new Tuple<bool, int>(retValue, Find.TickManager.TicksGame + EncloseThingCacheTicks);
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
            if (!pathGrid.Walkable(start)) return region;

            var queuedPositions = new Queue<IntVec3>();
            queuedPositions.Enqueue(start);
            while (queuedPositions.Count > 0)
            {
                if (region.Count >= MaxRegionSize) break;

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

        private static IntVec3 FindSafeConstructionSpot(Thing target)
        {
            var possiblePositions = Enumerable.Range(-1, 3)
                .SelectMany(y => Enumerable.Range(-1, 3)
                    .Where(x => !(y == 0 && x == 0))
                    .Select(x => new IntVec3(x, 0, y)))
                .ToList();

            foreach (var pos in possiblePositions)
            {
                if (!WouldEncloseThings(target, null)) return pos;
            }
            return IntVec3.Invalid;
        }

        private static void DebugLog(string text)
        {
#if DEBUG
            //Log.Message(text, true);
#endif
        }
    }
}
