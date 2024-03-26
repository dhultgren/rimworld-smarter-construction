using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmarterConstruction.Core
{
    public class NeighborCounter
    {
        public static int CountImpassableNeighbors(Thing thing)
        {
            var thingCells = GenAdj.CellsOccupiedBy(thing.Position, thing.Rotation, thing.def.Size).ToHashSet();
            var possiblePositions = GetAllNeighbors(thingCells);
            return possiblePositions.Count(pos => thing?.Map?.pathing?.Normal?.pathGrid?.Walkable(pos) != true);
        }

        public static HashSet<IntVec3> GetAllNeighbors(HashSet<IntVec3> blockers)
        {
            var ret = blockers
                .SelectMany(center => Enumerable.Range(-1, 3)
                    .SelectMany(y => Enumerable.Range(-1, 3)
                        .Select(x => new IntVec3(center.x + x, 0, center.z + y))))
                .ToHashSet();
            ret.RemoveWhere(pos => blockers.Contains(pos));
            return ret;
        }

        public static HashSet<IntVec3> GetAllNeighbors(Thing thing)
        {
            return GetAllNeighbors(GenAdj.CellsOccupiedBy(thing.Position, thing.Rotation, thing.def.Size).ToHashSet());
        }

        public static HashSet<IntVec3> GetCardinalNeighbors(HashSet<IntVec3> blockers)
        {
            var ret = blockers
                .SelectMany(pos => GetCardinalNeighbors(pos))
                .ToHashSet();
            ret.RemoveWhere(pos => blockers.Contains(pos));
            return ret;
        }

        public static IEnumerable<IntVec3> GetCardinalNeighbors(IntVec3 pos)
        {
            yield return new IntVec3(pos.x - 1, pos.y, pos.z);
            yield return new IntVec3(pos.x, pos.y, pos.z - 1);
            yield return new IntVec3(pos.x + 1, pos.y, pos.z);
            yield return new IntVec3(pos.x, pos.y, pos.z + 1);
        }
    }
}
