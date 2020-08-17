using HarmonyLib;
using RimWorld;
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
            new Harmony("SmarterConstruction").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
