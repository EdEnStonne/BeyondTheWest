using UnityEngine;
using System;
using System.Collections.Generic;
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
    
    public struct RadiusCheckResultObject
    {
        public PhysicalObject physicalObject;        
        public List<BodyChunk> bodyChunksHit = new List<BodyChunk>();
        public BodyChunk closestBodyChunk;
        public Vector2 vectorDistance = Vector2.zero; // from pos to chunk
        public float distance = 0f;

        public RadiusCheckResultObject(PhysicalObject physicalObject)
        {
            this.physicalObject = physicalObject;
        }
    }
    public static bool IsObjectInRadius(PhysicalObject physicalObject, Vector2 position, float radius, out RadiusCheckResultObject radiusCheckResultObject)
    {
        radiusCheckResultObject = new(physicalObject);

        float dist = -1;
        BodyChunk cbody = null;
        Vector2 vecDist = Vector2.one;
        foreach (BodyChunk c in physicalObject.bodyChunks)
        {
            Vector2 cdist = c.pos - position;
            float realdist = Mathf.Max(0, cdist.magnitude - c.rad);
            if (realdist < radius)
            {
                if (realdist < dist || cbody == null)
                {
                    cbody = c;
                    dist = realdist;
                    vecDist = cdist.normalized;
                }
                radiusCheckResultObject.bodyChunksHit.Add(c);
            }
        }
        if (cbody != null)
        {
            radiusCheckResultObject.closestBodyChunk = cbody;
            radiusCheckResultObject.distance = dist;
            radiusCheckResultObject.vectorDistance = vecDist;   
            return true;
        }
        return false;
    }
    public static List<RadiusCheckResultObject> GetAllCreatureInRadius(Room room, Vector2 position, float radius)
    {
        List<RadiusCheckResultObject> creatureslist = new();
        foreach (AbstractCreature Abcreature in room.abstractRoom.creatures)
        {
            if (Abcreature.realizedCreature != null 
                && !Abcreature.realizedCreature.slatedForDeletetion 
                && !Abcreature.realizedCreature.inShortcut
                && IsObjectInRadius(Abcreature.realizedCreature, position, radius, out var resultCreature))
            {
                creatureslist.Add(resultCreature);
            }
        }
        return creatureslist;
    }
    public static List<RadiusCheckResultObject> GetAllObjectsInRadius(Room room, Vector2 position, float radius)
    {
        List<RadiusCheckResultObject> objectlist = new();
        for (int j = 0; j < room.physicalObjects.Length; j++)
		{
			for (int k = 0; k < room.physicalObjects[j].Count; k++)
            {
                PhysicalObject physicalObject = room.physicalObjects[j][k];
                if (!physicalObject.slatedForDeletetion
                    && (physicalObject is not Creature c || !c.inShortcut)
                    && IsObjectInRadius(physicalObject, position, radius, out var resultCreature))
                {
                    objectlist.Add(resultCreature);
                }
            }
        }
        return objectlist;
    }

    public static float Random(float minValue, float maxValue)
    {
        return UnityEngine.Random.Range(minValue, maxValue);
    }
    public static float Random(float MaxValue)
    {
        return Random(0, MaxValue);
    }
    public static float Random()
    {
        return Random(0, 1);
    }
    public static float random
    {
        get
        {
            return Random();
        }
    }

    public static Vector2 RandomVector(float minXValue, float maxXValue, float minYValue, float maxYValue)
    {
        return new Vector2(Random(minXValue, maxXValue), Random(minYValue, maxYValue));
    }
    public static Vector2 RandomCircleVector()
    {
        return RandomVector(-1, 1, -1, 1).normalized;
    }
    public static Vector2 RandomCircleVector(float rad)
    {
        return RandomCircleVector() * rad;
    }
    
    public static void CustomKnockback(BodyChunk bodyChunk, Vector2 force, bool notifyMeadow = false)
    {
        bodyChunk.vel += force;
        if (notifyMeadow && Plugin.meadowEnabled && !MeadowCompat.IsMine(bodyChunk.owner.abstractPhysicalObject))
        {
            MeadowCompat.BTWFuncMeadow_RPCCustomKnockBack(bodyChunk.owner, (short)bodyChunk.index, force);
        }
    }
    public static void CustomKnockback(BodyChunk bodyChunk, Vector2 direction, float force, bool notifyMeadow = false)
    {
        CustomKnockback(bodyChunk, direction.normalized * force, notifyMeadow);
    }
    public static void CustomKnockback(BodyChunk bodyChunk, float forceX, float forceY, bool notifyMeadow = false)
    {
        CustomKnockback(bodyChunk, new Vector2(forceX, forceY), notifyMeadow);
    }
    public static void CustomKnockback(PhysicalObject physicalObject, Vector2 force, bool notifyMeadow = false)
    {
        foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
        { 
            CustomKnockback(bodyChunk, force); 
        }
        if (notifyMeadow && Plugin.meadowEnabled && !MeadowCompat.IsMine(physicalObject.abstractPhysicalObject))
        {
            MeadowCompat.BTWFuncMeadow_RPCCustomKnockBack(physicalObject, -1, force);
        }
    } 
    public static void CustomKnockback(PhysicalObject physicalObject, Vector2 direction, float force, bool notifyMeadow = false)
    {
        CustomKnockback(physicalObject, direction.normalized * force, notifyMeadow);
    } 
    public static void CustomKnockback(PhysicalObject physicalObject, Vector2 direction, float force, float randomForce, bool notifyMeadow = false)
    {
        foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
        { 
            CustomKnockback(bodyChunk, direction.normalized * (force + Random(-randomForce, randomForce)), notifyMeadow); 
        }
    } 
    public static void CustomKnockback(PhysicalObject physicalObject, float forceX, float forceY, bool notifyMeadow = false)
    {
        CustomKnockback(physicalObject, new Vector2(forceX, forceY), notifyMeadow);
    } 

    public static bool CanTwoObjectsInteract(PhysicalObject physicalObject1, PhysicalObject physicalObject2)
    {
        return physicalObject1 != null && physicalObject2 != null
            && physicalObject1 != physicalObject2 
            && (
                physicalObject1.abstractPhysicalObject.rippleLayer == physicalObject2.abstractPhysicalObject.rippleLayer 
                || physicalObject1.abstractPhysicalObject.rippleBothSides 
                || physicalObject2.abstractPhysicalObject.rippleBothSides) 
            && !physicalObject1.slatedForDeletetion
            && !physicalObject2.slatedForDeletetion;
    }

    public static Vector2 RandomExitPos(ArenaGameSession arena)
    {
        if (arena?.room != null)
        {
            return RandomExitPos(arena.room);
        }
        return Vector2.zero;
    }
    public static Vector2 RandomExitPos(Room room)
    {
        if (room != null)
        {
            int exit = Mathf.Max(0, (int)Random(room.abstractRoom.exits));
            ShortcutData shortcutData = room.ShortcutLeadingToNode(exit); 
            return shortcutData.StartTile.ToVector2() * 20f + Vector2.one * 10;
        }
        return Vector2.zero;
    }

    public static void ResetCore(Player player)
    {
        if (CoreFunc.cwtCore.TryGetValue(player.abstractCreature, out var AEC))
        {
            AEC.energy = AEC.CoreMaxEnergy;
            AEC.antiGravityCount = 0;
            AEC.oxygenCount = 0;
            AEC.repairCount = 0;
            AEC.slowModeCount = 0;
            AEC.boostingCount = -FrameRate * 5;
            AEC.antiGravityCount = 0;
            AEC.waterCorrectionCount = 0;
            AEC.coreBoostLeft = AEC.CoreMaxBoost;
            if (AEC.RealizedCore != null)
            {
                AEC.RealizedCore.grayScale = 0f;
            }
        }
    }
    public static void ResetSpark(Player player)
    {
        if (SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
        {
            // SCM.Charge = 0;
            SCM.dischargeCooldown = FrameRate * 3;
            SCM.overchargeImmunity = FrameRate * 1;
        }
    }

    public static float EaseInOutSine(float x) { return -(Mathf.Cos(Mathf.PI * x) - 1) / 2; }
    public static float EaseOut(float x, float pow = 2) { return 1f - Mathf.Pow(1f - x, pow); }
    public static float EaseIn(float x, float pow = 2) { return Mathf.Pow(x, pow); }
    public static float EaseInOut(float x, float pow) { return (x < 0.5) ? Mathf.Pow(2, pow - 1) * Mathf.Pow(x, pow) : 1 - Mathf.Pow(-2 * x + 2, pow) / 2;; }

}