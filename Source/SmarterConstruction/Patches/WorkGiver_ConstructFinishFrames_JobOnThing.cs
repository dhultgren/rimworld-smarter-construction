﻿using HarmonyLib;
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
        private static readonly int MaxJobsPerTick = 10;
        private static int currentTick = 0;
        private static int thingsUpdatedThisTick = 0;

        public static void Postfix(Pawn pawn, Thing t, ref Job __result, bool forced, WorkGiver_ConstructFinishFrames __instance)
        {
            if (__result != null && t?.def?.entityDefToBuild?.passability == Traversability.Impassable && !forced)
            {
                if (currentTick != Find.TickManager.TicksGame)
                {
                    currentTick = Find.TickManager.TicksGame;
                    thingsUpdatedThisTick = 0;
                }

                // Minimize stuttering by only updating at most MaxJobsPerTick cached data points every tick
                // Will still run the detection code if there isn't any cached data available, otherwise it could cause a job loop
                var maxCacheLength = thingsUpdatedThisTick < MaxJobsPerTick ? SmarterConstruction.Settings.GetJobEncloseThingCacheTicks : 100000;
                var encloseData = ClosedRegionDetector.WouldEncloseThings(t, pawn, maxCacheLength);

                if (encloseData.EnclosesSelf)
                {
                    DebugUtils.VerboseLog($"Allowing self enclosing job on {t.Position} for pawn {pawn.Label} expecting a teleport.");
                }
                if (encloseData.EnclosesThings)
                {
                    __result = null;
                }
                thingsUpdatedThisTick++;
            }
        }
    }
}
