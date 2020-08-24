using Verse;
using Verse.AI;

namespace SmarterConstruction.Core
{
    public interface IPathGrid
    {
        bool WalkableFast(IntVec3 loc);
    }

    public class PathGridWrapper : IPathGrid
    {
        private PathGrid _pathGrid;
        public PathGridWrapper(PathGrid pathGrid = null)
        {
            _pathGrid = pathGrid;
        }

        public bool WalkableFast(IntVec3 loc)
        {
            return _pathGrid.WalkableFast(loc);
        }
    }
}
