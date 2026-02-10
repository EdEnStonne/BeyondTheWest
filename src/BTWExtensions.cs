using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

namespace BeyondTheWest;

public static class BTWExtensions
{
    public static StaticChargeManager GetStaticChargeManager(this Player self)
    {
        return self?.abstractCreature != null && StaticChargeManager.TryGetManager(self.abstractCreature, out var SCM) ? SCM : null; 
    }
    public static StaticChargeManager GetSCM(this Player self)
    {
        return GetStaticChargeManager(self); 
    }

    public static AbstractEnergyCore GetAbstractEnergyCore(this Player self)
    {
        return self?.abstractCreature != null && AbstractEnergyCore.TryGetCore(self.abstractCreature, out var core) ? core : null; 
    }
    public static AbstractEnergyCore GetAEC(this Player self)
    {
        return GetAbstractEnergyCore(self); 
    }

    public static ModifiedTechManager GetModifiedTechManager(this Player self)
    {
        return self?.abstractCreature != null && ModifiedTechManager.TryGetManager(self.abstractCreature, out var MTM) ? MTM : null; 
    }
    public static ModifiedTechManager GetMTM(this Player self)
    {
        return GetModifiedTechManager(self); 
    }

    public static PoleKickManager GetPoleKickManager(this Player self)
    {
        return self?.abstractCreature != null && PoleKickManager.TryGetManager(self.abstractCreature, out var PKM) ? PKM : null; 
    }
    public static PoleKickManager GetPKM(this Player self)
    {
        return GetPoleKickManager(self); 
    }

    public static WallClimbManager GetWallClimbManager(this Player self)
    {
        return self?.abstractCreature != null && WallClimbManager.TryGetManager(self.abstractCreature, out var WCM) ? WCM : null; 
    }
    public static WallClimbManager GetWKM(this Player self)
    {
        return GetWallClimbManager(self); 
    }
    
    public static BTWPlayerData GetBTWPlayerData(this Player self)
    {
        return self?.abstractCreature != null 
            && BTWPlayerData.TryGetManager(self.abstractCreature, out var btwPlayerData) ? 
                btwPlayerData : null; 
    }
    public static BTWCreatureData GetBTWData(this Creature self)
    {
        return self?.abstractCreature != null 
            && BTWCreatureData.TryGetManager(self.abstractCreature, out var bTWCreatureData) ? 
                bTWCreatureData : null; 
    }
    public static BTWCreatureData GetBTWData(this AbstractCreature self)
    {
        return BTWCreatureData.TryGetManager(self, out var bTWCreatureData) ? 
                bTWCreatureData : null; 
    }

    public static bool IsTrailseeker(this Player self)
    {
        return self != null && TrailseekerFunc.IsTrailseeker(self); 
    }
    public static bool IsCore(this Player self)
    {
        return self != null && CoreFunc.IsCore(self); 
    }
    public static bool IsSpark(this Player self)
    {
        return self != null && SparkFunc.IsSpark(self); 
    }

    public static bool IsWalkable(this Room.Tile tile)
    {
        return tile.Terrain == Room.Tile.TerrainType.Solid
            || tile.Terrain == Room.Tile.TerrainType.Slope
            || tile.Terrain == Room.Tile.TerrainType.Floor;
    }
    public static bool IsAir(this Room.Tile tile)
    {
        return tile.Terrain == Room.Tile.TerrainType.Air;
    }
    public static Room.SlopeDirection GetSlope(this Room.Tile tile, Room room)
    {
        return room.IdentifySlope(new IntVector2(tile.X, tile.Y));
    }
    public static IntVector2 GetSlopeAngle(this Room.Tile tile, Room room)
    {
        Room.SlopeDirection dir = tile.GetSlope(room);
        if (dir == Room.SlopeDirection.Broken)
        {
            return new IntVector2(0, 0);
        }
        return new IntVector2(
            dir == Room.SlopeDirection.DownRight || dir == Room.SlopeDirection.UpRight ? 1 : -1,
            dir == Room.SlopeDirection.UpLeft || dir == Room.SlopeDirection.UpRight ? 1 : -1);
    }

    public static bool Local(this PhysicalObject physicalObject)
    {
        return BTWFunc.IsLocal(physicalObject);
    }
    public static bool Local(this AbstractPhysicalObject abstractPhysicalObject)
    {
        return BTWFunc.IsLocal(abstractPhysicalObject);
    }

    public static int VoidConductiveScore(this UpdatableAndDeletable updatable)
    {
        return VoidSpark.ConductiveScore(updatable);
    }

