using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using MonoMod.Cil;
using BeyondTheWest;
using Mono.Cecil.Cil;

public class CoreFunc
{
    public static ConditionalWeakTable<AbstractCreature, CoreObject.AbstractEnergyCore> cwtCore = new();

    public static void ApplyHooks()
    {
        On.Player.ctor += Player_coreInit;
        On.Player.Jump += Player_CoreBetaJump;
        On.Player.Update += Player_CoreUpdate;
        IL.Weapon.Update += Weapon_PassThroughCore;
        On.Creature.Violence += Player_Explosion_Resistance;
        Plugin.Log("CoreFunc ApplyHooks Done !");
    }

    public static bool IsCore(Player player)
    {
        return player.SlugCatClass.ToString() == "Core";
    }
    public static bool DoNotDeflect(Weapon weapon, SharedPhysics.CollisionResult result)
    {
        // Plugin.Log("Testing spear deflect with " + weapon + " by " + weapon.thrownBy + " hitting " + result.obj);
        if (weapon != null && 
            result.obj != null && weapon.thrownBy != null &&
            weapon.thrownBy is Player player && player != null && result.obj is CoreObject.EnergyCore core
            && player == core.player)
        {
            Plugin.Log("Allowed spear to go through core");
            return true;
        }
        return false;
    }
    public static void GiveCoreToPlayer(Player player)
    {
        Plugin.Log("Trying to add core to " + player.ToString());

        CoreObject.AbstractEnergyCore abstractEnergyCore = new(player.abstractCreature); 
        cwtCore.Add(player.abstractCreature, abstractEnergyCore); 
        player.room.abstractRoom.AddEntity(abstractEnergyCore);
        // abstractEnergyCore.RealizeInRoom();

        Plugin.Log("Core " + abstractEnergyCore.ToString() + " of " + player.abstractCreature.ToString() + "(" + player.ToString() + ")" + " added in world !");
    }


    // Hooks

    private static void Player_CoreUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (IsCore(self))
        {
            bool hasCore = cwtCore.TryGetValue(self.abstractCreature, out var AEC);
            if (!hasCore || AEC == null || AEC.world == null || AEC.pos == null || self.abstractCreature.world.GetAbstractRoom(AEC.pos) == null)
            {
                Plugin.Log("Something wrong happened to the core of " + self.ToString() + ". Fixing it...");
                if (hasCore) {
                    if (AEC != null)
                    {
                        AEC.RealizedCore?.Destroy();
                        AEC.Destroy();
                    }
                    cwtCore.Remove(self.abstractCreature);
                }
                GiveCoreToPlayer(self);
            }
        }
        orig(self, eu);
    }
    private static void Player_Explosion_Resistance(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self != null && self is Player player && player != null && type == Creature.DamageType.Explosion && IsCore(player))
        {
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage * 0.75f, stunBonus * 0.5f);
        }
        else
        {
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }
    }

    private static void Player_coreInit(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (IsCore(self) && !cwtCore.TryGetValue(self.abstractCreature, out _))
        {
            GiveCoreToPlayer(self);
        }
    }
    private static void Player_CoreBetaJump(On.Player.orig_Jump orig, Player self)
    {
        // Plugin.Log("Jump ? (Core)");
        if (cwtCore.TryGetValue(self.abstractCreature, out var AEC) && AEC.IsBetaBoost)
        {
            if ((AEC.coreBoostLeft > 0 || AEC.CoreMaxBoost <= 0) && AEC.boostingCount >= 0)
            {
                CoreObject.EnergyCore energyCore = AEC.RealizedCore;
                var predictedAnim = BTWFunc.PredictJump(self);

                if (energyCore == null 
                    || !energyCore.BoostAllowed     
                    || (
                        (predictedAnim != self.animation) 
                        && (predictedAnim == Player.AnimationIndex.BellySlide ||
                            predictedAnim == Player.AnimationIndex.RocketJump ||
                            predictedAnim == Player.AnimationIndex.LedgeGrab ||
                            predictedAnim == Player.AnimationIndex.LedgeCrawl ||
                            predictedAnim == Player.AnimationIndex.Flip)))
                {
                    Plugin.Log("Tech exception applied, no boosting");
                }
                else
                {
                    return;
                }
            }
            
            AEC.boostingCount = -80;
            AEC.antiGravityCount = -10;
        }
        orig(self);
        // Plugin.Log("Jumped ! (Core)");
    }
    private static void Weapon_PassThroughCore(ILContext il)
    {
        Plugin.Log("Weapon PassThrough IL starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStfld<Weapon>("floorBounceFrames"),
                x => x.MatchRet()
                )
            )
            {
                cursor.MoveAfterLabels();

                cursor.Emit(OpCodes.Nop);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_S, (byte)16);
                cursor.EmitDelegate(DoNotDeflect);
                Instruction Mark = cursor.Previous;

                cursor.Emit(OpCodes.Ldloca_S, (byte)16); // this took 7h to debug. God.
                cursor.Emit(OpCodes.Ldnull);
                cursor.Emit(OpCodes.Stfld, typeof(SharedPhysics.CollisionResult).GetField("obj"));
                Instruction Mark2 = cursor.Next;

                if (cursor.TryGotoPrev(MoveType.After, x => x == Mark))
                {
                    cursor.Emit(OpCodes.Brfalse_S, Mark2);
                }
                else { Plugin.logger.LogError("Couldn't find IL hook 2 :<"); }
            }
            else { Plugin.logger.LogError("Couldn't find IL hook 1 :<"); }

            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        // Plugin.Log(il);
        Plugin.Log("Weapon PassThrough IL ends");
    }
}