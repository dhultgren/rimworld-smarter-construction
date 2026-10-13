using Verse;

namespace SmarterConstruction.Core
{
    public interface IWalkabilityHandler
    {
        bool Walkable(IntVec3 loc);
    }

    public class WalkabilityHandler : MapComponent, IWalkabilityHandler
    {
        public WalkabilityHandler(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            map.events.BuildingSpawned += OnBuildingAdded;
        }

        private void OnBuildingAdded(Building building)
        {
            if (!building.def.IsFrame)
            {
                DebugUtils.VerboseLog($"Building finished: {building.Label} at {building.Position}. Recalculating pathGrid for that position.");
                bool haveNotified = false;
                // If this isn't done the path cost may take too long to update and cause trapped pawns
                map.pathing.Normal.pathGrid.RecalculatePerceivedPathCostAt(building.Position, ref haveNotified);
            }
        }

        public bool Walkable(IntVec3 loc)
        {
            var walkable = map.pathing.Normal.pathGrid.Walkable(loc);
            return walkable;
        }

        public static IWalkabilityHandler GetHandler(Map map)
        {
            return map.GetComponent<WalkabilityHandler>();
        }
    }
}
