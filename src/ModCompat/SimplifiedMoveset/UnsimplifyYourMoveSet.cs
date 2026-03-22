using UnityEngine;
using BeyondTheWest;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using SimplifiedMoveset;

namespace BeyondTheWest.SimplifiedMovesetCompat;
public static class BTWSimplifiedMoveset
{
    // Hooks
    public static void ApplyHooks()
    {
        new Hook(typeof(PlayerMod).GetMethod(nameof(PlayerMod.IsVoidSlugcat)), DisableSimpleWallClimbForTrailseeker);
        new Hook(typeof(PlayerMod).GetMethod(nameof(PlayerMod.IsClimbingOnBeam)), IamNOTclimbingonthatBEAM);
        new Hook(typeof(PlayerMod).GetMethod(nameof(PlayerMod.UpdateAnimation_BeamTip)), DisablePoleUpdateOnPoleRelease);
        new Hook(typeof(PlayerMod).GetMethod(nameof(PlayerMod.UpdateAnimation_ClimbOnBeam)), DisablePoleUpdateOnPoleRelease);
        new Hook(typeof(PlayerMod).GetMethod(nameof(PlayerMod.UpdateAnimation_GetUpOnBeam)), DisablePoleUpdateOnPoleRelease);
        new Hook(typeof(PlayerMod).GetMethod(nameof(PlayerMod.UpdateAnimation_HangFromBeam)), DisablePoleUpdateOnPoleRelease);
        new Hook(typeof(PlayerMod).GetMethod(nameof(PlayerMod.UpdateAnimation_HangUnderVerticalBeam)), DisablePoleUpdateOnPoleRelease);
        new Hook(typeof(PlayerMod).GetMethod(nameof(PlayerMod.UpdateAnimation_StandOnBeam)), DisablePoleUpdateOnPoleRelease);
        BTWPlugin.Log("BTWSimplifiedMoveset ApplyHooks Done !");
    }

    //----------- Function
    public static void DetatchFromBeam(Player player)
    {
        if (player.Get_Attached_Fields() is PlayerMod.Player_Attached_Fields attached_fields)
        {
            attached_fields.grab_beam_cooldown_position = player.bodyChunks[1].pos;
        }
        if (player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam) // I might have to resort to this
        {
            player.animation = Player.AnimationIndex.None;
            player.bodyMode = Player.BodyModeIndex.Default;
        }
    }

    //----------- Hooks
    private static bool DisableSimpleWallClimbForTrailseeker(Func<Player, bool> orig, Player player)
    {
        return orig(player) || player.IsTrailseeker();
    }
    private static bool IamNOTclimbingonthatBEAM(Func<Player, bool> orig, Player player)
    {
        if (player.GetWallClimbManager() is WallClimbManager wallClimbManager
            && !wallClimbManager.holdToPoles)
        {
            return false;
        }
        return orig(player);
    }
    private static void DisablePoleUpdateOnPoleRelease(Action<Player, PlayerMod.Player_Attached_Fields> orig, Player player, PlayerMod.Player_Attached_Fields attached_fields)
    {
        if (player.GetWallClimbManager() is WallClimbManager wallClimbManager
            && !wallClimbManager.holdToPoles)
        {
            return;
        }
        orig(player, attached_fields);
    }
}