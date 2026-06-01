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
    internal class JobDriver_CocoonTarget : JobDriver_ClimbToPosition
    {

        private const float GrabTicksFinish = 30;
        private float GrabTicks = 0;
        private float GrabProgress = 0;

        private float CocoonTicksFinish = 350;
        private float CocoonTicks = 0;
        private float CocoonProgress = 0;

        private bool FailedGrab = false;

        protected override IntVec3 FinalGoalCell => job.GetTarget(TargetIndex.B).Cell;
        public Thing ToHaul => job.GetTarget(TargetIndex.A).Thing;
        public Pawn Victim => job.GetTarget(TargetIndex.A).Pawn;

        protected virtual bool DropCarriedThingIfNotTarget => false;

        public override void ExposeData()
        {
            base.ExposeData();

        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {

            return true;
        }

        public bool IsNoLongerValidTarget()
        {
            return FailedGrab;
        }

        private bool TryCommitGrab()
        {
            if (!TryGetCommitContext(false, out _, out Pawn victim, out CompMatureMorph matureMorph))
            {
                return false;
            }

            matureMorph.TryGrab(victim);
            return true;
        }

        private bool TryCommitCocoon()
        {
            if (!TryGetCommitContext(true, out _, out Pawn victim, out CompMatureMorph matureMorph))
            {
                return false;
            }

            matureMorph.TryCocooning(victim);
            return true;
        }

        private bool TryGetCommitContext(bool victimMustBeCarried, out Pawn actor, out Pawn victim, out CompMatureMorph matureMorph)
        {
            actor = pawn;
            victim = Victim;
            matureMorph = null;
            if (actor == null || actor.Destroyed || actor.Dead || !actor.Spawned || actor.Map == null || actor.mindState == null)
            {
                return false;
            }

            if (victim == null || victim.Destroyed || victim.Dead || victim.health == null)
            {
                return false;
            }

            if (victimMustBeCarried)
            {
                if (victim.CarriedBy != actor)
                {
                    return false;
                }
            }
            else if (!victim.Spawned || actor.Map != victim.Map || !actor.Position.AdjacentTo8WayOrInside(victim.Position))
            {
                return false;
            }

            matureMorph = actor.GetMorphComp();
            if (matureMorph == null)
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.AddFailCondition(IsNoLongerValidTarget);

            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnAggroMentalState(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return AttemptGrab();
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return AttemptCocoon();
        }


        private Toil AttemptGrab()
        {
            Toil toil = ToilMaker.MakeToil("AttemptGrab");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                CompMatureMorph matureMorph = pawn.GetMorphComp();
                if (matureMorph != null)
                {
                    if (!matureMorph.InitiateGrabCheck(Victim))
                    {
                        FailedGrab = true;
                    }
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                GrabTicks += delta;
                GrabProgress = (GrabTicks / GrabTicksFinish);
                if (GrabTicks >= GrabTicksFinish)
                {
                    if (!TryCommitGrab())
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    ReadyForNextToil();
                }

            };
            toil.WithProgressBar(TargetIndex.A, () => GrabProgress);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect(EffecterDefOf.Breastfeeding, TargetIndex.A);
            return toil;
        }

        private Toil AttemptCocoon()
        {
            Toil toil = ToilMaker.MakeToil("FormingCocoon");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                if (pawn.Position.GetTerrain(pawn.Map) != XenoBuildingDefOf.HiveFloor)
                {
                    CocoonTicksFinish = XenoBuildingDefOf.Hivemass.statBases.GetStatValueFromList(StatDefOf.WorkToBuild, 10f);
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                Pawn actor = pawn;
                CocoonTicks += delta;
                CocoonProgress = (CocoonTicks / CocoonTicksFinish);
                if (actor?.needs?.food != null)
                {
                    actor.needs.food.CurLevel = actor.needs.food.CurLevel - XMTHiveUtility.HiveHungerCostPerTick * delta;

                    if (actor.needs.food.Starving)
                    {
                        Hediff Malnutrition = actor.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition);

                        if (Malnutrition != null)
                        {
                            Malnutrition.Severity += 0.001f * delta;
                            actor.workSettings.Disable(WorkTypeDefOf.Construction);
                        }
                        ReadyForNextToil();
                        return;
                    }
                }

                if (CocoonTicks >= CocoonTicksFinish)
                {
                    if (!TryCommitCocoon())
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    ReadyForNextToil();
                }

            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithProgressBar(TargetIndex.A, () => CocoonProgress);
            toil.WithEffect(InternalDefOf.ResinBuild, TargetIndex.A);
            return toil;
        }
    }
}
