using HarmonyLib;
using RimWorld;
using SmarterConstruction.Core;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace SmarterConstruction.Patches
{
    // Hack to stop pawns from getting stuck in the pathfinding when something changes on the way
    // TODO: find the actual cause
    [HarmonyPatch(typeof(JobDriver_ConstructFinishFrame), "MakeNewToils")]
    public class Patch_PawnsGettingStuck
    {
        private static readonly int TicksBetweenCacheChecks = 50;

        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, Pawn ___pawn)
        {
            foreach (var t in __result)
            {
                if (t.defaultCompleteMode == ToilCompleteMode.PatherArrival)
                {
                    t.AddFailCondition(() =>
                    {
                        if (Find.TickManager.TicksGame % TicksBetweenCacheChecks == 0)
                        {
                            var pawn = t?.actor;
                            var map = pawn?.Map;
                            if (PawnPositionCache.GetCache(map)?.IsPawnStuck(pawn) == true)
                            {
                                DebugUtils.DebugLog("Failing goto toil because it has taken too long, pawn " + ___pawn.Label + ". If this was wrong, please report it!");
                                return true;
                            }
                        }
                        return false;
                    });
                }
                yield return t;
            }
        }
    }
}
