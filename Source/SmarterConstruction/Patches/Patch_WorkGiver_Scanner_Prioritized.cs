using HarmonyLib;
using RimWorld;

namespace SmarterConstruction.Patches
{
    // Tell Rimworld that construction work givers should look at priority
    [HarmonyPatch(typeof(WorkGiver_Scanner))]
    [HarmonyPatch("Prioritized", MethodType.Getter)]
    public class Patch_WorkGiver_Scanner_Prioritized
    {
        public static bool Prefix(WorkGiver_Scanner __instance, ref bool __result)
        {
            if (!SmarterConstruction.Settings.AddPriorityToWorkgivers.Contains(__instance.GetType())) return true;

            __result = true;
            return false;
        }
    }
}
