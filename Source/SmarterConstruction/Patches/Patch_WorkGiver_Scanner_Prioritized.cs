using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace SmarterConstruction.Patches
{
    [HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
    public class PatchMakeFinishFramesPrioritized
    {
        static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // Tell Rimworld that WorkGiver_ConstructFinishFrames should look at priority
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

            // Replace thing finder code with a version optimized for this use case, but only for WorkGiver_ConstructFinishFrames
            var customCall = typeof(CustomGenClosest).GetMethod(nameof(CustomGenClosest.ClosestThing_Global_Reachable_Custom));
            var secondIndex = instructionList
                .Skip(prioritizedCallIndex)
                .FirstIndexOf(instruction => instruction.ToString().Contains("Verse.GenClosest::ClosestThing_Global_Reachable")) + prioritizedCallIndex;
            var originalCall = instructionList[secondIndex].WithLabels(generator.DefineLabel());
            var afterOriginalCall = instructionList[secondIndex + 1].WithLabels(generator.DefineLabel());
            instructionList.RemoveRange(secondIndex, 2);
            instructionList.InsertRange(secondIndex, new[]{
                instructionList[prioritizedCallIndex-2].Clone(),
                instructionList[prioritizedCallIndex-1].Clone(),
                new CodeInstruction(OpCodes.Isinst, typeof(WorkGiver_ConstructFinishFrames)),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Cgt_Un),
                new CodeInstruction(OpCodes.Brfalse, originalCall.labels[0]),
                new CodeInstruction(OpCodes.Call, customCall),
                new CodeInstruction(OpCodes.Br, afterOriginalCall.labels[0]),
                originalCall,
                afterOriginalCall
            });

            //var instructionsAround = instructionList.Skip(prioritizedCallIndex - 5).Take(secondIndex - prioritizedCallIndex + 20).ToList();
            return instructionList;
        }
    }

    public class CustomGenClosest
    {
        public static Thing ClosestThing_Global_Reachable_Custom(IntVec3 center, Map map, IEnumerable<Thing> searchSet, PathEndMode peMode, TraverseParms traverseParams, float maxDistance, Predicate<Thing> validator = null, Func<Thing, float> priorityGetter = null)
        {
            if (searchSet == null) return null;
            var bestPrio = float.MinValue;
            var maxPriorityDistanceSquared = SmarterConstruction.Settings.MaxDistanceForPriority * SmarterConstruction.Settings.MaxDistanceForPriority;
            var closestDistSquared = float.MaxValue;
            Thing bestThing = null;
            foreach (Thing t in searchSet)
            {
                var dist = (center - t.Position).LengthHorizontalSquared;
                var hasPriority = dist <= maxPriorityDistanceSquared
                    && priorityGetter != null
                    && t?.def?.entityDefToBuild?.passability == Traversability.Impassable;
                if ((hasPriority || dist < closestDistSquared)
                    && map.reachability.CanReach(center, t, peMode, traverseParams))
                {
                    var priority = priorityGetter == null ? 0f : priorityGetter(t);
                    if ((priority > bestPrio || (priority == bestPrio && dist < closestDistSquared))
                        && (validator == null || validator(t)))
                    {
                        closestDistSquared = dist;
                        bestPrio = priority;
                        bestThing = t;
                    }
                }
            }
            return bestThing;
        }
    }
}
