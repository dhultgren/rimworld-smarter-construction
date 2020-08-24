using HarmonyLib;
using RimWorld;
using SmarterConstruction.Core;
using System;
using System.Linq;
using Verse;

namespace SmarterConstruction.Patches
{
    // Set priority based on how many blocking neighbors the thing has
    [HarmonyPatch(typeof(WorkGiver_Scanner), "GetPriority", new Type[] { typeof(Pawn), typeof(TargetInfo) })]
    public class Patch_WorkGiver_Scanner_GetPriority
    {
        private static readonly int MaxDistanceForPriority = 10;

        public static void Postfix(Pawn pawn, TargetInfo t, ref float __result, WorkGiver_Scanner __instance)
        {
            if (pawn?.Faction?.IsPlayer != true) return;
            if (t.Thing?.def?.entityDefToBuild?.passability != Traversability.Impassable) return;
            if (__result < 0) return;
            if (!SmarterConstruction.AddPriorityToWorkgivers.Contains(__instance?.GetType())) return;
            if (!pawn.Position.IsValid || !t.Cell.IsValid || pawn.Position.DistanceTo(t.Cell) > MaxDistanceForPriority) return;

            float modPriority = NeighborCounter.CountImpassableNeighbors(t.Thing);

            __result += modPriority;
        }
    }
}
