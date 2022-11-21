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
            var retInstruction = instructionList
                .Skip(insertionIndex)
                .First(instruction => instruction.opcode == OpCodes.Ret && instruction.labels.Count > 0);

            instructionList.InsertRange(insertionIndex, new[] {
                new CodeInstruction(OpCodes.Ldloc_1),                         // Set workDone = workToBuild to avoid graphic glitch
                new CodeInstruction(OpCodes.Ldloc_3),                         //
                new CodeInstruction(OpCodes.Stfld, workDoneField),            //

                new CodeInstruction(OpCodes.Ldloc_1),                         // Skip CompleteConstruction if the building would enclose something
                new CodeInstruction(OpCodes.Ldloc_0),                         //
                new CodeInstruction(OpCodes.Call, testEncloseCall),           //
                new CodeInstruction(OpCodes.Brtrue, retInstruction.labels[0]) //
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
