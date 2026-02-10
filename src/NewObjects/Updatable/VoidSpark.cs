using System;
using System.Collections.Generic;
using System.Linq;
using BeyondTheWest.Items;
using BeyondTheWest.MeadowCompat;
using BeyondTheWest.MSCCompat;
using RWCustom;
using UnityEngine;

namespace BeyondTheWest;

public class VoidSpark : UpdatableAndDeletable, IDrawable
{
    public static Vector2 GetPosition(UpdatableAndDeletable updatable)
    {
        Vector2 position = Vector2.negativeInfinity;
        if (updatable is Creature creature)
        {
            position = creature.mainBodyChunk.pos;
        }
        else if (updatable is PhysicalObject physicalObject)
        {
            position = physicalObject.firstChunk.pos;
        }
        return position;
    }
    public static bool HasAPosition(UpdatableAndDeletable updatable, out Vector2 position)
    {
        position = GetPosition(updatable);
        return position != Vector2.negativeInfinity;
    }
    public static void MakeDraggedSparks(Room room, float size, Vector2 position, byte sparks, Color color, float drag)
    {
        for (int i = sparks; i >= 0; i--)
        {
            room.AddObject(new MouseSparkDrag(position, BTWFunc.RandomCircleVector(size), 20f, color, drag));
        }
    }
    public static int ConductiveScore(UpdatableAndDeletable updatable)
    {
        if (updatable is VoidSpark)
        {
            return 0;
        }
        else if (updatable is Creature creature)
        {
            float score = 9f;

            if (creature.GetBTWData() is BTWCreatureData data)
            {
                if (data.voidSparkImmune)
                {
                    return 0;
                }
                else
                {
                    if (data.crystalInfected)
                    {
                        score += 12f;
                    }
                }
            }
            if (creature is Player player)
            {
                if (ModManager.MSC && MSCFunc.IsSaint(player))
                {
                    score += 1f;
                }
                else if (player.IsCore())
                {
                    score += 2f;
                }
                else if (ModManager.MSC && MSCFunc.IsArtificer(player))
                {
                    score -= 1f;
                }
            }

            if (creature.dead)
            {
                score /= 5f;
            }
            return (int)(score * BTWFunc.TileSize);
        }
        else if (updatable is Weapon weapon)
        {
            if (weapon is Rock)
            {
                return (int)(1.5f * BTWFunc.TileSize);
            }
            else if (weapon is Spear spear)
            {
                if (spear.bugSpear)
                {
                    return 12 * BTWFunc.TileSize;
                }
                else if (spear is CrystalSpear)
                {
                    return 20 * BTWFunc.TileSize;
                }
                return 3 * BTWFunc.TileSize;
            }
            else if (weapon is VoidCrystal)
            {
                return 15 * BTWFunc.TileSize;
            }
            return 1 * BTWFunc.TileSize;
        }
        else if (updatable is EnergyCore energyCore)
        {
            return (energyCore.AEC.energy > 0 ? 20 : 8) * BTWFunc.TileSize;
        }

        if (ModManager.MSC)
        {
            if (updatable is MoreSlugcats.SingularityBomb)
            {
                return 30 * BTWFunc.TileSize;
            }
            else if (updatable is MoreSlugcats.EnergyCell)
            {
                return 20 * BTWFunc.TileSize;
            }
        }
        return 0;
    }
    public static void HitSomethingWithVoidSpark(UpdatableAndDeletable target, float damage, Vector2 direction, AbstractCreature killtagholder)
    {
        if (target.room != null)
        {
            Room room = target.room;
            Vector2 position = GetPosition(target);
            if (target is Creature creature)
            {
                BodyChunk chunk = creature.mainBodyChunk;

                if (creature.Local())
                {
                    float stun = damage * BTWFunc.FrameRate * 3;
                    if (killtagholder != null)
                    {
                        creature.SetKillTag(killtagholder);
                    }
                    if (creature.GetBTWData() is BTWCreatureData data)
                    {
                        if (data.crystalInfected)
                        {
                            damage = 0f;
                            stun *= 3f;
                        }
                    }
                    
                    if (BTWPlugin.meadowEnabled)
                    {
                        ArenaDeathTracker.SetDeathTrackerOfCreature(creature.abstractCreature, 56, false, true);
                    }
                    creature.Violence(null, direction, chunk, null, Creature.DamageType.None, damage, stun);
                    if (BTWFunc.VectorProjectionNormSigned(chunk.vel, direction) < 40f)
                    {
                        BTWFunc.CustomKnockback(chunk, direction, Mathf.Min(40f, damage * 9f));
                    }
                    
                    if (ModManager.MSC && creature is Player targettedPlayer)
                    {
                        if (Math.Max(0, targettedPlayer.playerState.permanentDamageTracking) + damage >= 1.0)
                        {
                            targettedPlayer.Die();
                        }
                        targettedPlayer.playerState.permanentDamageTracking += 
                            (damage / targettedPlayer.Template.baseDamageResistance) * 0.5f;
                    }
                }
                
                room.PlaySound(SoundID.Bomb_Explode, position, 0.15f, BTWFunc.Random(0.6f, 0.7f));
                room.PlaySound(SoundID.Spear_Hit_Small_Creature, position, 0.65f, BTWFunc.Random(1.65f, 1.7f));
            }
            else if (target is PhysicalObject physicalObject)
            {
                if (physicalObject.Local())
                {
                    BTWFunc.CustomKnockback(physicalObject.firstChunk, direction, damage * 10f);
                    
                    if (physicalObject is EnergyCore energyCore)
                    {
                        bool wasOK = energyCore.AEC.energy > 0;
                        energyCore.AEC.energy -= damage * 300f;
                        if (energyCore.player != null)
                        {
                            BTWFunc.CustomKnockback(energyCore.player, direction, damage * 4f);
                            energyCore.player.stun = (int)Mathf.Max(damage * BTWFunc.FrameRate, energyCore.player.stun);
                        }
                        if (energyCore.AEC.energy <= 0)
                        {
                            energyCore.MeltdownStart();
                        }
                    }

                    if (ModManager.MSC)
                    {
                        if (physicalObject is MoreSlugcats.SingularityBomb singularityBomb)
                        {
                            singularityBomb.activateSingularity = true;
                        }
                        else if (physicalObject is MoreSlugcats.EnergyCell energyCell)
                        {
                            energyCell.Use(true);
                        }
                    }
                    room.PlaySound(SoundID.Bomb_Explode, position, 0.2f, BTWFunc.Random(0.6f, 0.7f));
                }
            }
            else
            {
                
            }

            room.PlaySound(SoundID.Death_Lightning_Spark_Object, position, 0.2f, BTWFunc.Random(1.75f, 1.8f));
        }
    }
    public static VoidSpark FindSparkByID(Room room, int ID)
    {
        int index = room.updateList.FindIndex(x => x is VoidSpark voidSpark && voidSpark.ID == ID);
        if (index > 0)
        {
            return room.updateList[index] as VoidSpark;
        }
        return null;
    }
    public static int nextID = 0;
    public static int NewID()
    {
        int newID = nextID;
        nextID++;
        return newID;
    }

