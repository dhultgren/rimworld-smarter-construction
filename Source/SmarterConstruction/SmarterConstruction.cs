using HarmonyLib;
using RimWorld;
using SmarterConstruction.Patches;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SmarterConstruction
{
    [StaticConstructorOnStartup]
    public class SmarterConstruction
    {
        public static readonly HashSet<Type> PatchWorkGiverTypes = new HashSet<Type>
        {
            typeof(WorkGiver_ConstructDeliverResources),
            typeof(WorkGiver_ConstructDeliverResourcesToBlueprints),
            typeof(WorkGiver_ConstructDeliverResourcesToFrames),
            typeof(WorkGiver_ConstructFinishFrames)
        };

        static SmarterConstruction()
        {
            var harmony = new Harmony("SmarterConstruction");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Patch_JobDriver_MakeNewToils.Patch(harmony);
        }
    }
}
