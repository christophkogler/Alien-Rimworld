using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace Xenomorphtype
{
    public class JobDriver_ProduceJelly : JobDriver_ClimbToPosition
    {
        private float Ticks = 0;
        private float Progress = 0;
        private float TicksFinish = 300;
        private const int GainBatchTicks = 10;
        protected float xpPerTick = 0.085f;
        public IntVec3 target
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Cell;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public bool IsNoLongerValidTarget()
        {
            return false;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(IsNoLongerValidTarget);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return BeginProducingJelly();
        }

        private Toil BeginProducingJelly()
        {
            CompJellyMaker jellyMaker = pawn.GetComp<CompJellyMaker>();
            if (jellyMaker != null)
            {
                TicksFinish = jellyMaker.JellyFromCell(target)*jellyMaker.WorkPerJelly;
            }
            float cookSpeed = pawn.GetStatValue(ExternalDefOf.CookSpeed);
            int pendingGainTicks = 0;
            Action flushPendingGains = delegate
            {
                if (pendingGainTicks <= 0)
                {
                    return;
                }

                if (pawn.skills != null)
                {
                    pawn.skills.Learn(SkillDefOf.Cooking, xpPerTick * pendingGainTicks);
                }
                if (pawn.needs != null && pawn.needs.joy != null)
                {
                    pawn.needs.joy.GainJoy(0.001f * pendingGainTicks, InternalDefOf.NestTending);
                }
                pendingGainTicks = 0;
            };
            Toil toil = ToilMaker.MakeToil("AttemptJellyMaking");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                if (TicksFinish <= 0)
                {
                    Progress = 1;
                    ReadyForNextToil();
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                if (TicksFinish <= 0)
                {
                    Progress = 1;
                    return;
                }

                Ticks += cookSpeed * delta;
                pendingGainTicks += delta;
                if (pendingGainTicks >= GainBatchTicks)
                {
                    flushPendingGains();
                }

                Progress = (Ticks / TicksFinish);
                if (Ticks >= TicksFinish)
                {
                    ReadyForNextToil();
                }

            };
            toil.AddFinishAction(delegate
            {
                flushPendingGains();
                CompJellyMaker jellyMaker = pawn.GetComp<CompJellyMaker>();
                if (jellyMaker != null)
                {
                    jellyMaker.ConvertToJelly(target, Progress);
                    
                }
            });
            toil.WithProgressBar(TargetIndex.A, () => Progress);
            toil.WithEffect(InternalDefOf.ResinBuild, TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }
    }
}
