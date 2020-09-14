using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SmarterConstruction.Patches
{
    // Tell Rimworld that WorkGiver_ConstructFinishFrames should look at priority
    [HarmonyPatch(typeof(WorkGiver_Scanner))]
    [HarmonyPatch("Prioritized", MethodType.Getter)]
    public class Patch_WorkGiver_Scanner_Prioritized
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var ret = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Isinst, typeof(WorkGiver_ConstructFinishFrames)),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Cgt_Un),
                new CodeInstruction(OpCodes.Ret)
            };
            return ret;
        }
    }
}
