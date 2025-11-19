using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using BepInEx;
using BepInEx.Logging;
using System;
using MonoMod.Cil;
using RWCustom;
using BeyondTheWest;
using Mono.Cecil.Cil;
using System.Runtime.InteropServices;

public class CoreFunc
{
    public static ConditionalWeakTable<AbstractCreature, CoreObject.AbstractEnergyCore> cwtCore = new();

    public static void ApplyHooks()
    {
        On.Player.ctor += Player_coreInit;
        On.Player.Jump += Player_CoreBetaJump;
        IL.Weapon.Update += Weapon_PassThroughCore;
        Plugin.Log("CoreFunc ApplyHooks Done !");
    }


    public static bool IsCore(Player player)
    {
        return player.SlugCatClass.ToString() == "Core";
    }
    public static bool DoNotDeflect(Weapon weapon, SharedPhysics.CollisionResult result)
    {
        // Plugin.Log("Testing spear deflect with " + weapon + " by " + weapon.thrownBy + " hitting " + result.obj);
        if (result.obj != null && weapon.thrownBy != null &&
            weapon.thrownBy is Player player && result.obj is CoreObject.EnergyCore core
            && player == core.player)
        {
            Plugin.Log("Allowed spear to go through core");
            return true;
        }
        return false;
    }


    // Hooks


    private static void Player_coreInit(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (IsCore(self) && !cwtCore.TryGetValue(self.abstractCreature, out _))
        {
            Plugin.Log("Trying to add core to " + self.ToString());

            CoreObject.AbstractEnergyCore abstractEnergyCore = new(self.abstractCreature); 
            cwtCore.Add(self.abstractCreature, abstractEnergyCore); 
            self.room.abstractRoom.AddEntity(abstractEnergyCore);
            // abstractEnergyCore.RealizeInRoom();

            Plugin.Log("Core " + abstractEnergyCore.ToString() + " of " + self.abstractCreature.ToString() + "(" + self.ToString() + ")" + " added in world !");
        }
    }
    private static void Player_CoreBetaJump(On.Player.orig_Jump orig, Player self)
    {
        // Plugin.Log("Jump ? (Core)");
        if (cwtCore.TryGetValue(self.abstractCreature, out var AEC) && AEC.IsBetaBoost && (AEC.coreBoostLeft > 0 || AEC.CoreMaxBoost <= 0) && AEC.boostingCount >= 0)
        {
            CoreObject.EnergyCore energyCore = AEC.RealizedCore;
            var predictedAnim = BTWFunc.PredictJump(self);

            if (energyCore == null || 
                !energyCore.BoostAllowed ||    
                    ((predictedAnim != self.animation) &&
                    (predictedAnim == Player.AnimationIndex.BellySlide ||
                    predictedAnim == Player.AnimationIndex.RocketJump ||
                    predictedAnim == Player.AnimationIndex.LedgeGrab ||
                    predictedAnim == Player.AnimationIndex.LedgeCrawl ||
                    predictedAnim == Player.AnimationIndex.Flip)))
            {
                // Plugin.Log("Tech exception applied, no boosting");
                AEC.boostingCount = -20;
                AEC.antiGravityCount = -10;
            }
            else
            {
                return;
            }
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