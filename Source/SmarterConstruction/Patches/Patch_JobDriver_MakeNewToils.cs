using HarmonyLib;
using RimWorld;
using SmarterConstruction.Pathfinding;
using System.Collections.Generic;
using System.Linq;
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


    // Fail during construction if the building would enclose something
    /*[HarmonyPatch(typeof(JobDriver_HaulToContainer), "MakeNewToils")]
    public class Patch_JobDriver_MakeNewToils2
    {
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_HaulToContainer __instance, Pawn ___pawn, Job ___job)
        {
            var thing = __instance.ThingToCarry.GetInnerIfMinified();
            var passability = thing?.def?.passability;
            if (thing != null && passability == Traversability.Impassable && ___job.targetB.Thing is Blueprint)
            {
                //__instance.AddFailCondition(() => ClosedRegionDetector.WouldEncloseThings(___job.targetA.Thing, ___pawn));
                var toisl = new List<Toil>(__result);
                var lol = __result
                    .Where(t => t.defaultCompleteMode == ToilCompleteMode.Delay)
                    .ToList();
                foreach(var t in toisl)
                {
                    if (t.defaultCompleteMode == ToilCompleteMode.Delay)
                    {
                        t.AddFailCondition(() => ClosedRegionDetector.WouldEncloseThings(___job.targetB.Thing, ___pawn));
                    }
                    yield return t;
                }
            }
        }
    }*/
}
