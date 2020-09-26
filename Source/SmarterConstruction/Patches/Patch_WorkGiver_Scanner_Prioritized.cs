using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace SmarterConstruction.Patches
{
    // Tell Rimworld that WorkGiver_ConstructFinishFrames should look at priority
    [HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
    public class PatchMakeFinishFramesPrioritized
    {
        static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionList = instructions.ToList();
            var prioritizedCallIndex = instructionList.FirstIndexOf(instruction => instruction.opcode == OpCodes.Callvirt && instruction.ToString().Contains("WorkGiver_Scanner::get_Prioritized"));

            var newInstructions = new[]
            {
                instructionList[prioritizedCallIndex-2].Clone(),
                instructionList[prioritizedCallIndex-1].Clone(),
                new CodeInstruction(OpCodes.Isinst, typeof(WorkGiver_ConstructFinishFrames)),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Cgt_Un),
                new CodeInstruction(OpCodes.Or)
            };

            instructionList.InsertRange(prioritizedCallIndex + 1, newInstructions);
            return instructionList;
        }
    }
}
