using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace SmarterConstruction.Patches
{
    //Set priority based on how many blocking neighbors the thing has
    [HarmonyPatch(typeof(WorkGiver_Scanner), "GetPriority", new Type[] { typeof(Pawn), typeof(TargetInfo) })]
    public class Patch_WorkGiver_Scanner_GetPriority
    {
        public static void Postfix(Pawn pawn, TargetInfo t, ref float __result, WorkGiver_Scanner __instance)
        {
            if (pawn == null || pawn.Faction?.IsPlayer != true) return;
            if (t.Thing?.def?.entityDefToBuild?.passability != Traversability.Impassable) return;
            if (__result < 0) return;
            if (!SmarterConstruction.PatchWorkGiverTypes.Contains(__instance.GetType())) return;

            float modPriority = CountImpassableNeighbors(t.Cell, pawn.Map ?? t.Map);

            __result += modPriority;
        }

        private static int CountImpassableNeighbors(IntVec3 center, Verse.Map map)
        {
            var possiblePositions = Enumerable.Range(-1, 3)
                .SelectMany(y => Enumerable.Range(-1, 3)
                    .Where(x => !(y == 0 && x == 0))
                    .Select(x => new IntVec3(center.x + x, 0, center.z + y)))
                .ToList();
            return possiblePositions.Count(pos => !map.pathGrid.Walkable(pos));
        }
    }
}
