using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmarterConstruction.Core
{
    public static class ClosedRegionDetector
    {
        public static EncloseThingsResult WouldEncloseThings(Thing target, Pawn ___pawn, int maxCacheLength)
        {
            if (target?.Position == null || target?.Map?.pathing?.Normal?.pathGrid == null || target?.def == null) return new EncloseThingsResult();

            var cache = EncloseThingsCache.GetCache(target.Map);
            var cachedResult = cache.GetIfAvailable(target, maxCacheLength);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var retValue = new EncloseThingsResult();
            var blockedPositions = GenAdj.CellsOccupiedBy(target.Position, target.Rotation, target.def.Size).ToHashSet();
            var closedRegion = ClosedRegionCreatedByAddingImpassable(target.Map.GetComponent<WalkabilityHandler>(), blockedPositions);
            if (closedRegion.Count > 0)
            {
                var enclosedThings = closedRegion.SelectMany(p => p.GetThingList(target.Map)).ToList();
                var hasEnclosedUnacceptable = enclosedThings.Any(t => t is Blueprint || t is Frame);
                var enclosedPlayerPawns = enclosedThings.Where(t => t is Pawn && t.Faction != null && t.Faction.IsPlayer).ToList();

                retValue.EnclosesRegion = true;
                retValue.EnclosesThings = hasEnclosedUnacceptable || enclosedPlayerPawns.Count(p => p != ___pawn) > 0;
                retValue.EnclosesSelf = enclosedPlayerPawns.Any(p => p == ___pawn);

                /*DebugUtils.VerboseLog($"Enclose check for {target.Label} at {target.Position} by pawn {___pawn.Label}: " +
                    $"EnclosesRegion={retValue.EnclosesRegion}, EnclosesThings={retValue.EnclosesThings}, EnclosesSelf={retValue.EnclosesSelf}" +
                    $" EnclosedThingsTypes=[{string.Join(", ", enclosedThings.Select(t => t.GetType().Name))}]" +
                    $" ClosedRegion=[{string.Join(", ", closedRegion.Select(v => v.ToString()))}]");*/
            }
            cache.Add(target, retValue);
            return retValue;
        }

        public static HashSet<IntVec3> ClosedRegionCreatedByAddingImpassable(IWalkabilityHandler walkabilityHandler, HashSet<IntVec3> addedBlockers)
        {
            var neighbors = NeighborCounter.GetCardinalNeighbors(addedBlockers);
            var closedRegion = new HashSet<IntVec3>();
            var curRegion = new HashSet<IntVec3>();
            foreach (var pos in neighbors)
            {
                if (curRegion.Contains(pos)) continue;
                curRegion = FloodFill(walkabilityHandler, pos, addedBlockers);
                if (curRegion.Count > 0 && curRegion.Count < SmarterConstruction.Settings.MaxRegionSize)
                {
                    closedRegion.AddRange(curRegion);
                }
            }
            return closedRegion;
        }

        private static HashSet<IntVec3> FloodFill(IWalkabilityHandler walkabilityHandler, IntVec3 start, HashSet<IntVec3> addedBlockers)
        {
            var region = new HashSet<IntVec3>();
            if (!walkabilityHandler.Walkable(start))
            {
                if (NeighborCounter.GetCardinalNeighbors(start).All(p => addedBlockers.Contains(p) || !walkabilityHandler.Walkable(p)))
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
                if (!region.Contains(pos) && walkabilityHandler.Walkable(pos) && !addedBlockers.Contains(pos))
                {
                    region.Add(pos);
                    var neighbors = NeighborCounter.GetCardinalNeighbors(pos);
                    foreach (var n in neighbors) queuedPositions.Enqueue(n);
                }
            }
            return region;
        }

        public static HashSet<IntVec3> FindSafeConstructionSpots(Thing target)
        {
            var blockedPositions = GenAdj.CellsOccupiedBy(target.Position, target.Rotation, target.def.Size).ToHashSet();
            return FindSafeConstructionSpots(target.Map.GetComponent<WalkabilityHandler>(), blockedPositions);
        }

        public static HashSet<IntVec3> FindSafeConstructionSpots(IWalkabilityHandler walkabilityHandler, HashSet<IntVec3> addedBlockers)
        {
            var possiblePositions = NeighborCounter.GetAllNeighbors(addedBlockers);
            var enclosedPositions = ClosedRegionCreatedByAddingImpassable(walkabilityHandler, addedBlockers);
            possiblePositions.RemoveWhere(pos =>
            {
                if (enclosedPositions.Contains(pos)) return true;
                if (!walkabilityHandler.Walkable(pos)) return true;
                return false;
            });
            return possiblePositions;
        }
    }
}
