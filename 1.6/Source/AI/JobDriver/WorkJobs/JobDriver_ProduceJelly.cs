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
        private bool jellyConverted = false;
        protected float xpPerTick = 0.085f;
        public IntVec3 target
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Cell;
            }
        }
        private bool HasValidTargetCell()
        {
            if (pawn?.Map == null || job == null)
            {
                return false;
            }

            LocalTargetInfo targetInfo = job.GetTarget(TargetIndex.A);
            return targetInfo.IsValid && targetInfo.Cell.IsValid && targetInfo.Cell.InBounds(pawn.Map);
        }

        private CompJellyMaker GetValidJellyMaker()
        {
            if (!HasValidTargetCell())
            {
                return null;
            }

            return pawn.GetComp<CompJellyMaker>();
        }

        private float ClampedProgress()
        {
            if (Progress <= 0f)
            {
                return 0f;
            }
            if (Progress >= 1f)
            {
                return 1f;
            }
            return Progress;
        }

        private void TryConvertWorkedJelly()
        {
            if (jellyConverted || Ticks <= 0f)
            {
                return;
            }

            CompJellyMaker jellyMaker = GetValidJellyMaker();
            if (jellyMaker == null)
            {
                return;
            }

            float progress = ClampedProgress();
            if (progress <= 0f)
            {
                return;
            }

            jellyConverted = true;
            jellyMaker.ConvertToJelly(target, progress);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public bool IsNoLongerValidTarget()
        {
            return !HasValidTargetCell();
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(IsNoLongerValidTarget);
            AddFailCondition(() => pawn.Faction == null && !CompJellyMaker.IsProperLightLevelForFeralJelly(target, pawn.Map));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return BeginProducingJelly();
        }

        private Toil BeginProducingJelly()
        {
            CompJellyMaker jellyMaker = GetValidJellyMaker();
            if (jellyMaker != null)
            {
                TicksFinish = jellyMaker.JellyFromCell(target)*jellyMaker.WorkPerJelly;
            }
            int pendingGainTicks = 0;
            Action flushPendingGains = delegate
            {
                if (pendingGainTicks <= 0)
                {
                    return;
                }

                if (pawn?.skills != null)
                {
                    pawn.skills.Learn(SkillDefOf.Cooking, xpPerTick * pendingGainTicks);
                }
                if (pawn?.needs?.joy != null)
                {
                    pawn.needs.joy.GainJoy(0.001f * pendingGainTicks, InternalDefOf.NestTending);
                }
                pendingGainTicks = 0;
            };
            Toil toil = ToilMaker.MakeToil("AttemptJellyMaking");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                if (GetValidJellyMaker() == null || TicksFinish <= 0)
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                if (GetValidJellyMaker() == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Ticks += pawn.GetStatValue(ExternalDefOf.CookSpeed) * delta;
                pendingGainTicks += delta;
                if (pendingGainTicks >= GainBatchTicks)
                {
                    flushPendingGains();
                }
                Progress = (Ticks / TicksFinish);
                if (Ticks >= TicksFinish)
                {
                    Progress = 1f;
                    TryConvertWorkedJelly();
                    ReadyForNextToil();
                }

            };
            toil.AddFinishAction(delegate
            {
                flushPendingGains();
                TryConvertWorkedJelly();
            });
            toil.WithProgressBar(TargetIndex.A, () => Progress);
            toil.WithEffect(InternalDefOf.ResinBuild, TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }
    }
}
