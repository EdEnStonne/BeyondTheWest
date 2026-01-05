using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;

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
        
        Player player = this.RealizedPlayer;
        if (player != null)
        {
            if (this.isSuperLaunchJump && (player.animation != Player.AnimationIndex.None || this.Landed))
            {
                isSuperLaunchJump = false;
            }
        }
    }
    

    public bool isSuperLaunchJump = false;
    public bool local = true;
}
public static class BTWPlayerDataHooks
{
    public static void ApplyHooks()
    {
        On.Player.ctor += Player_BTWPlayerData_Init;
        On.Player.Update += Player_BTWPlayerData_Update;
        On.Player.Jump += Player_BTWPlayerData_OnJump;
        BTWPlugin.Log("PoleKickManagerHooks ApplyHooks Done !");
    }

    private static void Player_BTWPlayerData_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
    }

    private static void Player_BTWPlayerData_Init(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (!BTWPlayerData.TryGetManager(self.abstractCreature, out _))
        {
            BTWPlayerData.AddManager(abstractCreature);
            BTWPlugin.Log($"BTWPlayerData created for [{abstractCreature}] !");
        }
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