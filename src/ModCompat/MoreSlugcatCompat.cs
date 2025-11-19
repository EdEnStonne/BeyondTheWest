using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using BepInEx;
using BepInEx.Logging;
using System;
using MonoMod.Cil;
using RWCustom;
using Mono.Cecil.Cil;
using JetBrains.Annotations;
using BeyondTheWest;
using MoreSlugcats;
using System.Net.NetworkInformation;

public class MoreSlugcatCompat
{
    //---------- Objects
    public class LightingArcManager
    {
        public LightingArcManager(BodyChunk from, BodyChunk target, float intensity, Color color)
        {
            this.from = from;
            this.target = target;
            if (this.from.owner?.room != null)
            {
                Room room = this.from.owner.room;
                this.lightningArc = new LightningBolt(from.pos, target.pos, 0, intensity, intensity/5f, 0.64f, 0.64f, true);
                this.lightningArc.intensity = intensity / 3f;
                this.lightningArc.color = color;
                // room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, target, false, 0.35f, 1.8f - UnityEngine.Random.value * 0.5f);
                room.AddObject(this.lightningArc);
            }
        }

        public void Update()
        {
            if (this.from != null && this.target != null && this.lightningArc != null && !this.lightningArc.slatedForDeletetion)
            {
                this.lightningArc.from = this.from.pos;
                this.lightningArc.target = this.target.pos;
                timelived++;
            }
            else
            {
                mustDestroy = true;
            // Plugin.Log("Deleting Arc "+ this.lightningArc.ToString() + " " + timelived);
            }
        }

        public LightningBolt lightningArc;
        public BodyChunk from;
        public BodyChunk target;
        public bool mustDestroy = false;
        public int timelived = 0;
    }

    //---------- Functions

    // Spark
    public static ConditionalWeakTable<SparkObject.StaticChargeManager, List<LightingArcManager>> cwtLightingArc = new();
    public static void StaticManager_CheckIfArtififerShouldExplode(SparkObject.StaticChargeManager SCM, Creature creature)
    {
        if (creature is Player player && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
        {
            player.PyroDeath();
        }
    }
    public static void StaticManager_AddSCMToLigningArcCWT(SparkObject.StaticChargeManager SCM)
    {
        cwtLightingArc.Add(SCM, new List<LightingArcManager>());
        
    }
    public static void StaticManager_AddLigningArc(SparkObject.StaticChargeManager SCM, BodyChunk target, float intensity)
    {
        Player player = SCM.Player;
        if (player != null && cwtLightingArc.TryGetValue(SCM, out var lightingArcs))
        {
            // Plugin.Log("Adding Arc");
            lightingArcs.Add(new LightingArcManager(player.firstChunk, target, intensity, player.ShortCutColor()));
        }
    }
    public static void StaticManager_UpdateLigningArcs(SparkObject.StaticChargeManager SCM)
    {
        Player player = SCM.Player;
        if (player != null && cwtLightingArc.TryGetValue(SCM, out var lightingArcs))
        {
            foreach (LightingArcManager lightingArc in lightingArcs)
            {
                // Plugin.Log("Updating Arc "+ lightingArc.lightningArc.ToString());
                lightingArc.Update();
            }
            lightingArcs.RemoveAll(x => x.mustDestroy);
        }
    }
    
    // RainTimerAddition
    

    // Hooks
    public static void ApplyHooks()
    {

    }

    //----------- Hooks

}
