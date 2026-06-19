
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using static Xenomorphtype.CompPawnInfo;

namespace Xenomorphtype
{
    internal class JobDriver_MutateTarget : JobDriver
    {

        private float TicksFinish = 350;
        private float Ticks = 0;
        private float Progress = 0;
        public Thing Target
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }
        public bool IsNoLongerValidTarget()
        {
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(IsNoLongerValidTarget);
            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOn(() => Find.TickManager.TicksGame > startTick + 5000 && (float)(job.GetTarget(TargetIndex.A).Cell - pawn.Position).LengthHorizontalSquared > 4f);
            yield return toil;
            yield return AttemptInjection();
            yield return FinishInjection();
        }

        private Toil AttemptInjection()
        {
            Toil toil = ToilMaker.MakeToil("AttemptInjection");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                Pawn actor = toil.GetActor();
                if (TryGetMutationTarget(actor, out Pawn target, out CompPawnInfo _))
                {
                    PawnUtility.ForceWait(target, Mathf.FloorToInt(TicksFinish), actor);
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                Ticks += delta;
                Progress = Mathf.Min(Ticks / TicksFinish, 1f);
                if (Ticks >= TicksFinish)
                {
                    ReadyForNextToil();
                }

            };
            toil.WithProgressBar(TargetIndex.A, () => Progress);
            toil.WithEffect(InternalDefOf.ResinBuild, TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }

        private Toil FinishInjection()
        {
            Toil toil = ToilMaker.MakeToil("FinishInjection");
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                Pawn actor = toil.GetActor();
                if (!TryGetMutationTarget(actor, out Pawn prey, out CompPawnInfo info))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                switch (info.StrongestPheromone)
                {
                    case PheromoneType.Lover:
                        BioUtility.TryMutatingPawn(ref prey, XenoGeneDefOf.XMT_LovinMutationSet, 1);
                        break;
                    case PheromoneType.Friend:
                        BioUtility.TryMutatingPawn(ref prey, XenoGeneDefOf.XMT_AscendanceMutationSet, 1);
                        break;
                    case PheromoneType.Threat:
                        BioUtility.TryMutatingPawn(ref prey, XenoGeneDefOf.XMT_HostMeatMutationSet, 1);
                        break;
                    default:
                        BioUtility.TryMutatingPawn(ref prey, null, 1);
                        break;
                }
            };
            return toil;
        }

        private bool TryGetMutationTarget(Pawn actor, out Pawn target, out CompPawnInfo info)
        {
            target = null;
            info = null;

            if (actor == null || actor.Destroyed || actor.Map == null || actor.CurJob == null)
            {
                return false;
            }

            target = actor.CurJob.GetTarget(TargetIndex.A).Thing as Pawn;
            if (target == null || target.Destroyed || target.Map == null || target.Map != actor.Map || target.health == null)
            {
                return false;
            }

            info = target.Info();
            return info != null;
        }
    }
}
