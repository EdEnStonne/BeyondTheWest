using System;
using BeyondTheWest;
using UnityEngine;
using RWCustom;
using HUD;
using BeyondTheWest.MeadowCompat;

namespace BeyondTheWest.ArenaAddition;
public class ArenaForcedDeath : UpdatableAndDeletable, IDrawable
{
    public ArenaForcedDeath(AbstractCreature abstractCreature, int killingTime, bool stunlockCreature, AbstractCreature killTagHolder, bool fake = false) 
    {
        this.stunlockCreature = stunlockCreature;
        this.killTagHolder = killTagHolder;
        this.killingTime = killingTime;
        this.abstractTarget = abstractCreature;
        this.fake = fake;
        if (this.target != null)
        {
            if (this.target is Player player)
            {
                this.baseColor = player.ShortCutColor();
            }
            if (this.CreatureMainChunk != null)
            {
                this.pos = this.CreatureMainChunk.pos;
            }
        }
        if (!fake && BTWPlugin.meadowEnabled && MeadowFunc.IsMine(abstractCreature) && MeadowFunc.IsMeadowLobby())
        {
            MeadowCalls.BTWArena_RPCArenaForcedDeathEffect(this);
        }
    }
    public ArenaForcedDeath(AbstractCreature abstractCreature, int killingTime, bool fake = false) 
        : this(abstractCreature, killingTime, true, null, fake) { }
    public ArenaForcedDeath(AbstractCreature abstractCreature, bool fake = false)
        : this(abstractCreature, BTWFunc.FrameRate * 1, true, null, fake) { }
    
    

    public void CancelKill()
    {
        this.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, CreatureMainChunk, false, 0.35f, 0.35f + BTWFunc.random * 0.35f);
        this.jobDone = true;
    }
    public void TriggerKillScene()
    {
        if (CreatureStillValid)
        {
            Creature creature = this.target;
            BodyChunk body = CreatureMainChunk;
            this.room.PlaySound(SoundID.Firecracker_Bang, body, false, 0.5f, 0.75f + BTWFunc.random);
            this.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, body, false, 0.5f, 0.4f + BTWFunc.random * 0.35f);
            for (int i = (int)(BTWFunc.random * 10) + 10; i > 0; i--)
            {
                this.room.AddObject( new WaterDrip(body.pos, BTWFunc.RandomCircleVector(20f), false) );
            }
            if (!fake)
            {
                if (DoesKnockback)
                {
                    BTWFunc.CustomKnockback(creature, BTWFunc.RandomCircleVector(20f));
                }
                if (this.killTagHolder != null) { creature.SetKillTag(this.killTagHolder); }
                if (BTWPlugin.meadowEnabled)
                {
                    ArenaDeathTracker.SetDeathTrackerOfCreature(creature.abstractCreature, 40, true);
                }
                creature.Die();
            }

            this.jobDone = true;
        }
    }
    public void StunCreature()
    {
        if (CreatureStillValid && !fake)
        {
            Creature creature = this.target;
            creature.LoseAllGrasps();
            creature.Stun(20);
        }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.abstractTarget == null) { this.Destroy(); return; }
        if (CreatureStillValid && !this.jobDone)
        {
            this.destruction = 0;
            this.pos = this.CreatureMainChunk.pos;
            if (this.room != this.target.room)
            {
                this.RemoveFromRoom();
                this.target.room.AddObject( this );
            }
            if (this.FractionLife >= 1)
            {
                this.TriggerKillScene();
            }
            if (this.stunlockCreature)
            {
                StunCreature();
            }
            this.life++;
        }
        else
        {
            if (this.destruction == 0 && !this.jobDone)
            {
                CancelKill();
            }
            if (this.FractionDestruct >= 1)
            {
                this.Destroy();
                return;
            }
            this.destruction++;
        }
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[2];

        FSprite karmaSprite = new FSprite(KarmaMeter.KarmaSymbolSprite(false, new IntVector2(9, 9)), true);
        karmaSprite.color = this.baseColor;
        karmaSprite.alpha = 0f;
        sLeaser.sprites[1] = karmaSprite;

        sLeaser.sprites[0] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["FlatLight"],
            alpha = 0f,
            color = this.baseColor
        };

        this.AddToContainer(sLeaser, rCam, null);
    }
    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
        {
            sLeaser.CleanSpritesAndRemove();
            return;
        }
        if (this.abstractTarget == null) { sLeaser.CleanSpritesAndRemove(); return; }

        if (this.CreatureMainChunk != null && !this.jobDone)
        {
            this.pos = this.CreatureMainChunk.pos;
        }

        float easedLife = BTWFunc.EaseOut(1 - this.FractionLife, 4);
        float easedDesc = BTWFunc.EaseIn(1 - this.FractionDestruct, 2);
        Vector2 shakeFactor = this.destruction > 0 ? Vector2.zero : BTWFunc.RandomCircleVector(2.5f) * this.FractionLife;

        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.x = pos.x - camPos.x + shakeFactor.x;
            sprite.y = pos.y - camPos.y + shakeFactor.y;
            sprite.alpha = 0f;
        }

        sLeaser.sprites[1].scale = (0.5f + easedLife * 1f) * easedDesc;
        sLeaser.sprites[1].alpha = easedDesc;
        sLeaser.sprites[1].color = Color.Lerp(this.baseColor, Color.red, this.FractionLife);
        
        sLeaser.sprites[0].scale = easedLife * 15f;
        sLeaser.sprites[0].alpha = this.FractionLife;
        if (this.destruction > 0)
        {
            if (this.jobDone)
            {
                sLeaser.sprites[1].alpha /= 2;
                sLeaser.sprites[0].alpha = easedDesc;
            }
            else
            {
                sLeaser.sprites[1].alpha /= 4;
                sLeaser.sprites[0].alpha = 0f;
            }
        }
        if (this.target != null && this.target.inShortcut)
        {
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.alpha = 0f;
            }
        }
    }
    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        foreach (FSprite sprite in sLeaser.sprites)
        {
            rCam.ReturnFContainer("HUD").AddChild(sprite);
        }
    }

    public AbstractCreature abstractTarget;
    public AbstractCreature killTagHolder;
    public Color baseColor = Color.white;
    public int life = 0;
    public int killingTime = BTWFunc.FrameRate * 1;
    public int destruction = 0;
    private const int destructTime = BTWFunc.FrameRate * 3;
    public bool stunlockCreature = true;
    private bool jobDone = false;
    public bool fake = false;
    public Vector2 pos;

    public Creature target
    {
        get
        {
            return abstractTarget?.realizedCreature;
        }
    }
    public float FractionLife
    {
        get
        {
            return Mathf.Clamp01((float)life / killingTime);
        }
    }
    public float FractionDestruct
    {
        get
        {
            return Mathf.Clamp01((float)destruction / destructTime);
        }
    }
    public BodyChunk CreatureMainChunk
    {
        get
        {
            if (this.room != null && this.target != null && this.target.room != null) 
                { return this.target.mainBodyChunk ?? this.target.firstChunk; }
            return null;
        }
    }
    public bool CreatureStillValid
    {
        get
        {
            return this.room != null && this.target != null && this.target.room != null && !this.target.dead;
        }
    }
    public bool DoesKnockback
    {
        get
        {
            if (this.room != null && this.target != null && this.target.room != null)
            {
                if (this.target.grabbedBy != null && this.target.grabbedBy.Count > 0)
                {
                    return false;
                }
                if (this.target is Player player && player.onBack != null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}