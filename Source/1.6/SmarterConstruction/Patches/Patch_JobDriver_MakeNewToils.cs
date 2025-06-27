using HarmonyLib;
using RimWorld;
using SmarterConstruction.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace SmarterConstruction.Patches
{
    // Fail during construction if the building would enclose something
    [HarmonyPatch]
    public class Patch_JobDriver_MakeNewToils
    {
        static MethodBase TargetMethod()
        {
            var targetType = typeof(JobDriver_ConstructFinishFrame)
                .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .FirstOrDefault(t => t.Name.Contains("DisplayClass8_0"));
            var targetMethod = targetType.GetMethod("<MakeNewToils>b__1", BindingFlags.NonPublic | BindingFlags.Instance);

            return targetMethod;
        }

        static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var completeConstructionCall = typeof(Frame).GetMethod("CompleteConstruction");
            var testEncloseCall = typeof(TranspilerHelper).GetMethod(nameof(TranspilerHelper.EndJobIfEnclosing));
            var readyForNextToilCall = typeof(JobDriver).GetMethod(nameof(JobDriver.ReadyForNextToil));
            var workDoneField = typeof(Frame).GetField(nameof(Frame.workDone));
            var instructionList = instructions.ToList();

            var completeConstructionIndex = instructionList.FindIndex(instruction =>
                instruction.opcode == OpCodes.Callvirt && instruction.Calls(completeConstructionCall));
            var insertionIndex = completeConstructionIndex - 2;
            var readyForNextToilInstruction = instructionList
                .Skip(insertionIndex)
                .First(instruction =>
                    (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                    instruction.operand is MethodInfo mi &&
                    mi.Name == "ReadyForNextToil");
            var readyForNextToilLabel = generator.DefineLabel();
            readyForNextToilInstruction.labels.Add(readyForNextToilLabel);

            instructionList.InsertRange(insertionIndex, new[] {
                new CodeInstruction(OpCodes.Ldloc_1),                               // Set workDone = workToBuild to avoid graphic glitch
                new CodeInstruction(OpCodes.Ldloc_3),                               //
                new CodeInstruction(OpCodes.Stfld, workDoneField),                  //

                new CodeInstruction(OpCodes.Ldloc_1),                               // If EndJobIfEnclosing returns true, skip to ReadyForNextToil
                new CodeInstruction(OpCodes.Ldloc_0),                               //
                new CodeInstruction(OpCodes.Call, testEncloseCall),                 //
                new CodeInstruction(OpCodes.Brtrue, readyForNextToilLabel)          //
            });

            return instructionList;
        }
    }

    public class TranspilerHelper
    {
        public static bool EndJobIfEnclosing(Frame target, Pawn pawn)
        {
            if (target?.def?.entityDefToBuild?.passability != Traversability.Impassable) return false;
            if (pawn?.CurJob?.playerForced == true) return false;

            var wouldEnclose = ClosedRegionDetector.WouldEncloseThings(target, pawn, SmarterConstruction.Settings.FinishCacheTicks);
            if (wouldEnclose.EnclosesThings)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return true;
            }
            DebugUtils.VerboseLog(pawn.Label + " finished " + target.Label + " on coordinates " + target.Position);

            var pawnsAtLocation = target.Position.GetThingList(target.Map)
                .Where(t => t is Pawn && t.Faction != null && t.Faction.IsPlayer)
                .ToList();
            if (wouldEnclose.EnclosesSelf) pawnsAtLocation.Add(pawn);
            if (pawnsAtLocation.Count > 0 && wouldEnclose.EnclosesRegion)
            {
                // move pawn to a safe location to avoid random movement
                var safePositions = ClosedRegionDetector.FindSafeConstructionSpots(new PathGridWrapper(target.Map.pathing.Normal.pathGrid), target);
                if (safePositions.Count == 0)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return true;
                }
                else
                {
                    var safePosition = safePositions.First();
                    pawnsAtLocation.ForEach(p =>
                    {
                        DebugUtils.DebugLog($"Teleported pawn {pawn.Label} from {pawn.Position} to {safePosition}");
                        p.Position = safePosition;
                    });
                }
            }

            Patch_WorkGiver_Scanner_GetPriority.RemoveNeighborCachedData(target);
            return false;
        }
    }
}
