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
            if (__result != null && t?.def?.entityDefToBuild?.passability == Traversability.Impassable && !forced)
            {
                var encloseData = ClosedRegionDetector.WouldEncloseThings(t, pawn, SmarterConstruction.Settings.GetJobEncloseThingCacheTicks);
                if (encloseData.EnclosesSelf)
                {
                    DebugUtils.VerboseLog($"Allowing self enclosing job on {t.Position} for pawn {pawn.Label} expecting a teleport.");
                }
                if (encloseData.EnclosesThings)
                {
                    __result = null;
                }
            }
        }
    }
}
