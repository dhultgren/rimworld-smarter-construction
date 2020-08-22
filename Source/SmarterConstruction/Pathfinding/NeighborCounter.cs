using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmarterConstruction.Pathfinding
{
    public class NeighborCounter
    {
        public static int CountImpassableNeighbors(Thing thing)
        {
            var thingCells = GenAdj.CellsOccupiedBy(thing.Position, thing.Rotation, thing.def.Size).ToHashSet();
            var possiblePositions = thingCells
                .SelectMany(center => Enumerable.Range(-1, 3)
                    .SelectMany(y => Enumerable.Range(-1, 3)
                        .Where(x => !(y == 0 && x == 0))
                        .Select(x => new IntVec3(center.x + x, 0, center.z + y))))
                .Where(pos => !thingCells.Contains(pos))
                .ToHashSet();
            return possiblePositions.Count(pos => thing?.Map?.pathGrid?.Walkable(pos) != true);
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
