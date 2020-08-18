﻿using HarmonyLib;
using RimWorld;
using SmarterConstruction.Pathfinding;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SmarterConstruction.Patches
{
    // Fail during construction if the building would enclose something
    [HarmonyPatch(typeof(JobDriver_ConstructFinishFrame), "MakeNewToils")]
    public class Patch_JobDriver_MakeNewToils
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_ConstructFinishFrame __instance, Pawn ___pawn, Job ___job)
        {
            if (___job.def == JobDefOf.FinishFrame && !___job.playerForced)
            {
                var passability = ___job.targetA.Thing?.def?.entityDefToBuild?.passability;
                if (passability == Traversability.Impassable)
                {
                    foreach (var t in __result)
                    {
                        if (t.defaultCompleteMode == ToilCompleteMode.Delay)
                        {
                            t.AddFailCondition(() => ClosedRegionDetector.WouldEncloseThings(___job.targetA.Thing, ___pawn));
                        }
                        yield return t;
                    }
                }
            }
        }
    }
}
