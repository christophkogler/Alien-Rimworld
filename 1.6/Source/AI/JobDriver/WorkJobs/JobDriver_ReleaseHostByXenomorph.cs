using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Xenomorphtype
{
    public class JobDriver_ReleaseHostByXenomorph : JobDriver_ClimbToPosition
    {
        private const TargetIndex HostInd = TargetIndex.A;
        private const TargetIndex ReleaseCellInd = TargetIndex.B;
        private const float GrabTicksFinish = 30f;

        private float grabTicks;
        private float grabProgress;
        private bool failedGrab;

        protected override IntVec3 FinalGoalCell => job.GetTarget(ReleaseCellInd).Cell;
        private Pawn Host => job.GetTarget(HostInd).Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(HostInd);
            this.FailOnAggroMentalState(HostInd);
            this.AddFailCondition(() => failedGrab || !IsReleasableHost(Host) || !FinalGoalCell.IsValid);

            yield return Toils_Goto.GotoThing(HostInd, PathEndMode.ClosestTouch);
            yield return AttemptGrab();
            yield return Toils_Haul.StartCarryThing(HostInd);
            yield return Toils_Haul.CarryHauledThingToCell(ReleaseCellInd, PathEndMode.OnCell);
            yield return DropHost();
            yield return ReleaseHost();
        }

        private Toil AttemptGrab()
        {
            Toil toil = ToilMaker.MakeToil("AttemptReleaseGrab");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                CompMatureMorph matureMorph = pawn.GetMorphComp();
                if (matureMorph != null && !matureMorph.InitiateGrabCheck(Host))
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
                    matureMorph.TryGrab(Host);
                }
            });
            toil.WithProgressBar(HostInd, () => grabProgress);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }

        private Toil DropHost()
        {
            Toil toil = ToilMaker.MakeToil("DropReleasedHost");
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

        private Toil ReleaseHost()
        {
            Toil toil = ToilMaker.MakeToil("ReleaseHost");
            toil.initAction = delegate
            {
                Pawn host = Host;
                if (host == null)
                {
                    return;
                }

                host.MapHeld.designationManager.TryRemoveDesignationOn(host, XenoWorkDefOf.XMT_Release);
                Hediff cocoon = host.health.hediffSet.GetFirstHediffOfDef(InternalDefOf.StarbeastCocoon);
                if (cocoon != null)
                {
                    host.health.RemoveHediff(cocoon);
                }

                if (host.IsPrisonerOfColony)
                {
                    GenGuest.PrisonerRelease(host);

                    if (!PawnBanishUtility.WouldBeLeftToDie(host, host.Map.Tile))
                    {
                        GenGuest.AddHealthyPrisonerReleasedThoughts(host);
                    }
                }

                QuestUtility.SendQuestTargetSignals(host.questTags, "Released", host.Named("SUBJECT"));

                if (host.Spawned && host.Position.OnEdge(host.Map))
                {
                    host.ExitMap(false, Rot4.Invalid);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private static bool IsReleasableHost(Pawn host)
        {
            return host != null
                && !host.Dead
                && !XMTUtility.IsXenomorph(host)
                && XMTUtility.IsCocooned(host);
        }
    }
}
