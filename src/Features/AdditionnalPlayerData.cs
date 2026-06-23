using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace BeyondTheWest;

public class BTWPlayerData : AdditionnalTechManager<BTWPlayerData>
{
    public static void AddManager(AbstractCreature creature, out BTWPlayerData BTWPD)
    {
        BTWPD = new(creature);
        AddNewManager(creature, BTWPD);
    }
    public static void AddManager(AbstractCreature creature)
    {
        AddManager(creature, out _);
    }
    public BTWPlayerData(AbstractCreature abstractCreature) : base(abstractCreature)
    {
        this.local = BTWFunc.IsLocal(abstractCreature);
        if (BTWPlugin.meadowEnabled)
        {
            MeadowCalls.BTWPlayerData_Init(this);
        }
    }
    
    public override void Update()
    {
        base.Update();
        
        if (this.RealizedPlayer is Player player)
        {
            if (this.isSuperLaunchJump && (player.animation != Player.AnimationIndex.None || this.Landed))
            {
                isSuperLaunchJump = false;
            }

            if (player.dangerGrasp != null && player.dangerGraspTime < 30)
			{
				int playerNumber = BTWFunc.GetPlayerNumber(player);
				Player.InputPackage inputPackage = RWInput.PlayerInput(playerNumber);
				this.dangerGraspLastSpecButton = dangerGraspCurrentInput.spec;
                this.dangerGraspCurrentInput = inputPackage;
			}
            else
            {
                dangerGraspLastSpecButton = false;
            }
            
            if (this.dizzy > 0)
            {
                this.dizzy--;
                player.Blink(5);
                if (player.Local())
                {
                    if (this.dizzy == 0)
                    {
                        player.camoRechargePenalty = 0;
                    }
                    else
                    {
                        player.camoRechargePenalty = 5;
                    }
                }
            }
            if (this.onlineBlind > 0)
            {
                this.onlineBlind--;
                player.Blink(5);
            }
            if (this.exhausted > 0)
            {
                this.exhausted--;
                player.slowMovementStun = 5;
                player.Blink(5);
            }
        }
    }
    

    public bool isSuperLaunchJump = false;
    public bool dangerGraspLastSpecButton = false;
    public Player.InputPackage dangerGraspCurrentInput = new();
    public float slugHeight = defaultSize;
    public float SlugHeightRatio
    {
        get => slugHeight / defaultSize;
        set => slugHeight = defaultSize * value;
    }
    public float slugPupHeight = defaultPupSize;
    public float SlugPupHeightRatio
    {
        get => slugPupHeight / defaultPupSize;
        set => slugPupHeight = defaultPupSize * value;
    }
    public const float defaultSize = 17f;
    public const float defaultPupSize = 12f;
    public bool local = true;
    public int dizzy = 0;
    public int exhausted = 0;
    public List<SporeCloud> sporecloudsHit = new();
    public int onlineBlind = 0;
    public Vector2 bodySpritePos;
    public Vector2 hipsSpritePos;
    public Vector2 headSpritePos;
}
public static class BTWPlayerDataHooks
{
    public static void ApplyHooks()
    {
        IL.Player.ctor += Player_BTWPlayerData_Init; //So it starts first garanteed
        On.Player.Update += Player_BTWPlayerData_Update; 
        On.Player.Jump += Player_BTWPlayerData_OnJump;
        On.Player.ThrownSpear += Player_SpearingExhaust;
        IL.Player.ThrowObject += Player_WeaponExhaust;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites_GetBodySpritePos;
        IL.Player.MovementUpdate += Player_MovementUpdate_ModifyHeight;
        BTWPlugin.Log("BTWPlayerDataHooks ApplyHooks Done !");
    }

