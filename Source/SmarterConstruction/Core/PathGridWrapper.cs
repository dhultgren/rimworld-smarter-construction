﻿using Verse;
using Verse.AI;

namespace SmarterConstruction.Core
{
    public interface IPathGrid
    {
        bool Walkable(IntVec3 loc);
    }

    public class PathGridWrapper : IPathGrid
    {
        private PathGrid _pathGrid;
        public PathGridWrapper(PathGrid pathGrid = null)
        {
            _pathGrid = pathGrid;
        }

        public bool Walkable(IntVec3 loc)
        {
            return SmarterConstruction.Settings.ChangeMapEdgesCompatibility
                ? _pathGrid.Walkable(loc)
                : _pathGrid.WalkableFast(loc);
        }
    }
}
