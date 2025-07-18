﻿using HarmonyLib;
using System.Reflection;
using Verse;

namespace SmarterConstruction
{
    [StaticConstructorOnStartup]
    public class SmarterConstruction
    {
        public static SmarterConstructionSettings Settings { get; private set; }

        static SmarterConstruction()
        {
            Settings = new SmarterConstructionSettings();
            Compatibility.InitCompatibility();

            var harmony = new Harmony("SmarterConstruction");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
