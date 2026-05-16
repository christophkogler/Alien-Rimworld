using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Xenomorphtype
{
    internal class Designator_ReleasePrisoner : Designator
    {
        public override bool DragDrawMeasurements => true;
        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

        protected override DesignationDef Designation => XenoWorkDefOf.XMT_Release;

        public override bool Disabled
        {
            get
            {
                return !XMTUtility.PlayerXenosOnMap(Find.CurrentMap);
            }
        }

        public override bool Visible
        {
            get
            {
                return XMTUtility.PlayerXenosOnMap(Find.CurrentMap);
            }
        }

        public Designator_ReleasePrisoner()
        {
            defaultLabel = "XMT_CommandReleasePrisoner".Translate();
            defaultDesc = "XMT_CommandReleasePrisonerDescription".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/Break");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Hunt;
            hotKey = KeyBindingDefOf.Misc12;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(base.Map))
            {
                return false;
            }

            if (!ReleasablePrisonersInCell(c).Any())
            {
                return "XMT_MessageMustDesignateReleasablePrisoner".Translate();
            }

            return true;
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            foreach (Pawn prisoner in ReleasablePrisonersInCell(loc))
            {
                DesignateThing(prisoner);
            }
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (t is Pawn pawn && pawn.IsPrisonerOfColony && !pawn.Dead && base.Map.designationManager.DesignationOn(pawn, Designation) == null)
            {
                return true;
            }

            return false;
        }

        public override void DesignateThing(Thing t)
        {
            base.Map.designationManager.RemoveAllDesignationsOn(t);
            base.Map.designationManager.AddDesignation(new Designation(t, Designation));
        }

        private IEnumerable<Pawn> ReleasablePrisonersInCell(IntVec3 c)
        {
            if (c.Fogged(base.Map))
            {
                yield break;
            }

            List<Thing> thingList = c.GetThingList(base.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (CanDesignateThing(thingList[i]).Accepted)
                {
                    yield return (Pawn)thingList[i];
                }
            }
        }
    }
}
