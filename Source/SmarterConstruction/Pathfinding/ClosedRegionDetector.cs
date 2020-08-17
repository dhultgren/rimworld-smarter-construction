using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmarterConstruction.Pathfinding
{
    public static class ClosedRegionDetector
    {
        public static readonly int MaxRegionSize = 50;

        private static readonly Dictionary<Thing, Tuple<bool, int>> WouldEncloseThingsCache = new Dictionary<Thing, Tuple<bool, int>>();
        private static readonly int EncloseThingCacheTicks = 5;

        public static bool WouldEncloseThings(Thing target, Pawn ___pawn)
        {
            if (target?.Position == null || target?.Map?.pathGrid == null) return false;
            if (WouldEncloseThingsCache.ContainsKey(target))
            {
                var tuple = WouldEncloseThingsCache[target];
                if (tuple.Item2 > Find.TickManager.TicksGame)
                {
                    return tuple.Item1;
                }
                WouldEncloseThingsCache.Remove(target);
            }

            var retValue = false;
            var closedRegion = ClosedRegionCreatedByAddingImpassable(new PathGridWrapper(target.Map.pathGrid), target.Position);
            if (closedRegion.Count > 0)
            {
                var enclosedThings = closedRegion.SelectMany(p => p.GetThingList(target.Map)).ToList();
                var enclosedUnacceptable = enclosedThings.Where(t => t is Blueprint || t is Frame).ToList();
                var enclosedPlayerPawns = enclosedThings.Where(t => t is Pawn && t.Faction != null && t.Faction.IsPlayer).ToList();
                // TODO: allow pawn to be enclosed if player forced
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

        public static HashSet<IntVec3> ClosedRegionCreatedByAddingImpassable(IPathGrid pathGrid, IntVec3 addedBlocker)
        {
            var neighbors = GetCardinalNeighbors(addedBlocker);
            var closedRegion = new HashSet<IntVec3>();
            var curRegion = new HashSet<IntVec3>();
            foreach(var pos in neighbors)
            {
                if (curRegion.Contains(pos)) continue;
                curRegion = FloodFill(pathGrid, pos, addedBlocker);
                if (curRegion.Count > 0 && curRegion.Count < MaxRegionSize)
                {
                    closedRegion.AddRange(curRegion);
                }
            }
            return closedRegion;
        }

        public static HashSet<IntVec3> FindClosedRegion(IPathGrid pathGrid, IntVec3 start, IntVec3 addedBlocker)
        {
            var region = FloodFill(pathGrid, start, addedBlocker);
            return region.Count >= MaxRegionSize ? new HashSet<IntVec3>() : region;
        }

        private static int fills = 0;

        private static HashSet<IntVec3> FloodFill(IPathGrid pathGrid, IntVec3 start, IntVec3 addedBlocker)
        {
            var region = new HashSet<IntVec3>();
            if (!pathGrid.Walkable(start)) return region;

            var queuedPositions = new Queue<IntVec3>();
            queuedPositions.Enqueue(start);
            if (++fills % 100 == 0) Log.Message("Floodfill #" + fills);
            while (queuedPositions.Count > 0)
            {
                if (region.Count >= MaxRegionSize) break;

                var pos = queuedPositions.Dequeue();
                if (!region.Contains(pos) && pathGrid.Walkable(pos) && pos != addedBlocker)
                {
                    region.Add(pos);
                    var neighbors = GetCardinalNeighbors(pos);
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

        private static IEnumerable<IntVec3> GetCardinalNeighbors(IntVec3 pos)
        {
            yield return new IntVec3(pos.x - 1, pos.y, pos.z);
            yield return new IntVec3(pos.x, pos.y, pos.z - 1);
            yield return new IntVec3(pos.x + 1, pos.y, pos.z);
            yield return new IntVec3(pos.x, pos.y, pos.z + 1);
        }
    }
}
