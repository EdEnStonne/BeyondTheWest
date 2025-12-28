using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using Noise;
using System.Collections.Generic;
using BeyondTheWest.MSCCompat;

namespace BeyondTheWest;
public class ElectricExplosion : UpdatableAndDeletable
{
    public static void MakeSparkExplosion(Room room, float size, Vector2 position, byte sparks, bool underwater, Color color)
    {
        if (underwater) { room.AddObject(new UnderwaterShock.Flash(position, size, 1f, 30, color)); }

        room.AddObject(new Explosion.ExplosionLight(position, size, 1f, 5, color));
        room.AddObject(new ShockWave(position, size, 0.001f, 50, false));
        for (int i = sparks; i >= 0; i--)
        {
            room.AddObject(new MouseSpark(position, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)), 25f, color));
        }
    }
    public static void ShockCreature(Creature target, BodyChunk closestBodyChunk, PhysicalObject sourceObject, 
        Creature killTagHolder, float killTagHolderDmgFactor, float damage, float stun, 
        Color color, bool doSpams = false, bool notifyMeadow = false, bool hitPlayer = true, 
        List<PhysicalObject> sparedObjects = null)
    {
        if (target?.room == null) { return; }
        if (color == null) { color = Color.white; }
        if (sparedObjects == null) { sparedObjects = new(); }

        bool isMine = BTWFunc.IsLocal(target);
        
        if (killTagHolder != null) { 
            target.SetKillTag(killTagHolder.abstractCreature); 

            if (target == killTagHolder)
            {
                damage *= killTagHolderDmgFactor;
            }

            if (ModManager.MSC && closestBodyChunk != null)
            {
                BodyChunk mainChunk = killTagHolder.mainBodyChunk ?? killTagHolder.firstChunk;
                LightingArc lightingArc = new (
                    mainChunk, closestBodyChunk,
                    0.5f + damage, damage > 1 ? 1f : 0.5f, (int)(stun / 10f + 5), color
                );
                target.room.AddObject(lightingArc);
            }
        }

        if ((!hitPlayer && target is Player) || sparedObjects.Exists(x => x == target)) { damage = 0; }
        
        if (isMine)
        {
            target.Violence(sourceObject?.firstChunk, null, closestBodyChunk, null, Creature.DamageType.Electric, damage, stun);
            
            if (doSpams)
            {
                target.room.AddObject(new CreatureSpasmer(target, false, target.stun));
            }
            if (damage > 1f)
            {
                target.LoseAllGrasps();
            }
        }

        if (Plugin.meadowEnabled && notifyMeadow && !BTWFunc.IsLocal(target))
        {
            MeadowCalls.SparMeadow_ShockCreatureRPC(target, closestBodyChunk, sourceObject, 
                killTagHolder, killTagHolderDmgFactor, damage, stun, color, doSpams);
        }
    }
    public ElectricExplosion(
        Room room, PhysicalObject sourceObject, Vector2 pos, 
        int lifeTime, float rad, float force, float maxdamage, float maxstun, 
        Creature killTagHolder, float killTagHolderDmgFactor, float backgroundNoise, 
        bool underwater = false, bool doSpams = false, bool notifyMeadow = false)
    {
        // Plugin.Log("Alright, ElectricExplosion created :\n"
        //     +"  room : ["+ room +"]\n"
        //     +"  sourceObject : ["+ sourceObject +"]\n"
        //     +"  pos : ["+ pos +"]\n"
        //     +"  lifeTime : ["+ lifeTime +"]\n"
        //     +"  rad : ["+ rad +"]\n"
        //     +"  force : ["+ force +"]\n"
        //     +"  maxdamage : ["+ maxdamage +"]\n"
        //     +"  maxstun : ["+ maxstun +"]\n"
        //     +"  killTagHolder : ["+ killTagHolder +"]\n"
        //     +"  killTagHolderDmgFactor : ["+ killTagHolderDmgFactor +"]\n"
        //     +"  backgroundNoise : ["+ backgroundNoise +"]\n"
        //     +"  underwater : ["+ underwater +"]\n"
        //     +"  doSpams : ["+ doSpams +"]\n"
        //     +"  notifyMeadow : ["+ notifyMeadow+"]");
        this.room = room;
        this.sourceObject = sourceObject;
        this.pos = pos;
        this.lifeTime = lifeTime;
        this.rad = rad;
        this.force = force;
        this.underwater = underwater;
        this.maxDamage = maxdamage;
        this.maxStun = maxstun;
        this.killTagHolder = killTagHolder;
        this.killTagHolderDmgFactor = killTagHolderDmgFactor;
        this.backgroundNoise = backgroundNoise;
        this.doSpams = doSpams;
        this.notifyMeadow = notifyMeadow;

        if (Plugin.meadowEnabled && notifyMeadow)
        {
            MeadowCalls.SparMeadow_ElectricExplosionRPC(this);
        }
        if (killTagHolder != null)
        {
            if (killTagHolderDmgFactor <= 0)
            {
                this.objectsHit.Add(killTagHolder);
            }
            if (killTagHolder.grasps != null)
            {
                foreach (Creature.Grasp grasp in this.killTagHolder.grasps)
                {
                    if (grasp != null && grasp.grabbed != null)
                    {
                        this.passThroughObjects.Add(grasp.grabbed);
                    }
                }
            }
        }
    }

    //-------------- Functions
    public override void Update(bool eu)
    {
        base.Update(eu);
        if (!this.chainReactionNotified && this.room != null)
        {
            this.chainReactionNotified = true;
            for (int i = 0; i < this.room.updateList.Count; i++)
            {
                if (this.room.updateList[i] is IReactToElectricExplosion)
                {
                    (this.room.updateList[i] as IReactToElectricExplosion).Explosion(this);
                }
            }
            if (this.sourceObject != null)
            {
                this.room.InGameNoise(new InGameNoise(this.pos, this.backgroundNoise * 900f, this.sourceObject, this.backgroundNoise * 4f));
            }
            byte sparks = (byte)(UnityEngine.Random.Range(5f, 10f) * (1 + this.maxDamage));
            MakeSparkExplosion(this.room, this.rad, this.pos, sparks, this.underwater, this.color);
            if (this.maxDamage > 1f) { room.ScreenMovement(this.pos, default, this.maxDamage / 10f); }

            room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, this.pos, 0.5f + Math.Min(1f, this.volume), BTWFunc.Random(1.1f, 1.5f));
            room.PlaySound(SoundID.Bomb_Explode, this.pos, this.volume / 2f, BTWFunc.Random(1.75f, 2.25f));
        }
        this.room.MakeBackgroundNoise(this.backgroundNoise);

        if (this.frame < this.lifeTime 
            && this.rad > 0 
            && (this.maxDamage > 0 || this.maxStun > 0)
            && this.room != null)
        {
            for (int j = 0; j < this.room.physicalObjects.Length; j++)
            {
                for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
                {
                    PhysicalObject obj = this.room.physicalObjects[j][k];
                    RadiusCheckResultObject result = new(obj);

                    if ((!this.objectsHit.Exists(x => x == obj))
                        && (!this.onlyHitCreatures || obj is Creature)
                        && (!this.passThroughObjects.Exists(x => x == obj))
                        && (this.sourceObject == null || BTWFunc.CanTwoObjectsInteract(this.sourceObject, obj))
                        && BTWFunc.IsObjectInRadius(obj, this.pos, this.rad, out result)
                        && !(Plugin.meadowEnabled 
                            && MeadowFunc.IsMeadowArena() 
                            && this.killTagHolder != null 
                            && this.killTagHolder is Player pl && pl != null 
                            && obj is Creature cr 
                            && MeadowFunc.IsCreatureFriendlies(pl, cr)))
                    {
                        bool cthroughWall = !room.VisualContact(this.pos, this.pos + result.distance * result.vectorDistance);
                        bool submerged = BTWFunc.BodyChunkSumberged(result.closestBodyChunk);
                        if (cthroughWall && !this.hitThroughtWalls) { continue; }
                        if ((submerged && !this.hitSubmerged) || (!submerged && !this.hitNonSubmerged)) { continue; }

                        Vector2 knockbackDir = this.forcedKnockbackDirection == Vector2.zero ? result.vectorDistance : this.forcedKnockbackDirection;
                        float ratioDist = Mathf.Clamp01(1 - (result.distance / this.rad));
                        float dmg = this.maxDamage * (
                            this.baseDamageFraction + (1 - this.baseDamageFraction) * Mathf.Pow(ratioDist, 2));
                        float force = this.force;
                        int stun = (int)(this.maxStun * (
                            this.baseStunFraction + (1 - this.baseStunFraction) * Mathf.Pow(ratioDist, 2)));

                        if (cthroughWall) { dmg *= 0.35f; stun = (int)(stun * 0.25f); }
                        if (this.sparedObjects.Exists(x => x == obj)) { dmg = 0f; stun *= 2; force *= 0.1f; }
                        if (submerged) { stun += 1; force *= 0.25f; }

                        bool targetLocal = BTWFunc.IsLocal(obj.abstractPhysicalObject);
                        if (targetLocal)
                        {
                            foreach (BodyChunk b in result.bodyChunksHit)
                            {
                                BTWFunc.CustomKnockback(b, knockbackDir, force, notifyMeadow);
                            }
                        }

                        if (ModManager.MSC)
                        {
                            if (targetLocal && dmg > 1.5f && obj is Player player && player != null)
                            {
                                MSCCalls.ExplodeArtificer(player);
                            }
                        }
                        if (obj is Creature creature)
                        {
                            Plugin.Log("Ouch ! Creature ["+ creature +"] got shocked by ["+ this.killTagHolder 
                                +"] using ["+ this.sourceObject +"] !");
                            Plugin.Log("Took <"+ dmg +"/"+ this.maxDamage +"> damage and <"
                                + stun +"/"+ this.maxStun +"> stun (reach ratio is <"+ ratioDist +">).");
                            
                            ShockCreature(creature, result.closestBodyChunk, this.sourceObject, this.killTagHolder,
                                this.killTagHolderDmgFactor, dmg, stun, this.color, this.doSpams, this.notifyMeadow,
                                this.hitPlayer, this.sparedObjects);
                        }
                        this.objectsHit.Add(obj);
                    }
                }
            }
        }
        this.frame++;
        
        if (this.frame >= this.lifeTime)
        {
            this.Destroy();
        }
    }

    //------------- Variables
    // Objects
    public PhysicalObject sourceObject;
    public Creature killTagHolder;
    public Vector2 pos;
    public List<PhysicalObject> sparedObjects = new();
    public List<PhysicalObject> passThroughObjects = new();
    public List<PhysicalObject> objectsHit = new();
    public Vector2 forcedKnockbackDirection = Vector2.zero;
    public Color color = Color.white;

    // Basic
    public bool doSpams = false;
    public bool notifyMeadow = false;
    public bool chainReactionNotified = false;
    public bool hitSubmerged = true;
    public bool hitNonSubmerged = true;
    public bool hitThroughtWalls = true;
    public bool onlyHitCreatures = false;
    public bool hitPlayer = true;
    public bool underwater = false;

    public int lifeTime;
    public int frame = 0;
    public float rad;
    public float force;
    public float maxDamage;
    public float maxStun;
    public float killTagHolderDmgFactor;
    public float backgroundNoise;
    public float baseDamageFraction = 0.5f;
    public float baseStunFraction = 0.25f;
    public float volume = 0.5f;
    public interface IReactToElectricExplosion // eh may be useful for later
    {
        void Explosion(ElectricExplosion electricExplosion);
    }
}
    