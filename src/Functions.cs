using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using BepInEx.Logging;
using System;
using BepInEx;
using MonoMod.Cil;
using BeyondTheWest;

public class BTWFunc
{
    public const int FrameRate = 40;
    // ma functions
    public static Vector2 OffsetRelativeToRot(float rot, Vector2 offset)
    {
        return new Vector2((float)(offset.x * Math.Cos(rot) - offset.y * Math.Sin(rot)), (float)(offset.x * Math.Sin(rot) + offset.y * Math.Cos(rot)));
    }
    public static Vector2 OffsetRelativeToVRot(Vector2 rotation, Vector2 offset)
    {
        float rot = (float)(-rotation.GetRadians() - Math.PI / 2);
        return OffsetRelativeToRot(rot, offset);
    }
    public static Vector2 OffsetRelativeToBody(BodyChunk body, Vector2 offset)
    {
        return OffsetRelativeToVRot(body.Rotation, offset);
    }
    public static int EnergyBoostPossibleAction(Player player)
    {
        if (
            player.bodyMode != Player.BodyModeIndex.WallClimb &&
            player.bodyMode != Player.BodyModeIndex.CorridorClimb &&
            !(
                player.animation == Player.AnimationIndex.LedgeGrab ||
                player.animation == Player.AnimationIndex.ClimbOnBeam
            ) &&
            (
                (
                    player.animation != Player.AnimationIndex.Roll &&
                    player.animation == Player.AnimationIndex.BellySlide &&
                    !(!player.whiplashJump && player.input[0].x != -player.rollDirection)
                ) || (
                    player.animation != Player.AnimationIndex.Roll &&
                    player.animation != Player.AnimationIndex.BellySlide &&
                    player.animation != Player.AnimationIndex.AntlerClimb &&
                    !(player.animation == Player.AnimationIndex.ZeroGSwim) && !(player.animation == Player.AnimationIndex.ZeroGPoleGrab) &&
                    !(player.animation == Player.AnimationIndex.DownOnFours && player.bodyChunks[1].ContactPoint.y < 0 && player.input[0].downDiagonal == player.flipDirection) &&
                    player.standing &&
                    player.slideCounter > 0 && player.slideCounter < 10
                )
            )
        )
        {
            return 2;
        }
        else if (player.animation == Player.AnimationIndex.StandUp
            || player.animation == Player.AnimationIndex.Flip
            || player.animation == Player.AnimationIndex.GetUpToBeamTip
            || player.animation == Player.AnimationIndex.ZeroGSwim
            || player.animation == Player.AnimationIndex.RocketJump
            || player.animation == Player.AnimationIndex.None)
        {
            return 1;
        }
        return 0;
    }
    public static int NightCycleTime(World world, int dayDuration)
    {
        RainWorld rainWorld = world.game.rainWorld;
        return rainWorld.setup.cycleTimeMin * 40
            + (rainWorld.setup.cycleTimeMax - rainWorld.setup.cycleTimeMin) * 40
            - (dayDuration - rainWorld.setup.cycleTimeMin * 40);
    }
    public static Player.AnimationIndex PredictJump(Player player)
    {
        PredictJump(player, out var animationPredicted);
        return animationPredicted;
    }
    public static void PredictJump(Player player, out Player.AnimationIndex animationPredicted)
    {
        animationPredicted = player.animation;
        if (player.bodyMode == Player.BodyModeIndex.WallClimb)
        {
            return;
        }
        if (!(player.bodyMode == Player.BodyModeIndex.CorridorClimb))
        {
            if (player.animation == Player.AnimationIndex.LedgeGrab)
            {
                if (player.input[0].x != 0)
                {
                    return;
                }
            }
            else if (player.animation == Player.AnimationIndex.ClimbOnBeam)
            {
                if (player.input[0].x != 0)
                {
                    animationPredicted = Player.AnimationIndex.None;
                    return;
                }
                if (player.input[0].y <= 0)
                {
                    animationPredicted = Player.AnimationIndex.None;
                    return;
                }
                if (player.slowMovementStun < 1 && player.slideUpPole < 1)
                {
                    return;
                }
            }
            else
            {
                if (player.animation == Player.AnimationIndex.Roll)
                {
                    animationPredicted = Player.AnimationIndex.RocketJump;
                    return;
                }
                if (player.animation == Player.AnimationIndex.BellySlide)
                {
                    if (!player.whiplashJump && player.input[0].x != -player.rollDirection)
                    {
                        animationPredicted = Player.AnimationIndex.RocketJump;
                        return;
                    }
                    animationPredicted = Player.AnimationIndex.Flip;
                }
                else
                {
                    if (player.animation == Player.AnimationIndex.AntlerClimb)
                    {
                        animationPredicted = Player.AnimationIndex.None;
                        return;
                    }
                    if (!(player.animation == Player.AnimationIndex.ZeroGSwim) && !(player.animation == Player.AnimationIndex.ZeroGPoleGrab))
                    {
                        int num5 = player.input[0].x;
                        bool flag = false;
                        if (player.animation == Player.AnimationIndex.DownOnFours && player.bodyChunks[1].ContactPoint.y < 0 && player.input[0].downDiagonal == player.flipDirection)
                        {
                            animationPredicted = Player.AnimationIndex.BellySlide;
                            flag = true;
                        }
                        if (!flag)
                        {
                            animationPredicted = Player.AnimationIndex.None;
                            if (player.standing)
                            {
                                if (player.slideCounter > 0 && player.slideCounter < 10)
                                {
                                    animationPredicted = Player.AnimationIndex.Flip;
                                }
                            }
                            if (player.bodyChunks[1].onSlope != 0)
                            {
                                if (num5 == -player.bodyChunks[1].onSlope)
                                {
                                    return;
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
    public static bool BodyChunkSumberged(BodyChunk chunk)
    {
        return chunk.submersion > 0.9f;
    }
    public static float EaseInOutSine(float x) { return -(Mathf.Cos(Mathf.PI * x) - 1) / 2; }
    public static float EaseOut(float x, float pow = 2) { return 1f - Mathf.Pow(1f - x, pow); }
    public static float EaseIn(float x, float pow = 2) { return Mathf.Pow(x, pow); }
    public static float EaseInOut(float x, float pow) { return (x < 0.5) ? Mathf.Pow(2, pow - 1) * Mathf.Pow(x, pow) : 1 - Mathf.Pow(-2 * x + 2, pow) / 2;; }

}