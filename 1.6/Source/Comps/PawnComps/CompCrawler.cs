

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Xenomorphtype
{
    [StaticConstructorOnStartup]
    internal class CompCrawler : ThingComp
    {
        static private Texture2D CrawlTexture => ContentFinder<Texture2D>.Get("UI/Abilities/Starbeast_Crawl");
        static private Texture2D StandTexture => ContentFinder<Texture2D>.Get("UI/Abilities/Starbeast_Stand");
        CompCrawlerProperties Props => props as CompCrawlerProperties;

        Pawn Parent => parent as Pawn;

       

        bool _crawling;
        public bool Crawling
        {
            get
            {
                return IsCrawling(Parent);
            }
            set
            {
                if(!value)
                {
                    Parent.jobs.posture = PawnPosture.Standing;
                }
                else
                {
                    Parent.jobs.posture = PawnPosture.LayingOnGroundNormal;
                }
                _crawling = value;
            }
        }

        public static bool IsCrawling(Pawn pawn)
        {
            if (pawn == null || pawn.Downed || !pawn.health.CanCrawl)
            {
                return false;
            }

            if (!pawn.ageTracker.Adult)
            {
                return true;
            }

            return pawn.GetComp<CompCrawler>()?._crawling == true;
        }
        

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _crawling, "crawling", false);

        }
        public override float GetStatFactor(StatDef stat)
        {
            if(stat == StatDefOf.CrawlSpeed)
            {
                return (base.GetStatFactor(stat) * 0.5f) + (Parent.health.capacities.GetLevel(PawnCapacityDefOf.Moving ) * 0.5f);
            }

            if (stat == StatDefOf.MeleeCooldownFactor)
            {
                if (_crawling)
                {
                    return base.GetStatFactor(stat) * 2;
                }
            }

            if (stat == StatDefOf.WorkSpeedGlobal)
            {
                if (_crawling)
                {
                    return base.GetStatFactor(stat) * 0.5f;
                }
            }
            return base.GetStatFactor(stat);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Parent.Downed)
            {
                yield break;
            }

            if(Parent.Faction == null || !Parent.Faction.IsPlayer)
            {
                yield break;
            }

            if(!Parent.ageTracker.Adult)
            {
                yield break;
            }

            if (_crawling)
            {
                Command_Action CrawlAction = new Command_Action();
                CrawlAction.defaultLabel = "XMT_StandLabel".Translate();
                CrawlAction.defaultDesc = "XMT_StandDescription".Translate();
                CrawlAction.icon = StandTexture;
                CrawlAction.action = delegate
                {
                    Crawling = false;

                };

                yield return CrawlAction;
            }
            else
            {
                Command_Action CrawlAction = new Command_Action();
                CrawlAction.defaultLabel = "XMT_CrawlLabel".Translate();
                CrawlAction.defaultDesc = "XMT_CrawlDescription".Translate();
                CrawlAction.icon = CrawlTexture;
                CrawlAction.action = delegate
                {
                    Crawling = true;
                };

                yield return CrawlAction;
            }
        }
    }
    public class CompCrawlerProperties : CompProperties
    {
        public CompCrawlerProperties()
        {
            this.compClass = typeof(CompCrawler);
        }
        public CompCrawlerProperties(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }
    }
}
