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
            var workDoneField = typeof(Frame).GetField(nameof(Frame.workDone));
            var instructionList = instructions.ToList();

            var completeConstructionIndex = instructionList.FindIndex(instruction =>
                instruction.opcode == OpCodes.Callvirt && instruction.Calls(completeConstructionCall));
            var insertionIndex = completeConstructionIndex - 2;
            var retInstruction = instructionList
                .Skip(insertionIndex)
                .First(instruction => instruction.opcode == OpCodes.Ret && instruction.labels.Count > 0);

            instructionList.InsertRange(insertionIndex, new[] {
                new CodeInstruction(OpCodes.Ldloc_1),                               // Set workDone = workToBuild to avoid graphic glitch
                new CodeInstruction(OpCodes.Ldloc_3),                               //
                new CodeInstruction(OpCodes.Stfld, workDoneField),                  //

                new CodeInstruction(OpCodes.Ldloc_1),                               // If EndJobIfEnclosing returns true, skip CompleteConstruction() and ReadyForNextToil()
                new CodeInstruction(OpCodes.Ldloc_0),                               // Calling both EndCurrentJob and ReadyForNextToil can result in cleaning up the job multiple times
                new CodeInstruction(OpCodes.Call, testEncloseCall),                 //
                new CodeInstruction(OpCodes.Brtrue, retInstruction.labels[0])       //
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
                DebugUtils.DebugLog($"Ending job for {pawn.Label} at {target.Position} because it would enclose something.");
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return true;
            }

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
                    DebugUtils.DebugLog($"No safe positions found for {pawn.Label} at {target.Position}, ending job.");
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

            DebugUtils.VerboseLog($"{pawn.Label} finished {target.Label} at {target.Position}");
            Patch_WorkGiver_Scanner_GetPriority.RemoveNeighborCachedData(target);
            return false;
        }
    }
}
