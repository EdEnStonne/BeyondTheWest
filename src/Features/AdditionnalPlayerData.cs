using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;

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

            if (this.onlineDizzy > 0)
            {
                this.onlineDizzy--;
                player.Blink(5);
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
    public int onlineDizzy = 0;
    public int onlineBlind = 0;
}
public static class BTWPlayerDataHooks
{
    public static void ApplyHooks()
    {
        IL.Player.ctor += Player_BTWPlayerData_Init; //So it starts first garanteed
        On.Player.Update += Player_BTWPlayerData_Update; //Same here
        On.Player.Jump += Player_BTWPlayerData_OnJump;
        BTWPlugin.Log("BTWPlayerDataHooks ApplyHooks Done !");
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