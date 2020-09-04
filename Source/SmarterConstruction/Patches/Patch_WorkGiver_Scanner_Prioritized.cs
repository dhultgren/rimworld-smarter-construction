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
        public static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var retTrue = generator.DefineLabel();
            var ret = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Isinst, typeof(WorkGiver_ConstructFinishFrames)),
                new CodeInstruction(OpCodes.Brtrue, retTrue),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(retTrue),
                new CodeInstruction(OpCodes.Ret)
            };
            return ret;
        }
    }
}
