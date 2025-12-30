using System;
using BeyondTheWest;
using UnityEngine;
using RWCustom;
using HUD;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using UnityEngine.Assertions.Must;
using System.Linq;
using BeyondTheWest.MeadowCompat;

namespace BeyondTheWest.ArenaAddition;

public class ArenaShield : UpdatableAndDeletable, IDrawable
{
    public static ConditionalWeakTable<Player, ArenaShield> arenaShields = new();
    public static ConditionalWeakTable<AbstractCreature, ArenaShield> shieldToAdd = new();
    public static bool TryGetShield(Player player, out ArenaShield shield)
    {
        return arenaShields.TryGetValue(player, out shield);
    }
    public static ArenaShield GetShield(Player player)
    {
        TryGetShield(player, out ArenaShield shield);
        return shield;
    }

    public ArenaShield(int shieldTime)
    {
        this.shieldTime = shieldTime;
    }
    public ArenaShield(Player player, int shieldTime) : this(shieldTime)
    {
        this.target = player;
        Init();
    }
    public ArenaShield(Player player) : this(player, BTWFunc.FrameRate * 10) { }
    public ArenaShield() : this(BTWFunc.FrameRate * 10) {}
    
    public void Init()
    {
        if (this.target != null)
        {
            this.isInit = true;
            this.baseColor = this.target.ShortCutColor();
            if (this.CreatureMainChunk != null)
            {
                this.pos = this.CreatureMainChunk.pos;
            }
            if (TryGetShield(this.target, out var arenaShield))
            {
                arenaShield.Destroy();
            }
            arenaShields.Add(this.target, this);
            if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowLobby())
            {
                this.isMine = BTWFunc.IsLocal(this.target.abstractCreature);
                this.meadowSync = true;
                if (this.isMine)
                {
                    MeadowCalls.BTWArena_RPCArenaForcefieldAdded(this);
                }
            }
        }
    }
    public void Block(bool callForSync = true, bool fake = false)
    {
        if (this.CreatureMainChunk != null && this.room != null && this.blockAnim <= 0)
        {
            this.blockAnim = blockAnimMax;
            this.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, CreatureMainChunk, false, 0.65f, 2.5f + BTWFunc.random * 0.5f);
            if (!fake)
            {
                this.life += this.shieldTime/8; 
            }
        }
        if (BTWPlugin.meadowEnabled && callForSync && !fake && this.meadowSync)
        {
            MeadowCalls.BTWArena_RPCArenaForcefieldBlock(this);
        }
    }
    public void Dismiss(bool callForSync = true)
    {
        if (this.CreatureMainChunk != null && this.room != null)
        {
            if (this.destruction <= 0 || this.Shielding)
            {
                this.room.PlaySound(SoundID.HUD_Pause_Game, this.CreatureMainChunk, false, 0.75f, 0.4f + BTWFunc.random * 0.2f);
            }
            this.life = this.shieldTime;
        }
        if (BTWPlugin.meadowEnabled && callForSync && this.meadowSync && this.isMine)
        {
            MeadowCalls.BTWArena_RPCArenaForcefieldDismiss(this);
        }
    }
    public override void Destroy()
    {
        base.Destroy();
        if (this.target != null)
        {
            Dismiss();
            if (ArenaShield.TryGetShield(this.target, out _))
            {
                arenaShields.Remove(this.target);
            }
        }
    }
    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.target == null) 
        {  
            if (this.isInit)
            {
                this.Destroy(); 
            }
            else
            {
                Init();
            }
            return; 
        }
        if (this.blockAnim > 0) { this.blockAnim--; }
        if (CreatureStillValid && this.Shielding)
        {
            if (this.CreatureMainChunk != null)
            {
                this.pos = this.CreatureMainChunk.pos;
            }
            if (this.target.room != null && this.room != this.target.room)
            {
                this.RemoveFromRoom();
                this.target.room.AddObject( this );
            }
            if (this.FractionLife == 0)
            {
                Block(false, true);
                this.life = 0;
            }
            this.life++;
        }
        else
        {
            if (this.destruction == 0)
            {
                Dismiss();
            }
            if (this.FractionDestruct >= 1)
            {
                this.Destroy();
                return;
            }
            this.destruction++;
        }
    }

    public float GetCircleFraction(int circle)
    {
        float timePerCircle = (float)this.shieldTime / this.circlesAmount;
        return 1 - Mathf.Clamp01((this.life - timePerCircle * (circle - 1)) / timePerCircle);
    }
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        this.circlesAmount = Mathf.Clamp(this.shieldTime / BTWFunc.FrameRate, 6, 20);
        sLeaser.sprites = new FSprite[this.circlesAmount + 1];

        FSprite Shield = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
            color = this.baseColor,
            alpha = 0f,
            scale = 5f
        };
        sLeaser.sprites[0] = Shield;

        for (int i = 1; i <= this.circlesAmount; i++)
        {
            sLeaser.sprites[i] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
                color = Color.white,
                alpha = 0f,
                scale = 1.0f
            };
        }

        this.AddToContainer(sLeaser, rCam, null);
    }
    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
        {
            if (this.isInit)
            {
                sLeaser.CleanSpritesAndRemove();
            }
            else
            {
                foreach (FSprite sprite in sLeaser.sprites)
                {
                    sprite.alpha = 0f;
                }
            }
            return;
        }
        if (this.target == null) { 
            sLeaser.CleanSpritesAndRemove(); 
            return; 
        }

        if (this.CreatureMainChunk != null)
        {
            this.pos = this.CreatureMainChunk.pos;
        }

        float easedLife = BTWFunc.EaseOut(1 - this.FractionLife, 4);
        float easedDesc = BTWFunc.EaseIn(1 - this.FractionDestruct, 2);

        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.x = pos.x - camPos.x;
            sprite.y = pos.y - camPos.y;
            sprite.alpha = 0f;
        }

        sLeaser.sprites[0].scale = 6.5f - BTWFunc.EaseOut(this.FractionBlock) * 0.5f;
        sLeaser.sprites[0].alpha = 0.05f + BTWFunc.EaseOut(this.FractionBlock) * 0.1f;

        for (int i = 1; i <= this.circlesAmount; i++)
        {
            sLeaser.sprites[i].x += Mathf.Sin(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 35f;
            sLeaser.sprites[i].y += Mathf.Cos(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 35f;
            sLeaser.sprites[i].color = Color.white;
            sLeaser.sprites[i].alpha = 0.4f + 0.6f * (1 - GetCircleFraction(i));
            sLeaser.sprites[i].scale = 0.45f * BTWFunc.EaseOut(GetCircleFraction(i), 3);
        }

        if (this.destruction > 0)
        {
            sLeaser.sprites[0].alpha = easedDesc * 0.05f;
        }
        if (this.target != null && (this.target.inShortcut || this.target.room == null))
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

    private bool isInit = false;

    public Player target;
    public Color baseColor = Color.white;
    public int life = 0;
    private int circlesAmount = 0;
    public int shieldTime = BTWFunc.FrameRate * 10;
    public int destruction = 0;
    public int blockAnim = 0;
    private bool isMine = true;
    private bool meadowSync = false;
    private const int blockAnimMax = 30;
    private const int destructTime = BTWFunc.FrameRate * 1;
    public Vector2 pos;

    public float FractionLife
    {
        get
        {
            return Mathf.Clamp01((float)life / shieldTime);
        }
    }
    public float FractionDestruct
    {
        get
        {
            return Mathf.Clamp01((float)destruction / destructTime);
        }
    }
    public float FractionBlock
    {
        get
        {
            return Mathf.Clamp01((float)blockAnim / blockAnimMax);
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
            return this.room != null && this.target != null && !this.target.dead;
        }
    }
    public bool Shielding
    {
        get
        {
            return this.room != null && FractionLife < 1 && CreatureStillValid;
        }
    }
}

public static class ArenaShieldHooks
{
    public static void ApplyHooks()
    {
        IL.Weapon.Update += Weapon_BlockWithArenaShield;
        On.Creature.Violence += Player_BlockViolence;
        On.Creature.Die += Player_BlockLiteralDeath;
        On.Creature.Grab += Player_RemoveShieldOnGrabItem;
        On.Player.ctor += Player_AddQueuedShield;
        On.Player.ThrowObject += Player_RemoveShieldOnThrowObject;
        On.Creature.Violence += Player_RemoveShieldOnViolence;
        BTWPlugin.Log("CompetitiveAddition ApplyHooks Done !");
    }


    public static bool OutOfBounds(Creature creature)
    {
        float num6 = -creature.bodyChunks[0].restrictInRoomRange + 1f;
        if (creature is Player player 
            && creature.bodyChunks[0].restrictInRoomRange == creature.bodyChunks[0].defaultRestrictInRoomRange)
        {
            if (player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                num6 = Mathf.Max(num6, -250f);
            }
            else
            {
                num6 = Mathf.Max(num6, -500f);
            }
        }
        return creature.bodyChunks[0].pos.y < num6 
            && (!creature.room.water 
                || creature.room.waterInverted 
                || creature.room.defaultWaterLevel < -10) 
            && (!creature.Template.canFly 
                || creature.Stunned 
                || creature.dead) 
            && (creature is Player 
                || !creature.room.game.IsArenaSession 
                || creature.room.game.GetArenaGameSession.chMeta == null 
                || !creature.room.game.GetArenaGameSession.chMeta.oobProtect);
    }
    public static bool DoesBlock(Weapon weapon, SharedPhysics.CollisionResult result)
    {
        if (weapon != null && result.obj != null 
            && result.obj is Player player && player != null
            && ArenaShield.TryGetShield(player, out var shield) 
            && shield.Shielding)
        {
            BTWPlugin.Log("["+ weapon +"] BLOCKED BY SHIELD OF PLAYER ["+ player +"]");

            shield.Block();
            Vector2 inbetweenPos = Vector2.Lerp(result.obj.firstChunk.lastPos, weapon.firstChunk.lastPos, 0.5f);
            Vector2 dir = (weapon.firstChunk.lastPos - result.obj.firstChunk.lastPos).normalized * 2f + BTWFunc.RandomCircleVector() + Vector2.up * 0.5f;
            
            weapon.WeaponDeflect(inbetweenPos, dir.normalized, 50f);
            return true;
        }
        return false;
    }
    // Hooks  
    private static void Player_AddQueuedShield(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (ArenaShield.shieldToAdd.TryGetValue(abstractCreature, out var shield))
        {
            shield.target = self;
            shield.Init();
            self.room.AddObject( shield );
            ArenaShield.shieldToAdd.Remove(abstractCreature);
            BTWPlugin.Log($"Spared shield added to [{self}] ! Can the shield be found ? <{ArenaShield.TryGetShield(self, out _)}>");
        }
    }
    private static void Player_RemoveShieldOnThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        if (ArenaShield.TryGetShield(self, out var shield) 
            && shield.Shielding)
        {
            BTWPlugin.Log("REMOVED SHIELD OF PLAYER ["+ self +"]. Reason : item throw.");
            shield.Dismiss();
        }
        orig(self, grasp, eu);
    }
    private static bool Player_RemoveShieldOnGrabItem(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
    {
        if (self is Player player && player != null
            && ArenaShield.TryGetShield(player, out var shield) 
            && shield.Shielding)
        {
            BTWPlugin.Log("REMOVED SHIELD OF PLAYER ["+ player +"]. Reason : item grab.");
            shield.Dismiss();
        }
        return orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
    }
    private static void Player_BlockLiteralDeath(On.Creature.orig_Die orig, Creature self)
    {
        if (self is Player player && player != null
            && ArenaShield.TryGetShield(player, out var shield) 
            && shield.Shielding)
        {
            if (!OutOfBounds(self))
            {
                BTWPlugin.Log("DEATH BLOCKED BY SHIELD OF PLAYER ["+ player +"]");

                shield.Block();
                player.Stun(BTWFunc.FrameRate * 1);
                if (player.State is HealthState)
                {
                    (player.State as HealthState).health = 1f;
                }
                if (player.airInLungs < 1f)
                {
                    player.airInLungs = 1f;
                }
                if (player.Hypothermia > 0f)
                {
                    player.Hypothermia = 0f;
                }
                if (player.grabbedBy != null && player.grabbedBy.Count > 0)
                {
                    List<Creature.Grasp> grasps = new(player.grabbedBy);
                    foreach (Creature.Grasp grasp in grasps)
                    {
                        grasp.grabber.Stun(BTWFunc.FrameRate * 3);
                        grasp.Release();
                    }
                }
                if (player.injectedPoison > 0f)
                {
                    player.injectedPoison = 0f;
                }
                return;
            }
            shield.Dismiss();
        }
        orig(self);
    }
    private static void Weapon_BlockWithArenaShield(ILContext il)
    {
        BTWPlugin.Log("Weapon PassThrough IL starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStfld<Weapon>("floorBounceFrames"),
                x => x.MatchRet()
                )
            )
            {
                cursor.MoveAfterLabels();

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_S, (byte)16);
                cursor.EmitDelegate(DoesBlock);
                Instruction Mark = cursor.Previous;

                cursor.Emit(OpCodes.Ldloca_S, (byte)16);
                cursor.Emit(OpCodes.Ldnull);
                cursor.Emit(OpCodes.Stfld, typeof(SharedPhysics.CollisionResult).GetField("obj"));
                Instruction Mark2 = cursor.Next;

                if (cursor.TryGotoPrev(MoveType.After, x => x == Mark))
                {
                    cursor.Emit(OpCodes.Brfalse_S, Mark2);
                }
                else { BTWPlugin.logger.LogError("Couldn't find IL hook 2 :<"); }
            }
            else { BTWPlugin.logger.LogError("Couldn't find IL hook 1 :<"); }

            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        // Plugin.Log(il);
        BTWPlugin.Log("Weapon PassThrough IL ends");
    }
    private static void Player_BlockViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player player && player != null
            && ArenaShield.TryGetShield(player, out var shield) 
            && shield.Shielding)
        {
            BTWPlugin.Log("VIOLENCE BLOCKED BY SHIELD OF PLAYER ["+ player +"]");

            shield.Block();
            if (source?.owner != null && source.owner is Creature creature)
            {
                creature.Stun(3 * BTWFunc.FrameRate);
                Vector2 dir = (source.lastPos - (hitChunk ?? self.firstChunk).lastPos).normalized * 3f + BTWFunc.RandomCircleVector() + Vector2.up;
                
                BTWFunc.CustomKnockback(creature, dir.normalized, 30f, true);
            }
            return;
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type , damage, stunBonus);
    }
    private static void Player_RemoveShieldOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (source?.owner != null
            && source?.owner is Player player && player != null
            && ArenaShield.TryGetShield(player, out var shield) 
            && shield.Shielding)
        {
            BTWPlugin.Log("REMOVED SHIELD OF PLAYER ["+ player +"]. Reason : violence.");

            shield.Dismiss();
            Vector2 dir = (source.lastPos - (hitChunk ?? self.firstChunk).lastPos).normalized * 3f + BTWFunc.RandomCircleVector() + Vector2.up;
            BTWFunc.CustomKnockback(player, dir.normalized, 20f, true);
            player.Stun((int)stunBonus);
            player.gourmandAttackNegateTime = player.stun;
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type , 0f, stunBonus);
            return;
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type , damage, stunBonus);
    }
}