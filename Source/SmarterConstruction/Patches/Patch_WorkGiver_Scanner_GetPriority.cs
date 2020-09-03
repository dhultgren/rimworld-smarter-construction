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

        private static Dictionary<IntVec3, CachedPriority> cache = new Dictionary<IntVec3, CachedPriority>();

        public static void Postfix(Pawn pawn, TargetInfo t, ref float __result, WorkGiver_Scanner __instance)
        {
            if (cache.TryGetValue(t.Cell, out CachedPriority data))
            {
                if (data.CachedAtTick + MaxCacheTime < Find.TickManager.TicksGame)
                {
                    //DebugUtils.VerboseLog("Removing expired data " + t.Cell);
                    cache.Remove(t.Cell);
                }
                else
                {
                    //DebugUtils.VerboseLog("Using cached data " + t.Cell + " " + data.PriorityModifier);
                    __result += data.PriorityModifier;
                    return;
                }
            }
            if (__result < 0) return;
            if (t.Thing?.def?.entityDefToBuild?.passability != Traversability.Impassable) return;
            if (pawn?.Faction?.IsPlayer != true) return;
            if (!pawn.Position.IsValid || !t.Cell.IsValid || pawn.Position.DistanceTo(t.Cell) > MaxDistanceForPriority) return;
            if (!SmarterConstruction.Settings.AddPriorityToWorkgivers.Contains(__instance?.GetType())) return;

            int modPriority = NeighborCounter.CountImpassableNeighbors(t.Thing);
            cache[t.Cell] = new CachedPriority
            {
                CachedAtTick = Find.TickManager.TicksGame,
                PriorityModifier = modPriority
            };
            //DebugUtils.VerboseLog("Cached priority for " + t.Cell + " : " + modPriority);

            __result += modPriority;
        }

        public static void RemoveNeighborCachedData(Thing thing)
        {
            var points = NeighborCounter.GetAllNeighbors(thing);
            /*var removedPoints = cache.Where(pair => points.Contains(pair.Key)).ToList();
            DebugUtils.VerboseLog($"Finished {thing.Position} and invalidating cached points {string.Join(", ", removedPoints.Select(p => p.Key))}");*/
            cache.RemoveAll(pair => points.Contains(pair.Key));
        }

        private class CachedPriority
        {
            public int PriorityModifier { get; set; }
            public int CachedAtTick { get; set; }
        }
    }
}
