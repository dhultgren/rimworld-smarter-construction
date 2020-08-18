using HarmonyLib;
using RimWorld;
using SmarterConstruction.Pathfinding;
using Verse;
using Verse.AI;

namespace SmarterConstruction.Patches
{
    // Pretend job doesn't exist if the building would enclose something
    [HarmonyPatch(typeof(WorkGiver_ConstructFinishFrames), "JobOnThing")]
    public class WorkGiver_ConstructFinishFrames_JobOnThing
    {
        public static bool Prefix(Pawn pawn, Thing t, ref Job __result, bool forced, WorkGiver_ConstructFinishFrames __instance)
        {
            var passability = t?.def?.entityDefToBuild?.passability;
            if (passability == Traversability.Impassable && !forced)
            {
                if (ClosedRegionDetector.WouldEncloseThings(t, pawn))
                {
                    __result = null;
                    return false;
                }
            }

            return true;
        }
    }
}
