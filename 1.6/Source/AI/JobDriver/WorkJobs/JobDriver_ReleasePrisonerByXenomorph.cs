using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Xenomorphtype
{
    public class JobDriver_ReleasePrisonerByXenomorph : JobDriver_ClimbToPosition
    {
        private const TargetIndex PrisonerInd = TargetIndex.A;
        private const TargetIndex ReleaseCellInd = TargetIndex.B;
        private const float GrabTicksFinish = 30f;

        private float grabTicks;
        private float grabProgress;
        private bool failedGrab;

        protected override IntVec3 FinalGoalCell => job.GetTarget(ReleaseCellInd).Cell;
        private Pawn Prisoner => job.GetTarget(PrisonerInd).Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(PrisonerInd);
            this.FailOnAggroMentalState(PrisonerInd);
            this.AddFailCondition(() => failedGrab || Prisoner == null || !Prisoner.IsPrisonerOfColony || !FinalGoalCell.IsValid);

            yield return Toils_Goto.GotoThing(PrisonerInd, PathEndMode.ClosestTouch);
            yield return AttemptGrab();
            yield return Toils_Haul.StartCarryThing(PrisonerInd);
            yield return Toils_Haul.CarryHauledThingToCell(ReleaseCellInd, PathEndMode.OnCell);
            yield return DropPrisoner();
            yield return ReleasePrisoner();
        }

        private Toil AttemptGrab()
        {
            Toil toil = ToilMaker.MakeToil("AttemptReleaseGrab");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                CompMatureMorph matureMorph = pawn.GetMorphComp();
                if (matureMorph != null && !matureMorph.InitiateGrabCheck(Prisoner))
                {
                    failedGrab = true;
                }
            };
            toil.tickAction = delegate
            {
                grabTicks += 1f;
                grabProgress = grabTicks / GrabTicksFinish;
                if (grabTicks >= GrabTicksFinish)
                {
                    ReadyForNextToil();
                }
            };
            toil.AddFinishAction(delegate
            {
                CompMatureMorph matureMorph = pawn.GetMorphComp();
                if (matureMorph != null && grabProgress >= 1f && !failedGrab)
                {
                    matureMorph.TryGrab(Prisoner);
                }
            });
            toil.WithProgressBar(PrisonerInd, () => grabProgress);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }

        private Toil DropPrisoner()
        {
            Toil toil = ToilMaker.MakeToil("DropReleasedPrisoner");
            toil.initAction = delegate
            {
                if (pawn.carryTracker.CarriedThing != null)
                {
                    pawn.carryTracker.TryDropCarriedThing(FinalGoalCell, ThingPlaceMode.Near, out Thing _);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private Toil ReleasePrisoner()
        {
            Toil toil = ToilMaker.MakeToil("ReleasePrisoner");
            toil.initAction = delegate
            {
                Pawn prisoner = Prisoner;
                if (prisoner == null)
                {
                    return;
                }

                prisoner.MapHeld.designationManager.TryRemoveDesignationOn(prisoner, XenoWorkDefOf.XMT_Release);
                GenGuest.PrisonerRelease(prisoner);

                if (!PawnBanishUtility.WouldBeLeftToDie(prisoner, prisoner.Map.Tile))
                {
                    GenGuest.AddHealthyPrisonerReleasedThoughts(prisoner);
                }

                QuestUtility.SendQuestTargetSignals(prisoner.questTags, "Released", prisoner.Named("SUBJECT"));

                if (prisoner.Spawned && prisoner.Position.OnEdge(prisoner.Map))
                {
                    prisoner.ExitMap(false, Rot4.Invalid);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