    private static float ChangeHeight(float orig, Player player)
    {
        if (player.GetBTWPlayerData() is BTWPlayerData bTWPlayerData)
        {
            bool pup = player.isSlugpup && player.playerState.isPup;
            if (pup 
                ? bTWPlayerData.slugPupHeight != BTWPlayerData.defaultPupSize 
                : bTWPlayerData.slugHeight != BTWPlayerData.defaultSize)
            {
                return orig * (pup ? bTWPlayerData.SlugPupHeightRatio : bTWPlayerData.SlugHeightRatio);
            }
        }
        return orig;
    }
    private static void Player_MovementUpdate_ModifyHeight(ILContext il)
    {
        BTWPlugin.Log("BTWPlayerData IL 3 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After, 
                x => x.MatchLdloc(4),
                x => x.MatchConvR4()))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ChangeHeight);
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
        BTWPlugin.Log("BTWPlayerData IL 3 ends");
    }

    private static void PlayerGraphics_DrawSprites_GetBodySpritePos(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.player.GetBTWPlayerData() is BTWPlayerData bTWPlayerData)
        {
            bTWPlayerData.bodySpritePos = sLeaser.sprites[0].GetPosition();
            bTWPlayerData.hipsSpritePos = sLeaser.sprites[1].GetPosition();
            bTWPlayerData.headSpritePos = sLeaser.sprites[3].GetPosition();
        }
    }

    public static bool IsExhausted(bool orig, Player player)
    {
        return orig || (player.GetBTWPlayerData() is BTWPlayerData bTWPlayerData && bTWPlayerData.exhausted > 0);
    }
    private static void Player_WeaponExhaust(ILContext il)
    {
        BTWPlugin.Log("BTWPlayerData IL 2 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,  
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.gourmandExhausted))))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(IsExhausted);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("BTWPlayerData IL 2 ends");
    }

    private static void Player_SpearingExhaust(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        orig(self, spear);
        if (self == null || self.room == null) { return; }
        if (spear == null || spear.bugSpear) { return; }
        if (self.GetBTWPlayerData() is BTWPlayerData bTWPlayerData && bTWPlayerData.exhausted > 0)
        {
            spear.spearDamageBonus = Mathf.Min(0.1f + self.slugcatStats.throwingSkill * 0.1f, spear.spearDamageBonus);
        }
    }
    private static void AddNewManager(AbstractCreature abstractPlayer)
    {
        if (!BTWPlayerData.TryGetManager(abstractPlayer, out _))
        {
            BTWPlayerData.AddManager(abstractPlayer);
            BTWPlugin.Log($"BTWPlayerData created for [{abstractPlayer}] class [{(abstractPlayer.realizedCreature as Player).SlugCatClass}]<{(abstractPlayer.realizedCreature as Player).IsTrailseeker()}><{(abstractPlayer.realizedCreature as Player).IsCore()}><{(abstractPlayer.realizedCreature as Player).IsSpark()}> !");
        }
    }
    private static void Player_BTWPlayerData_Init(ILContext il)
    {
        BTWPlugin.Log("BTWPlayerData IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            cursor.Goto(il.Body.Instructions.Count - 1, MoveType.After);
            if (cursor.TryGotoPrev(MoveType.Before,  x => x.MatchRet()))
            {
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(AddNewManager);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("BTWPlayerData IL 1 ends");
    }
    private static void Player_BTWPlayerData_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        self.GetBTWPlayerData()?.Update();
    }
    
    private static void Player_BTWPlayerData_OnJump(On.Player.orig_Jump orig, Player self)
    {
        int oldChargedJump = self.superLaunchJump;
        bool CanSuperJump = BTWFunc.CanSuperJump(self);

        orig(self);
        if (BTWPlayerData.TryGetManager(self.abstractCreature, out var BTWdata))
        {
            if (CanSuperJump && oldChargedJump >= 20 && self.superLaunchJump == 0 && self.simulateHoldJumpButton == 6)
            {
                BTWdata.isSuperLaunchJump = true;
                BTWPlugin.Log($"[{self}] did a super Jump !");
            }
        }
    }
}