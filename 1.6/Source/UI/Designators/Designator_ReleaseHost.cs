using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Xenomorphtype
{
    internal class Designator_ReleaseHost : Designator
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

        public Designator_ReleaseHost()
        {
            defaultLabel = "XMT_CommandReleaseHost".Translate();
            defaultDesc = "XMT_CommandReleaseHostDescription".Translate();
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

            if (!ReleasableHostsInCell(c).Any())
            {
                return "XMT_MessageMustDesignateReleasableHost".Translate();
            }

            return true;
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            foreach (Pawn host in ReleasableHostsInCell(loc))
            {
                DesignateThing(host);
            }
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (t is Pawn pawn && IsReleasableHost(pawn) && base.Map.designationManager.DesignationOn(pawn, Designation) == null)
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

        private IEnumerable<Pawn> ReleasableHostsInCell(IntVec3 c)
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

        private static bool IsReleasableHost(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !XMTUtility.IsXenomorph(pawn)
                && XMTUtility.IsCocooned(pawn);
        }
    }
}
