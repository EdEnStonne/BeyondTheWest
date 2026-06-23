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
    public static bool TryGetShield(Player player, out ArenaShield shield)
    {
        return arenaShields.TryGetValue(player, out shield);
    }
    public static ArenaShield GetShield(Player player)
    {
        TryGetShield(player, out ArenaShield shield);
        return shield;
    }
    public static bool IsObjectIntangible(PhysicalObject physicalObject)
    {
        return IsObjectIntangible(physicalObject, out _);
    }
    public static bool IsObjectIntangible(PhysicalObject physicalObject, out ArenaShield shield)
    {
        shield = null;
        return physicalObject is Player player 
            && arenaShields.TryGetValue(player, out shield)
            && shield.Shielding;
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
        if (this.target != null && this.room != null)
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
        if (BTWPlugin.meadowEnabled && callForSync && this.isMine)
        {
            MeadowCalls.BTWArena_RPCArenaForcefieldDismiss(this);
        }
        if (BTWPlugin.meadowEnabled && this.target?.abstractCreature is AbstractCreature abstractPlayer)
        {
            MeadowFunc.ResetDeathMessage(abstractPlayer);
            MeadowFunc.ResetSlugcatIcon(abstractPlayer);
            BTWPlugin.Log($"Player [{abstractPlayer}] icon and death message was reset !");
        }
    }
    public override void Destroy()
    {
        base.Destroy();
        if (this.meadowInit && BTWPlugin.meadowEnabled && this.isMine)
        {
            MeadowCalls.BTWArena_ArenaShieldLeaveRoom(this);
        }
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
        if (!this.isInit)
        {
            Init();
            if (!this.isInit) return;
        }
        if (BTWPlugin.meadowEnabled && !this.meadowInit)
        {
            MeadowCalls.BTWArena_ArenaShieldEnterRoom(this);
        }
        if (this.target == null || this.target.slatedForDeletetion) 
        {  
            this.Destroy(); 
            return; 
        }
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
        sLeaser.sprites = new FSprite[this.circlesAmount];

        for (int i = 0; i < this.circlesAmount; i++)
        {
            sLeaser.sprites[i] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
                color = Color.gray,
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
        if (!this.isInit) return;
        if (this.target == null) { 
            sLeaser.CleanSpritesAndRemove(); 
            return; 
        }

        if (this.CreatureMainChunk != null)
        {
            this.pos = this.CreatureMainChunk.pos;
        }

        float easedLife = BTWFunc.EaseOut(1 - this.FractionLife, 4);
        // float easedDesc = BTWFunc.EaseIn(1 - this.FractionDestruct, 2);

        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.x = pos.x - camPos.x;
            sprite.y = pos.y - camPos.y;
            sprite.alpha = 0f;
        }

        for (int i = 0; i < this.circlesAmount; i++)
        {
            sLeaser.sprites[i].x += Mathf.Sin(((float)(i+1) / this.circlesAmount) * Mathf.PI * 2f) * 35f;
            sLeaser.sprites[i].y += Mathf.Cos(((float)(i+1) / this.circlesAmount) * Mathf.PI * 2f) * 35f;
            sLeaser.sprites[i].color = Color.Lerp(Color.white, Color.black, 0.75f - easedLife);
            sLeaser.sprites[i].alpha = 0.4f + 0.6f * (1 - GetCircleFraction(i+1));
            sLeaser.sprites[i].scale = 0.45f * BTWFunc.EaseOut(GetCircleFraction(i+1), 3);
            if (!this.isMine)
            {
                sLeaser.sprites[i].color = Color.Lerp(sLeaser.sprites[i].color, Color.black, 0.35f);
            }
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
    public bool meadowInit = false;

    public Player target;
    public Color baseColor = Color.white;
    public FShader oldPlayerShader;
    public static FShader shieldPlayerShader;
    public int life = 0;
    private int circlesAmount = 0;
    public int shieldTime = BTWFunc.FrameRate * 10;
    public int destruction = 0;
    public bool isMine = true;
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

internal static class ArenaShieldHooks
{
    public static void ApplyHooks()
    {
        On.Player.checkInput += Player_checkInput_StopThrowWhenShielded;
        On.Weapon.HitThisObject += Weapon_HitThisObject_DontHitShieldedPlayers;
        IL.Room.Update += Room_Update_DisableCollisionWithShieldedPlayers;
        On.Creature.Violence += Creature_Violence_DisableViolenceWithShieldedPlayers;
        On.Creature.Die += Creature_Die_DisableViolenceWithShieldedPlayers;
        IL.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer_StopStunningShieldedSlugs;
        IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint_StopAscendingShieldedPlayers;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites_MakePlayerCoolWhenShielded;
        IL.Explosion.Update += Explosion_Update_DontExplodeIntangliblePlayers;

        BTWPlugin.Log("ArenaShieldHooks ApplyHooks Done !");
    }
    
    public static void LoadResources(RainWorld rainWorld)
    {
        ArenaShield.shieldPlayerShader = rainWorld.Shaders["Hologram"];

        BTWPlugin.Log("ArenaShieldHooks LoadResources Done !");
    }
    
    private static bool ShouldNotExplode(Explosion explosion, int collisionLayer, int indexCreature)
    {
        PhysicalObject victim = explosion.room.physicalObjects[collisionLayer][indexCreature];
        return ArenaShield.IsObjectIntangible(explosion.sourceObject) || ArenaShield.IsObjectIntangible(victim);
    }
    private static void Explosion_Update_DontExplodeIntangliblePlayers(ILContext il)
    {
        BTWPlugin.Log("ArenaShieldHooks IL 4 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            ILLabel label = cursor.DefineLabel();
            if (cursor.TryGotoNext(MoveType.After, 
                x => x.MatchCallvirt(typeof(UpdatableAndDeletable).GetProperty(nameof(UpdatableAndDeletable.slatedForDeletetion)).GetGetMethod()),
                x => x.MatchBrtrue(out label)
            )) 
            {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_2);
                cursor.Emit(OpCodes.Ldloc_3);
                cursor.EmitDelegate(ShouldNotExplode);
                cursor.Emit(OpCodes.Brtrue, label);
            }
            else
            {
                BTWPlugin.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.LogError(ex);
        }
        BTWPlugin.Log("ArenaShieldHooks IL 4 ends");
    }

    private static void PlayerGraphics_DrawSprites_MakePlayerCoolWhenShielded(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (ArenaShield.arenaShields.TryGetValue(self.player, out var shield))
        {
            for (int i = 0; i <= 9; i++)
            {
                if (shield.Shielding && sLeaser.sprites[i].shader != ArenaShield.shieldPlayerShader)
                {
                    shield.oldPlayerShader = sLeaser.sprites[i].shader;
                    sLeaser.sprites[i].shader = ArenaShield.shieldPlayerShader;
                }
                else if (!shield.Shielding && sLeaser.sprites[i].shader == ArenaShield.shieldPlayerShader)
                {
                    sLeaser.sprites[i].shader = shield.oldPlayerShader;
                }
            }
        }
    }

    private static bool ShouldNotAscend(Player player, PhysicalObject victim)
    {
        return ArenaShield.IsObjectIntangible(player) || ArenaShield.IsObjectIntangible(victim);
    }
    private static void Player_ClassMechanicsSaint_StopAscendingShieldedPlayers(ILContext il)
    {
        BTWPlugin.Log("ArenaShieldHooks IL 3 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            ILLabel label = cursor.DefineLabel();
            if (cursor.TryGotoNext(MoveType.After, 
                x => x.MatchLdloc(18), 
                x => x.MatchIsinst<Creature>(), 
                x => x.MatchCallvirt<Creature>(nameof(Creature.Die))))
            {
                cursor.MarkLabel(label);
                cursor.GotoPrev(MoveType.Before, 
                    x => x.MatchLdloc(18), 
                    x => x.MatchIsinst<Creature>(), 
                    x => x.MatchCallvirt<Creature>(nameof(Creature.Die)));
                
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 18);
                cursor.EmitDelegate(ShouldNotAscend);

                cursor.Emit(OpCodes.Brfalse, cursor.Next);
                cursor.Emit(OpCodes.Ldc_I4_0);
                cursor.Emit(OpCodes.Stloc, 15);
                cursor.Emit(OpCodes.Br, label);
            }
            else
            {
                BTWPlugin.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.LogError(ex);
        }
        BTWPlugin.Log("ArenaShieldHooks IL 3 ends");
    }

    private static bool ShouldNotStun(bool orig, Player player, int collisionLayer, int indexCreature)
    {
        PhysicalObject victim = player.room.physicalObjects[collisionLayer][indexCreature];
        if (ArenaShield.IsObjectIntangible(player) || ArenaShield.IsObjectIntangible(victim))
        {
            return false;
        }
        return orig;
    }
    private static void Player_ClassMechanicsArtificer_StopStunningShieldedSlugs(ILContext il)
    {
        BTWPlugin.Log("ArenaShieldHooks IL 2 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before, x => x.MatchStloc(18)))
            {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 16);
                cursor.Emit(OpCodes.Ldloc, 17);
                cursor.EmitDelegate(ShouldNotStun);
            }
            else
            {
                BTWPlugin.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.LogError(ex);
        }
        BTWPlugin.Log("ArenaShieldHooks IL 2 ends");
    }

    private static void Creature_Die_DisableViolenceWithShieldedPlayers(On.Creature.orig_Die orig, Creature self)
    {
        if (ArenaShield.IsObjectIntangible(self) || ArenaShield.IsObjectIntangible(self.killTag?.realizedCreature))
        {
            return;
        }
        orig(self);
    }

    private static void Creature_Violence_DisableViolenceWithShieldedPlayers(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (ArenaShield.IsObjectIntangible(self) 
            || ArenaShield.IsObjectIntangible(source?.owner)
            || ArenaShield.IsObjectIntangible(self.killTag?.realizedCreature))
        {
            return;
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }

    private static bool ShouldDisableCollision(bool orig, Room room, int collisionLayer, int indexCreature1, int indexCreature2)
    {
        PhysicalObject obj1 = room.physicalObjects[collisionLayer][indexCreature1];
        PhysicalObject obj2 = room.physicalObjects[collisionLayer][indexCreature2];
        if (ArenaShield.IsObjectIntangible(obj1) || ArenaShield.IsObjectIntangible(obj2))
        {
            return true;
        }
        return orig;
    }
    private static void Room_Update_DisableCollisionWithShieldedPlayers(ILContext il)
    {
        BTWPlugin.Log("ArenaShieldHooks IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before, x => x.MatchStloc(22)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 18);
                cursor.Emit(OpCodes.Ldloc, 19);
                cursor.Emit(OpCodes.Ldloc, 20);
                cursor.EmitDelegate(ShouldDisableCollision);
            }
            else
            {
                BTWPlugin.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.LogError(ex);
        }
        BTWPlugin.Log("ArenaShieldHooks IL 1 ends");
    }

    private static bool Weapon_HitThisObject_DontHitShieldedPlayers(On.Weapon.orig_HitThisObject orig, Weapon self, PhysicalObject obj)
    {
        return orig(self, obj) && !ArenaShield.IsObjectIntangible(obj);
    }

    private static void Player_checkInput_StopThrowWhenShielded(On.Player.orig_checkInput orig, Player self)
    {
        orig(self);
        if (ArenaShield.arenaShields.TryGetValue(self, out var arenaShield) && arenaShield.Shielding)
        {
            self.input[0].thrw = false;
        }
    }
}