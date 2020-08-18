using HarmonyLib;
using RimWorld;
using SmarterConstruction.Pathfinding;
using Verse;
using Verse.AI;

namespace SmarterConstruction.Patches
{
    // Pretend job doesn't exist if the building would enclose something
    /*[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ResourceDeliverJobFor")]
    public class Patch_WorkGiver_ConstructDeliverResources
    {
        public static bool Prefix(Pawn pawn, IConstructible c, ref Job __result, WorkGiver_ConstructDeliverResources __instance)
        {
            return !(c is Blueprint_Install install
                && install.ThingToInstall?.def?.passability == Traversability.Impassable
                && ClosedRegionDetector.WouldEncloseThings(install, pawn));
        }
    }*/
}