    public static Vector2 RayTraceSolid(this Room self, Vector2 start, Vector2 end, float precision = 10f)
    {
        if (self.GetTile(start).Solid)
        {
            return start;
        }

        float dist = Vector2.Distance(start, end);
        for (float step = 20f; step < dist; step += precision)
        {
            Vector2 stepPos = Vector2.Lerp(start, end, step / dist);
            if (self.GetTile(stepPos).Solid 
                // || (self.terrain != null && self.terrain.Contains(stepPos)) // room.terrain doesn't exist...?
            )
            {
                return stepPos;
            }
        }
        
        return end;
    }
}

public static class BTWExtensionsHook
{
    public static void ApplyHooks()
    {
        IL.Weapon.Update += Weapon_BounceFix;
        On.Player.ProcessDebugInputs += Player_Debug;
    }

    private static void Player_Debug(On.Player.orig_ProcessDebugInputs orig, Player self)
    {
        orig(self);
        if (self.room == null || !self.room.game.devToolsActive || !self.Local()) { return; }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            for (int i = 0; i < 20; i++)
            {
                self.room.AddObject( new VoidSpark(
                    self.room.RandomPos(), 1f, 20
                ) );
            }
        }
    }

    public static int AllowBounceChain(int orig, Weapon weapon)
    {
        return weapon.floorBounceFrames / 2; // I unruined the fun for you
    }
    public static bool BounceHorizontal(Weapon weapon)
    {
        // from Weapon.Update
        BodyChunk chunk = weapon.firstChunk;
        float exitSpeed = weapon.overrideExitThrownSpeed > 0f ? weapon.overrideExitThrownSpeed : weapon.exitThrownModeSpeed; 
        IntVector2 tilePos = weapon.room.GetTilePosition(chunk.pos);
        IntVector2 nextVerticalTilePos = tilePos;
        nextVerticalTilePos.y += weapon.throwDir.y;
        // if (weapon.floorBounceFrames > 0)
        // {
        //     BTWPlugin.Log($"Weapon at [{weapon.room.GetTilePosition(chunk.pos)}] can bounce for <{weapon.floorBounceFrames}> ticks with colision [{chunk.ContactPoint}] on slope [{weapon.room.GetTile(chunk.pos).GetSlopeAngle(weapon.room)}], next slope [{weapon.room.GetTile(weapon.room.GetTilePosition(chunk.pos) + weapon.throwDir).GetSlopeAngle(weapon.room)}], with throwDir [{weapon.throwDir}]");
        // }
        if (weapon.floorBounceFrames > 0 
            && weapon.throwDir.y != 0 
            && weapon.throwDir.x == 0 // added this check so no priority issues
            && (chunk.ContactPoint.y != 0 || chunk.ContactPoint.x != 0) 
            && weapon.room.GetTile(nextVerticalTilePos).Terrain == Room.Tile.TerrainType.Slope)
        {

            Custom.Log(new string[]
            {
                "BTW horizonal bounce"
            });
            IntVector2 slopeAngle = weapon.room.GetTile(nextVerticalTilePos).GetSlopeAngle(weapon.room);
            chunk.vel.y = 0.5f * weapon.throwDir.y;
            weapon.throwDir = new IntVector2(slopeAngle.x, 0);
            chunk.vel.x = slopeAngle.x * Mathf.Max(chunk.vel.magnitude, exitSpeed + 1f);
            chunk.pos = weapon.room.MiddleOfTile(nextVerticalTilePos);
            weapon.floorBounceFrames /= 2;
            weapon.ChangeMode(Weapon.Mode.Thrown);
            weapon.thrownPos = chunk.pos;
            weapon.throwModeFrames = -1;
            weapon.setRotation = new Vector2?(weapon.throwDir.ToVector2());
            weapon.rotationSpeed = 0f;
            for (int m = 0; m < 4; m++)
            {
                weapon.room.AddObject(new Spark(
                    chunk.pos, 
                    chunk.vel * UnityEngine.Random.value + Custom.RNV() * UnityEngine.Random.value, 
                    new Color(1f, 1f, 1f), null, 6, 18));
            }
            weapon.room.PlaySound(SoundID.Weapon_Skid, chunk, false, 1f, 1f);
            return true;
        }
        return false;
    }
    public static void FloorBounceHorizontal(Weapon weapon)
    {
        // from Weapon.Update
        BodyChunk chunk = weapon.firstChunk;
        float exitSpeed = weapon.overrideExitThrownSpeed > 0f ? weapon.overrideExitThrownSpeed : weapon.exitThrownModeSpeed; 
        if (weapon.floorBounceFrames > 0 && weapon.throwDir.x == 0 && weapon.throwDir.y != 0)
		{
			weapon.floorBounceFrames--;
			if (chunk.ContactPoint.x != 0 && Mathf.Abs(chunk.vel.y) > 3f)
			{
				chunk.vel.x = (Mathf.Abs(chunk.vel.x) + 1f) * -chunk.ContactPoint.x 
                    + ((chunk.ContactPoint.x < 0) ? 4.5f : 0f);
				for (int i = 0; i < 4; i++)
				{
					weapon.room.AddObject(new Spark(
                        new Vector2(chunk.pos.x, weapon.room.MiddleOfTile(chunk.pos).y + chunk.ContactPoint.y * 10f), 
                        chunk.vel * UnityEngine.Random.value + Custom.RNV() * (UnityEngine.Random.value * 4f) - chunk.ContactPoint.ToVector2() * (4f * UnityEngine.Random.value), 
                        new Color(1f, 1f, 1f), null, 6, 18));
				}
				weapon.room.PlaySound(SoundID.Weapon_Skid, chunk, false, 1f, 1f);
				chunk.vel.y = Mathf.Max(exitSpeed + 1f, Mathf.Abs(chunk.vel.y)) * Mathf.Sign(chunk.vel.y);
				weapon.floorBounceFrames /= 2;
			}
		}
    }
    private static void Weapon_BounceFix(ILContext il)
    {
        BTWPlugin.Log("BTWExtensions IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
                
            if (cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Weapon>(nameof(Weapon.floorBounceFrames)),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<Weapon>(nameof(Weapon.throwDir)),
                x => x.MatchLdfld<IntVector2>(nameof(IntVector2.y)),
                x => x.MatchBrtrue(out _)
            ))
            {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(FloorBounceHorizontal);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook 1 :<");
            }
                
            if (cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Weapon>(nameof(Weapon.floorBounceFrames)),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<Weapon>(nameof(Weapon.throwDir)),
                x => x.MatchLdfld<IntVector2>(nameof(IntVector2.x)),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.firstChunk)).GetGetMethod()),
                x => x.MatchCallvirt(typeof(BodyChunk).GetProperty(nameof(BodyChunk.ContactPoint)).GetGetMethod()),
                x => x.MatchLdfld<IntVector2>(nameof(IntVector2.x)),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.firstChunk)).GetGetMethod()),
                x => x.MatchCallvirt(typeof(BodyChunk).GetProperty(nameof(BodyChunk.ContactPoint)).GetGetMethod()),
                x => x.MatchLdfld<IntVector2>(nameof(IntVector2.y)),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.firstChunk)).GetGetMethod()),
                x => x.MatchLdfld<BodyChunk>(nameof(BodyChunk.pos)),
                x => x.MatchCallvirt<Room>(nameof(Room.GetTile)),
                x => x.MatchLdfld<Room.Tile>(nameof(Room.Tile.Terrain)),
                x => x.MatchLdcI4(2),
                x => x.MatchBneUn(out _)
            ))
            {
                cursor.MoveAfterLabels();
                ILLabel checkpoint = cursor.DefineLabel();
                cursor.MarkLabel(checkpoint);

                ILLabel pastTheIf = cursor.DefineLabel();
                if (cursor.TryGotoNext(MoveType.After,
                        x => x.MatchLdarg(0),
                        x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                        x => x.MatchLdsfld<SoundID>(nameof(SoundID.Weapon_Skid)),
                        x => x.MatchLdarg(0),
                        x => x.MatchCall(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.firstChunk)).GetGetMethod()),
                        x => x.MatchLdcI4(0),
                        x => x.MatchLdcR4(1),
                        x => x.MatchLdcR4(1),
                        x => x.MatchCallvirt<Room>(nameof(Room.PlaySound))) 
                    && cursor.TryGotoNext(MoveType.After,
                        x => x.MatchBr(out pastTheIf))
                )
                {
                    cursor.GotoLabel(checkpoint, MoveType.Before);
                    cursor.MoveAfterLabels();
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(BounceHorizontal);
                    cursor.Emit(OpCodes.Brtrue, pastTheIf);
                }
                else
                {
                    BTWPlugin.logger.LogError("Couldn't find IL hook 2.5 :<");
                }

            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook 2 :<");
            }
                
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStfld<Weapon>(nameof(Weapon.floorBounceFrames))
            ))
            {
                cursor.GotoPrev(MoveType.After, x => x.MatchLdcI4(0));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(AllowBounceChain);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook 3 :<");
            }

            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("BTWExtensions IL 1 ends");
        // BTWPlugin.Log(il);
    }
}