using HarmonyLib;
using RimWorld;
using SmarterConstruction.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SmarterConstruction.Patches
{
    // Fail during construction if the building would enclose something
    public class Patch_JobDriver_MakeNewToils
    {
        public static void Patch(Harmony harmony)
        {
            var nestedTypes = typeof(JobDriver_ConstructFinishFrame).GetNestedTypes(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            var targetType = nestedTypes.FirstOrDefault(t => t.Name == "<>c__DisplayClass4_0");
            var targetMethod = targetType.GetRuntimeMethods().FirstOrDefault(m => m.Name == "<MakeNewToils>b__1");
            harmony.Patch(targetMethod, transpiler: new HarmonyMethod(typeof(Patch_JobDriver_MakeNewToils), nameof(Transpiler)));
        }

        static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var completeConstructionCall = typeof(Frame).GetMethod("CompleteConstruction");
            var testEncloseCall = typeof(TranspilerHelper).GetMethod(nameof(TranspilerHelper.EndJobIfEnclosing));
            var workDoneField = typeof(Frame).GetField(nameof(Frame.workDone));
            var instructionList = instructions.ToList();

            var insertionIndex = instructionList.FirstIndexOf(instruction => instruction.opcode == OpCodes.Callvirt && instruction.Calls(completeConstructionCall)) - 2;
            var jumpInstruction = instructionList
                .Take(insertionIndex)
                .Reverse()
                .FirstOrDefault(instruction => instruction.opcode == OpCodes.Blt_Un_S);
            var label = generator.DefineLabel();
            instructionList[insertionIndex + 3].labels.Add(label);

            instructionList.InsertRange(insertionIndex, new[] {
                new CodeInstruction(OpCodes.Ldloc_1),                         // Set workDone = workToBuild to avoid graphic glitch
                new CodeInstruction(OpCodes.Ldloc_3),                         //
                new CodeInstruction(OpCodes.Stfld, workDoneField),            //

                new CodeInstruction(OpCodes.Ldloc_1),                         // Skip CompleteConstruction if the building would enclose something
                new CodeInstruction(OpCodes.Ldloc_0),                         //
                new CodeInstruction(OpCodes.Call, testEncloseCall),           //
                new CodeInstruction(OpCodes.Brtrue, label),                   //
            });
            return instructionList;
        }
    }

    public class TranspilerHelper
    {
        public static bool EndJobIfEnclosing(Frame target, Pawn pawn)
        {
            if (target?.def?.entityDefToBuild?.passability != Traversability.Impassable) return false;
            var wouldEnclose = ClosedRegionDetector.WouldEncloseThings(target, pawn);
            if (wouldEnclose)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                pawn.GetLord()?.Notify_ConstructionFailed(pawn, target, null);
                return true;
            }

            // TODO: move pawn to a safe location if it's standing on top of the current target to avoid random movement

            return false;
        }
    }
}