    public VoidSpark(Vector2 position, float damage, int lifetime, bool fake = false) : base()
    {
        this.position = position;
        this.lastPosition = position;
        this.damage = damage;
        this.lifetime = lifetime;
        this.fake = fake;
        if (!fake)
        {
            this.ID = NewID();
        }
    }

    public float FinalScore(UpdatableAndDeletable updatable)
    {
        float score = updatable != null ? updatable.VoidConductiveScore() : 0;

        if (updatable == this.source || this.sparedList.Exists(x => x == updatable))
        {
            return 0;
        }

        if (HasAPosition(updatable, out var pos))
        {
            score /= (this.position - pos).magnitude;
        }
        else
        {
            score /= 100f;
        }

        return score;
    }
    public int HasGreaterScore(UpdatableAndDeletable updatable1, UpdatableAndDeletable updatable2)
    {
        return (int)(FinalScore(updatable1) - FinalScore(updatable2));
    }
    public UpdatableAndDeletable LookForTarget()
    {
        if (this.room != null)
        {
            List<UpdatableAndDeletable> objList = room.updateList
                .FindAll(x => x.VoidConductiveScore() > 0 && HasAPosition(x, out _));
            
            if (objList.Count > 0)
            {
                UpdatableAndDeletable target = objList[0];
                float finalScore = FinalScore(target);
                for (int i = 1; i < objList.Count; i++)
                {
                    if (FinalScore(objList[i]) > finalScore)
                    {
                        target = objList[i];
                        finalScore = FinalScore(objList[i]);
                    }
                }
                
                if (finalScore > this.damage)
                {
                    BTWPlugin.Log($"VoidSpark <{this.ID}> found [{target}] with the highest score of <{finalScore}>. Targetting with <{this.damage}> dmg.");
                    return target;
                }
            }
        }
        return null;
    }
    public void HitObject()
    {
        MakeDraggedSparks(this.room, 25f + 10f * this.damage, this.position, 
            (byte)(BTWFunc.RandInt(15, 25) + this.damage), this.color, 0.2f);
        
        BTWPlugin.Log($"VoidSpark hit [{this.target}] for <{this.damage}> dmg !");

        if (target is IReactToVoidFlux reactToVoidFlux)
        {
            reactToVoidFlux.HitByVoidSpark(this);
        }
        HitSomethingWithVoidSpark(this.target, this.damage, this.direction, this.killTagHolder);
        this.lifetime = 0;

        if (BTWPlugin.meadowEnabled && !this.fake)
        {
            MeadowCalls.BTWItems_VoidSparkHitSomething(this);
        }

        this.Destroy();
    }
    public void HitWall()
    {
        this.lifetime = 0;
        MakeDraggedSparks(this.room, 15f, this.position, (byte)BTWFunc.RandInt(12, 17), this.color, 0.2f);
        
        if (BTWPlugin.meadowEnabled && !this.fake)
        {
            MeadowCalls.BTWItems_VoidSparkExplode(this);
        }

        this.Destroy();
    }
    public void Dissipate()
    {
        this.lifetime = 0;
        MakeDraggedSparks(this.room, 7f, this.position, (byte)BTWFunc.RandInt(4, 7), this.color, 0.2f);
        
        if (BTWPlugin.meadowEnabled && !this.fake)
        {
            MeadowCalls.BTWItems_VoidSparkDissipate(this);
        }

        this.Destroy();
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.lifetime <= 0 && !this.slatedForDeletetion)
        {
            Dissipate();
            return;
        }

