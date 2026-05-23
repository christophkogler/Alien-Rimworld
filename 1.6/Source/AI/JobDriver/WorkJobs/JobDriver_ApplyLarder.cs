using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Xenomorphtype
{
    public class JobDriver_ApplyLarder : JobDriver
    {

        private float TicksFinish = 350;
        private float Ticks = 0;
        private float Progress = 0;
        public Pawn Prey
        {
            get
            {
                return (Pawn)job.GetTarget(TargetIndex.A).Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public bool IsNoLongerValidTarget()
        {
            Pawn prey = Prey;
            return prey == null || prey.Destroyed || XMTUtility.HasEmbryo(prey);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            AddFailCondition(IsNoLongerValidTarget);
            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOn(() => Find.TickManager.TicksGame > startTick + 5000 && (float)(job.GetTarget(TargetIndex.A).Cell - pawn.Position).LengthHorizontalSquared > 4f);
            yield return toil;
            yield return AttemptInjection();
            yield return FinishInjection();
        }

        private Toil AttemptInjection()
        {
            Toil toil = ToilMaker.MakeToil("AttemptGrab");
            toil.atomicWithPrevious = true;
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
            toil.WithEffect(EffecterDefOf.Surgery, TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }

        private Toil FinishInjection()
        {
            Toil toil = ToilMaker.MakeToil("FinishInjection");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Pawn prey = job.GetTarget(TargetIndex.A).Thing as Pawn;

                if (actor == null || actor.Destroyed || actor.Map == null || prey == null || prey.Destroyed || prey.Map == null || prey.Map != actor.Map || XMTUtility.HasEmbryo(prey))
                {
                    actor?.jobs?.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                CompMatureMorph matureMorph = actor.GetMorphComp();
                if (matureMorph == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                matureMorph.TryLardering(prey);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
