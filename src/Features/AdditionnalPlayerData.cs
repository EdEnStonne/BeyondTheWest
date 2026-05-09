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
        if (Plugin.meadowEnabled)
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

            if (player.rollDirection == 0 
                && !(player.isSlugpup && player.playerState.isPup)
                && player.bodyChunkConnections[0].distance == 17f
                && slugHeight != 17f)
            {
                player.bodyChunkConnections[0].distance = slugHeight;
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
        }
    }
    

    public bool isSuperLaunchJump = false;
    public bool dangerGraspLastSpecButton = false;
    public Player.InputPackage dangerGraspCurrentInput = new();
    public float slugHeight = 17f;
    public bool local = true;
    public int dizzy = 0;
    public List<SporeCloud> sporecloudsHit = new();
    public int onlineBlind = 0;
}
public static class BTWPlayerDataHooks
{
    public static void ApplyHooks()
    {
        IL.Player.ctor += Player_BTWPlayerData_Init; //So it starts first garanteed
        On.Player.Update += Player_BTWPlayerData_Update; //Same here
        On.Player.Jump += Player_BTWPlayerData_OnJump;
        On.Player.ThrownSpear += Player_SpearingExhaust;
        IL.Player.ThrowObject += Player_WeaponExhaust;
        Plugin.Log("BTWPlayerDataHooks ApplyHooks Done !");
    }

    public static bool IsExhausted(bool orig, Player player)
    {
        return orig || (player.GetBTWPlayerData() is BTWPlayerData bTWPlayerData && bTWPlayerData.exhausted > 0);
    }
    private static void Player_WeaponExhaust(ILContext il)
    {
        Plugin.Log("BTWPlayerData IL 2 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
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
                Plugin.logger.LogError("Couldn't find IL hook :<");
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("BTWPlayerData IL 2 ends");
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
            Plugin.Log($"BTWPlayerData created for [{abstractPlayer}] class [{(abstractPlayer.realizedCreature as Player).SlugCatClass}]<{(abstractPlayer.realizedCreature as Player).IsTrailseeker()}><{(abstractPlayer.realizedCreature as Player).IsCore()}><{(abstractPlayer.realizedCreature as Player).IsSpark()}> !");
        }
    }
    private static void Player_BTWPlayerData_Init(ILContext il)
    {
        Plugin.Log("BTWPlayerData IL 1 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            cursor.Goto(il.Body.Instructions.Count - 1, MoveType.After);
            if (cursor.TryGotoPrev(MoveType.Before,  x => x.MatchRet()))
            {
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(AddNewManager);
            }
            else
            {
                Plugin.logger.LogError("Couldn't find IL hook :<");
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("BTWPlayerData IL 1 ends");
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
                Plugin.Log($"[{self}] did a super Jump !");
            }
        }
    }
}