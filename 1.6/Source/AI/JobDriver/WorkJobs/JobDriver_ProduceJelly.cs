using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using Verse;

namespace Xenomorphtype
{
    public class JobDriver_ProduceJelly : JobDriver_ClimbToPosition
    {
        private float Ticks = 0f;
        private float Progress = 0f;
        private float TicksFinish = 300f;
        private bool converted = false;
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

        private void TryConvertWorkedJelly()
        {
            if (converted || Ticks <= 0f)
            {
                return;
            }

            CompJellyMaker jellyMaker = pawn.GetComp<CompJellyMaker>();
            if (jellyMaker == null)
            {
                return;
            }

            float conversionProgress = Mathf.Clamp(Progress, 0f, 1f);
            if (conversionProgress <= 0f)
            {
                return;
            }

            converted = true;
            jellyMaker.ConvertToJelly(target, conversionProgress);
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
            Toil toil = ToilMaker.MakeToil("AttemptJellyMaking");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                CompJellyMaker jellyMaker = pawn.GetComp<CompJellyMaker>();
                if (!HasValidTargetCell() || jellyMaker == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                TicksFinish = jellyMaker.JellyFromCell(target)*jellyMaker.WorkPerJelly;
                if (TicksFinish <= 0f)
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                if (!HasValidTargetCell())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Ticks += pawn.GetStatValue(ExternalDefOf.CookSpeed) * delta;
                if (pawn?.skills != null)
                {
                    pawn.skills.Learn(SkillDefOf.Cooking, xpPerTick * delta);
                }
                if (pawn?.needs?.joy != null)
                {
                    pawn.needs.joy.GainJoy(0.001f * delta, InternalDefOf.NestTending);
                }

                Progress = Ticks / TicksFinish;
                if (Ticks >= TicksFinish)
                {
                    Progress = 1f;
                    TryConvertWorkedJelly();
                    ReadyForNextToil();
                }

            };
            toil.AddFinishAction(delegate
            {
                TryConvertWorkedJelly();
            });
            toil.WithProgressBar(TargetIndex.A, () => Progress);
            toil.WithEffect(InternalDefOf.ResinBuild, TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }
    }
}
