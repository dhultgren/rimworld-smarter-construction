using HarmonyLib;
using RimWorld;
using SmarterConstruction.Core;
using Verse;
using Verse.AI;

namespace SmarterConstruction.Patches
{
    // Pretend job doesn't exist if the building would enclose something
    [HarmonyPatch(typeof(WorkGiver_ConstructFinishFrames), "JobOnThing")]
    public class WorkGiver_ConstructFinishFrames_JobOnThing
    {
        public static void Postfix(Pawn pawn, Thing t, ref Job __result, bool forced, WorkGiver_ConstructFinishFrames __instance)
        {
            if (t?.def?.entityDefToBuild?.passability == Traversability.Impassable
                && ClosedRegionDetector.WouldEncloseThings(t, pawn).EnclosesThings
                && !forced)
            {
                __result = null;
            }
        }
    }
}
