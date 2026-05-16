using System.Collections.Generic;
using Verse;

namespace Xenomorphtype
{
    internal static class StarbeastCapacityCache
    {
        private static readonly Dictionary<int, Dictionary<BodyPartGroupDef, float>> NaturalPartsAverageEfficiencyByPawn = new Dictionary<int, Dictionary<BodyPartGroupDef, float>>();

        public static bool TryGetNaturalPartsAverageEfficiency(HediffSet hediffSet, BodyPartGroupDef bodyPartGroup, out float efficiency)
        {
            efficiency = 0f;
            if (!CanCache(hediffSet, bodyPartGroup))
            {
                return false;
            }

            return NaturalPartsAverageEfficiencyByPawn.TryGetValue(hediffSet.pawn.thingIDNumber, out Dictionary<BodyPartGroupDef, float> pawnCache)
                && pawnCache.TryGetValue(bodyPartGroup, out efficiency);
        }

        public static void StoreNaturalPartsAverageEfficiency(HediffSet hediffSet, BodyPartGroupDef bodyPartGroup, float efficiency)
        {
            if (!CanCache(hediffSet, bodyPartGroup))
            {
                return;
            }

            int thingId = hediffSet.pawn.thingIDNumber;
            if (!NaturalPartsAverageEfficiencyByPawn.TryGetValue(thingId, out Dictionary<BodyPartGroupDef, float> pawnCache))
            {
                pawnCache = new Dictionary<BodyPartGroupDef, float>();
                NaturalPartsAverageEfficiencyByPawn.Add(thingId, pawnCache);
            }

            pawnCache[bodyPartGroup] = efficiency;
        }

        public static void Invalidate(HediffSet hediffSet)
        {
            Invalidate(hediffSet?.pawn);
        }

        public static void Invalidate(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            NaturalPartsAverageEfficiencyByPawn.Remove(pawn.thingIDNumber);
        }

        private static bool CanCache(HediffSet hediffSet, BodyPartGroupDef bodyPartGroup)
        {
            Pawn pawn = hediffSet?.pawn;
            return pawn != null
                && bodyPartGroup != null
                && pawn.def == InternalDefOf.XMT_Starbeast_AlienRace;
        }
    }
}
