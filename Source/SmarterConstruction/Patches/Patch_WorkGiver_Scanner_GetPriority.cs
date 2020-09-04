using HarmonyLib;
using RimWorld;
using SmarterConstruction.Core;
using System;
using System.Collections.Generic;
using Verse;

namespace SmarterConstruction.Patches
{
    // Set priority based on how many blocking neighbors the thing has
    [HarmonyPatch(typeof(WorkGiver_Scanner), "GetPriority", new Type[] { typeof(Pawn), typeof(TargetInfo) })]
    public class Patch_WorkGiver_Scanner_GetPriority
    {
        private static readonly int MaxDistanceForPriority = 10;
        private static readonly int MaxCacheTime = 2000;

        private static readonly Dictionary<IntVec3, CachedPriority> cache = new Dictionary<IntVec3, CachedPriority>();

        [HarmonyPostfix]
        public static void PriorityPostfix(Pawn pawn, TargetInfo t, ref float __result, WorkGiver_Scanner __instance)
        {
            if (cache.TryGetValue(t.Cell, out CachedPriority data))
            {
                if (data.CachedAtTick + MaxCacheTime < Find.TickManager.TicksGame)
                {
                    cache.Remove(t.Cell);
                }
                else
                {
                    __result += data.PriorityModifier;
                    return;
                }
            }
            if (__result < 0) return;
            if (t.Thing?.def?.entityDefToBuild?.passability != Traversability.Impassable) return;
            if (pawn?.Faction?.IsPlayer != true) return;
            if (!pawn.Position.IsValid || !t.Cell.IsValid || pawn.Position.DistanceTo(t.Cell) > MaxDistanceForPriority) return;
            if (!(__instance is WorkGiver_ConstructFinishFrames)) return;

            int modPriority = NeighborCounter.CountImpassableNeighbors(t.Thing);
            cache[t.Cell] = new CachedPriority
            {
                CachedAtTick = Find.TickManager.TicksGame,
                PriorityModifier = modPriority
            };

            __result += modPriority;
        }

        public static void RemoveNeighborCachedData(Thing thing)
        {
            var points = NeighborCounter.GetAllNeighbors(thing);
            cache.RemoveAll(pair => points.Contains(pair.Key));
        }

        private class CachedPriority
        {
            public int PriorityModifier { get; set; }
            public int CachedAtTick { get; set; }
        }
    }
}
