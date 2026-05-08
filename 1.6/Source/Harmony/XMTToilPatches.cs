using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using Verse;
using Verse.AI;
using static AlienRace.ExtendedGraphics.ConditionMood;

namespace Xenomorphtype
{
    internal class XMTToilPatches
    {
        private static Toil ReserveFood(Pawn pawn, Job job, JobDriver_Ingest source)
        {
            Toil toil = ToilMaker.MakeToil("ReserveFood");
            toil.initAction = delegate
            {
                if (pawn.Faction != null)
                {
                    Thing thing = job.GetTarget(TargetIndex.A).Thing;
                    if (pawn.carryTracker.CarriedThing != thing)
                    {
                        int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(thing, pawn, job.count);
                        if (maxAmountToPickup != 0)
                        {
                            if (!pawn.Reserve(thing, job, 10, maxAmountToPickup))
                            {
                                Log.Error("Pawn food reservation for " + pawn?.ToString() + " on job " + source?.ToString() + " failed, because it could not register food from " + thing?.ToString() + " - amount: " + maxAmountToPickup);
                                pawn.jobs.EndCurrentJob(JobCondition.Errored);
                            }

                            job.count = maxAmountToPickup;
                        }
                    }
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.atomicWithPrevious = true;
            return toil;
        }

        [HarmonyPatch(typeof(JobDriver_Ingest), "PrepareToIngestToils")]
        public static class Patch_JobDriver_Ingest_PrepareToIngestToils
        {

            [HarmonyPrefix]
            public static bool Prefix(JobDriver_Ingest __instance, ref IEnumerable<Toil> __result, bool ___usingNutrientPasteDispenser, bool ___eatingFromInventory)
            {
                if(___usingNutrientPasteDispenser)
                {
                    return true;
                }

                Pawn actor = __instance.pawn;

                if(actor == null)
                {
                    return true;
                }

                if (!XMTUtility.IsXenomorph(actor) )
                {
                    return true;
                }
                List<Toil> toils = new List<Toil>();
                if (___eatingFromInventory)
                {
                    toils.Add(Toils_Misc.TakeItemFromInventoryToCarrier(actor, TargetIndex.A));
                }
                else
                {
                    toils.Add(ReserveFood(actor, __instance.job, __instance));
                    toils.Add(Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A));
                }

                toils.Add(Toils_Ingest.FindAdjacentEatSurface(TargetIndex.B, TargetIndex.A));

                __result = toils;
                return false;
            }
        }

        [HarmonyPatch(typeof(JobDriver_Deconstruct), "FinishedRemoving")]
        public static class Patch_JobDriver_Deconstruct_FinishedRemoving
        {

            [HarmonyPrefix]
            public static void Prefix(JobDriver_Deconstruct __instance)
            {
                Pawn actor = __instance.pawn;
                
                if (XMTUtility.IsXenomorph(actor))
                {
                    if (__instance.job.GetTarget(TargetIndex.A).Thing is Building building)
                    {
                        if (!XMTUtility.IsHiveBuilding(building.def))
                        {
                           
                            int progress = Mathf.CeilToInt(building.HitPoints / 100);
                            ResearchUtility.ProgressMimicTech(progress, actor);
                            
                        }
                    }
                }
            }
        }
        [HarmonyPatch]
        public static class Patch_JobDriver_ConstructFinishFrame_BuildTick
        {
            private static FieldInfo buildField;
            private static FieldInfo jobDriverField;

            private static bool IsBuildingAttachment(Frame frame)
            {
                ThingDef thingDef = GenConstruct.BuiltDefOf(frame.def) as ThingDef;
                if (thingDef?.building != null)
                {
                    return thingDef.building.isAttachment;
                }

                return false;
            }

            public static MethodBase TargetMethod()
            {
                MethodInfo makeNewToils = AccessTools.Method(typeof(JobDriver_ConstructFinishFrame), "MakeNewToils");
                MethodBase moveNext = AccessTools.EnumeratorMoveNext(makeNewToils);
                FieldInfo tickIntervalAction = AccessTools.Field(typeof(Toil), "tickIntervalAction");
                List<KeyValuePair<OpCode, object>> body = PatchProcessor.ReadMethodBody(moveNext).ToList();

                int fieldIndex = body.FindIndex(code =>
                    (code.Key == OpCodes.Stfld || code.Key == OpCodes.Stsfld) &&
                    Equals(code.Value, tickIntervalAction));
                if (fieldIndex < 0)
                {
                    throw new InvalidOperationException("Could not find JobDriver_ConstructFinishFrame build tick delegate assignment.");
                }

                int methodIndex = body.FindLastIndex(fieldIndex, code => code.Key == OpCodes.Ldftn);
                if (methodIndex < 0 || !(body[methodIndex].Value is MethodBase method))
                {
                    throw new InvalidOperationException("Could not find JobDriver_ConstructFinishFrame build tick delegate method.");
                }

                buildField = AccessTools.Field(method.DeclaringType, "build");
                jobDriverField = AccessTools.Field(method.DeclaringType, "<>4__this");
                return method;
            }

            [HarmonyPrefix]
            public static bool Prefix(object __instance, int delta)
            {
                Toil build = (Toil)buildField.GetValue(__instance);
                Pawn actor = build.actor;
                JobDriver_ConstructFinishFrame jobDriver = (JobDriver_ConstructFinishFrame)jobDriverField.GetValue(__instance);
                Frame frame = (Frame)jobDriver.job.GetTarget(TargetIndex.A).Thing;

                if (!XMTUtility.IsXenomorph(actor) ||
                    (!XMTUtility.IsHiveBuilding(frame.BuildDef) && !XMTIdeologyConstructionUtility.IsRegisteredResinBuild(frame)))
                {
                    return true;
                }

                if (actor.skills != null)
                {
                    actor.skills.Learn(SkillDefOf.Construction, 0.25f * delta);
                    ResearchUtility.ProgressResinTech(delta, actor);
                }

                for (int i = 0; i < delta; i++)
                {
                    if (!BioUtility.PerformBioconstructionCost(actor))
                    {
                        jobDriver.FailOnMentalState(TargetIndex.A);
                        return false;
                    }
                }

                if (IsBuildingAttachment(frame))
                {
                    actor.rotationTracker.FaceTarget(GenConstruct.GetWallAttachedTo(frame));
                }
                else
                {
                    actor.rotationTracker.FaceTarget(frame);
                }

                float workDone = actor.GetStatValue(StatDefOf.ConstructionSpeed) * 1.7f * delta;
                if (frame.Stuff != null)
                {
                    workDone *= frame.Stuff.GetStatValueAbstract(StatDefOf.ConstructionSpeedFactor);
                }

                float workToBuild = frame.WorkToBuild;
                if (actor.Faction == Faction.OfPlayer)
                {
                    float statValue = actor.GetStatValue(StatDefOf.ConstructSuccessChance);
                    if (!TutorSystem.TutorialMode && Rand.Value < 1f - Mathf.Pow(statValue, workDone / workToBuild))
                    {
                        frame.FailConstruction(actor);
                        jobDriver.ReadyForNextToil();
                        return false;
                    }
                }

                if (frame.def.entityDefToBuild is TerrainDef)
                {
                    actor.Map.snowGrid.SetDepth(frame.Position, 0f);
                    actor.Map.sandGrid?.SetDepth(frame.Position, 0f);
                }

                frame.workDone += workDone;
                if (frame.workDone >= workToBuild)
                {
                    frame.CompleteConstruction(actor);
                    jobDriver.ReadyForNextToil();
                }

                return false;
            }
        }
    }
}