        if (!this.fake)
        {
            if (BTWPlugin.meadowEnabled && !this.meadowInit)
            {
               MeadowCalls.BTWItems_VoidSparkEnterRoom(this);
            }

            this.lastPosition = this.position;
            this.lifetime--;

            if (!this.chainReactionNotified)
            {
                for (int i = 0; i < this.room.updateList.Count; i++)
                {
                    if (this.room.updateList[i] is IReactToVoidFlux reactToVoidFlux)
                    {
                        reactToVoidFlux.VoidSparkAppears(this);
                    }
                }
                this.chainReactionNotified = true;
            }

            this.target ??= LookForTarget();

            if (this.room.GetTile(this.position).Solid)
            {
                HitWall();
            }
            else if (this.target != null)
            {
                Vector2 targetPos = GetPosition(target);
                Vector2 dir = targetPos - this.position;
                float step = damage * BTWFunc.TileSize * 5;
                Vector2 newPos = this.position + (dir.magnitude > step ? dir.normalized * step : dir);
                this.position = room.RayTraceSolid(this.position, newPos);
                this.direction = dir.normalized;

                if ((this.position - targetPos).magnitude < 1)
                {
                    HitObject();
                }
            }
            else
            {
                this.direction = BTWFunc.RandomCircleVector(BTWFunc.Random(BTWFunc.TileSize * this.damage));
                this.position += this.direction;
            }
        }
        

        if (ModManager.MSC)
        {
            if (this.lightingArc == null)
            {
                this.lightingArc = new LightingArc(this.position, this.lastPosition, 
                    this.damage / 2f, 0.5f + Mathf.Log10(1 + this.damage), this.lifetime, this.color);
                this.room.AddObject(this.lightingArc);
            }

            LightingArc arc = this.lightingArc as LightingArc;
            arc.fromOffset = this.position;
            arc.targetOffset = Vector2.Lerp(arc.targetOffset, this.lastPosition, 0.35f);
        }

        room.AddObject(new MouseSparkDrag(position, BTWFunc.RandomCircleVector(5f), 5f, color, 0.2f));
    }
    public override void Destroy()
    {
        if (BTWPlugin.meadowEnabled && this.meadowInit)
        {
            MeadowCalls.BTWItems_VoidSparkLeaveRoom(this);
        }
        base.Destroy();
        this.lightingArc?.Destroy();
        this.lightingArc = null;
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[2];

        sLeaser.sprites[0] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["LightSource"],
			scale = 4f,
            alpha = 0.5f,
            color = this.color
        };

        sLeaser.sprites[1] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["LightSource"],
			scale = 2f,
            alpha = 1f,
            color = this.color
        };

		this.AddToContainer(sLeaser, rCam, null);
    }
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
		rCam.ReturnFContainer("ForegroundLights").AddChild(sLeaser.sprites[0]);
		rCam.ReturnFContainer("ForegroundLights").AddChild(sLeaser.sprites[1]);
    }
    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        
    }
    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (this.slatedForDeletetion || this.room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
            return;
        }
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].x = this.position.x - camPos.x;
            sLeaser.sprites[i].y = this.position.y - camPos.y;
            sLeaser.sprites[i].color = this.color;
        }
        float fade = Mathf.Clamp01(this.lifetime / 5f);
        sLeaser.sprites[0].alpha = 0.5f * fade;
        sLeaser.sprites[1].alpha = fade;
    }


    public float damage;
    public bool chainReactionNotified = false;
    public int lifetime;
    public int ID = -1;
    public bool fake = false;
    public bool meadowInit = false;
    public Vector2 position = Vector2.zero;
    public Vector2 lastPosition = Vector2.zero;
    public Vector2 direction = Vector2.zero;
    public Color color = new(1f, 0.6f, 0.7f);
    public static Color defaultColor = new(1f, 0.6f, 0.7f);
    public UpdatableAndDeletable target;
    public UpdatableAndDeletable lightingArc;
    public AbstractCreature killTagHolder;
    public PhysicalObject source;
    public List<UpdatableAndDeletable> sparedList = new();

    public interface IReactToVoidFlux
    {
        void VoidSparkAppears(VoidSpark voidSpark);
        void HitByVoidSpark(VoidSpark voidSpark);
    }
}