using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;

namespace Xenomorphtype
{
    internal class JobDriver_AlterGenes : JobDriver
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
            yield return CompleteInjection();
        }

        private Toil AttemptInjection()
        {
            Toil toil = ToilMaker.MakeToil("AttemptInjection");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                Pawn actor = toil.GetActor();
                if(actor == null)
                {
                    return;
                }
                if (Target is Pawn pawnTarget)
                {
                    PawnUtility.ForceWait(pawnTarget, Mathf.FloorToInt(TicksFinish), actor);
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                Ticks += delta;
                Progress = (Ticks / TicksFinish);
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

        private Toil CompleteInjection()
        {
            Toil toil = ToilMaker.MakeToil("CompleteInjection");
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                Pawn actor = toil.GetActor();
                if (actor == null)
                {
                    return;
                }

                if (actor.Destroyed || actor.Map == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                Thing prey = Target;
                if (prey == null || prey.Destroyed || prey.Map == null || prey.Map != actor.Map)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                CompGeneManipulator manipulator = actor.GetComp<CompGeneManipulator>();
                if (manipulator == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                manipulator.AlterGenes(prey);
            };
            return toil;
        }
    }
}
