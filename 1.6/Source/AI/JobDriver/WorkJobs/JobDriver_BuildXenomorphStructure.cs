using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Verse.Noise;

namespace Xenomorphtype { 
    internal class JobDriver_BuildXenomorphStructure : JobDriver
    {
        private float IncreasedDifficulty = 0;
        private float TicksFinish => (pawn.CurJob.plantDefToSow != null ? pawn.CurJob.plantDefToSow.statBases.GetStatValueFromList(StatDefOf.WorkToBuild,250) : 60) + IncreasedDifficulty;
        private ThingDef BuildingDef => pawn.CurJob.plantDefToSow;
        private float Ticks = 0;
        private float Progress = 0;
        protected float xpPerTick = 0.085f;
        private bool BuildingCommitted = false;
        
        public IntVec3 BuildCell
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Cell;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        public bool IsNoLongerValidTarget()
        {
            return BuildingDef == null;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFailCondition(IsNoLongerValidTarget);

            Toil toil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch).FailOn(() => Find.TickManager.TicksGame > startTick + 5000 && (float)(job.GetTarget(TargetIndex.A).Cell - pawn.Position).LengthHorizontalSquared > 4f);
            yield return toil;
            yield return DoToilBuilding();
        }

        private Toil DoToilBuilding()
        {
            Toil toil = ToilMaker.MakeToil("AttemptBuilding");
            toil.atomicWithPrevious = true;
            toil.initAction = delegate
            {
                Thing obstruction = BuildCell.GetEdifice(pawn.Map);
                if (obstruction != null)
                {
                    IncreasedDifficulty = obstruction.HitPoints / 5;
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                Ticks += (pawn.GetStatValue(StatDefOf.ConstructionSpeedFactor)*delta);
                if (pawn.skills != null)
                {
                    pawn.skills.Learn(SkillDefOf.Construction, xpPerTick * delta);
                }
                Progress = (Ticks / TicksFinish);

                if (!BioUtility.PerformBioconstructionCost(pawn, delta))
                {
                    this.FailOnMentalState(TargetIndex.A);
                    return;
                }

                if (Ticks >= TicksFinish)
                {
                    if (!TryCommitBuilding())
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    ReadyForNextToil();
                }

            };
            toil.WithProgressBar(TargetIndex.A, () => Progress);
            toil.WithEffect(InternalDefOf.ResinBuild, TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }

        private bool TryCommitBuilding()
        {
            Pawn actor = pawn;
            ThingDef buildingDef = BuildingDef;
            IntVec3 buildCell = BuildCell;
            if (BuildingCommitted || actor == null || actor.Destroyed || actor.Dead || !actor.Spawned || actor.Map == null)
            {
                return false;
            }

            if (buildingDef == null || buildingDef.category != ThingCategory.Building || !buildCell.InBounds(actor.Map))
            {
                return false;
            }

            if (!actor.Position.AdjacentTo8WayOrInside(buildCell))
            {
                return false;
            }

            Building finishedBuilding = GenSpawn.Spawn(buildingDef, buildCell, actor.Map, WipeMode.FullRefund) as Building;
            if (finishedBuilding == null)
            {
                return false;
            }

            finishedBuilding.SetFaction(actor.Faction);
            BuildingCommitted = true;
            return true;
        }
    }
}
