using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;

namespace BeyondTheWest;
public class ModifiedTechManager : AdditionnalTechManager<ModifiedTechManager>
{
    public static void AddManager(AbstractCreature creature, out ModifiedTechManager MTM)
    {
        MTM = new(creature);
        AddNewManager(creature, MTM);
    }
    public static void AddManager(AbstractCreature creature)
    {
        AddManager(creature, out _);
    }

    public ModifiedTechManager(AbstractCreature abstractCreature) : base(abstractCreature)
    {
        if (Plugin.meadowEnabled)
        {
            MeadowCalls.ModifiedTech_Init(this);
        }
        
    }

    public void ApplyJumpTechBoost(Player.AnimationIndex oldAnim, Player.BodyModeIndex oldBMode)
    {
        Player player = this.RealizedPlayer;
        if (player != null)
        {
            if (player.animation == Player.AnimationIndex.Flip && !player.flipFromSlide)
            {
                for (int i = player.bodyChunks.Length - 1; i >= 0; i--)
                {
                    player.bodyChunks[i].vel.x *= this.flipScalarVel.x;
                    player.bodyChunks[i].vel.y *= this.flipScalarVel.y;
                }
            }
            else if (player.animation == Player.AnimationIndex.RocketJump && (oldAnim == Player.AnimationIndex.Roll || oldAnim == Player.AnimationIndex.BellySlide))
            {
                for (int i = player.bodyChunks.Length - 1; i >= 0; i--)
                {
                    player.bodyChunks[i].vel.x *= this.rocketJumpScalarVel.x;
                    player.bodyChunks[i].vel.y *= this.rocketJumpScalarVel.y;
                }
            }
            else if (oldBMode != Player.BodyModeIndex.Stand && oldBMode != Player.BodyModeIndex.ClimbingOnBeam && oldBMode != Player.BodyModeIndex.WallClimb)
            {
                for (int i = player.bodyChunks.Length - 1; i >= 0; i--)
                {
                    player.bodyChunks[i].vel.x *= this.othersScalarVel.x;
                    player.bodyChunks[i].vel.y *= this.othersScalarVel.y;
                }
            }
        }
    }
    public void ApplyJumpTechBoost()
    {
        ApplyJumpTechBoost(Player.AnimationIndex.None, Player.BodyModeIndex.Default);
    }
    // ------ Variables

    // Objects
    // Basic
    public bool techEnabled = true;
    public int poleBonus = 4;
    public Vector2 flipScalarVel = new(0.5f, 1.35f);
    public Vector2 rocketJumpScalarVel = new(1.60f, 0.85f);
    public Vector2 othersScalarVel = new(1.25f, 0.90f);

    // Get - Set
}
public static class ModifiedTechHooks
{
    public static void ApplyHooks()
    {
        On.Player.Jump += Player_ModifiedTech_Jump;
        Plugin.Log("ModifiedTechHooks ApplyHooks Done !");
    }
    private static void Player_ModifiedTech_Jump(On.Player.orig_Jump orig, Player self)
    {
        Player.AnimationIndex oldAnim = self.animation;
        Player.BodyModeIndex oldBMode = self.bodyMode;
        int oldSlideUpPoles = self.slideUpPole;

        orig(self);

        if (ModifiedTechManager.TryGetManager(self.abstractCreature, out var modifiedTech) && BTWFunc.IsLocal(self))
        {
            if (modifiedTech.techEnabled)
            {
                modifiedTech.ApplyJumpTechBoost(oldAnim, oldBMode);
            }

            if (self.slideUpPole == 17 && oldSlideUpPoles <= 0)
            {
                self.slideUpPole += modifiedTech.poleBonus;
            }
        }
    }
}