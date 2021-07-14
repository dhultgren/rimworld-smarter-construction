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
        private static readonly int MaxCacheTime = 3000;

        private static readonly Dictionary<IntVec3, CachedPriority> cache = new Dictionary<IntVec3, CachedPriority>();
        private static readonly Random random = new Random();

        [HarmonyPostfix]
        public static void PriorityPostfix(Pawn pawn, TargetInfo t, ref float __result, WorkGiver_Scanner __instance)
        {
            if (__result < 0) return;
            if (!(__instance is WorkGiver_ConstructFinishFrames)) return;
            if (t.Thing?.def?.entityDefToBuild?.passability != Traversability.Impassable) return;
            if (!pawn?.Position.IsValid == true || !t.Cell.IsValid || pawn.Position.DistanceTo(t.Cell) > SmarterConstruction.Settings.MaxDistanceForPriority) return;
            if (pawn.Map != null && !t.Cell.Walkable(pawn.Map)) return; // Replacing existing structure

            if (cache.TryGetValue(t.Cell, out CachedPriority data))
            {
                if (data.ExpiresAtTick < Find.TickManager.TicksGame)
                {
                    //DebugUtils.DebugLog("Expiring cached priority for " + t.Cell);
                    cache.Remove(t.Cell);
                }
                else
                {
                    __result += data.PriorityModifier;
                    return;
                }
            }

            int modPriority = NeighborCounter.CountImpassableNeighbors(t.Thing);
            cache[t.Cell] = new CachedPriority
            {
                ExpiresAtTick = Find.TickManager.TicksGame + MaxCacheTime + random.Next(-MaxCacheTime/10, MaxCacheTime/10),
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
            public int ExpiresAtTick { get; set; }
        }
    }
}
