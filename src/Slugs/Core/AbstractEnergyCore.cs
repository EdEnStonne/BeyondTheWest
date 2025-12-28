using System.Collections.Generic;
using UnityEngine;
using System;
using RWCustom;
using System.Runtime.CompilerServices;
using BeyondTheWest.MeadowCompat;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest;
public class AbstractEnergyCore : AbstractPhysicalObject
{
    public static ConditionalWeakTable<AbstractCreature, AbstractEnergyCore> cores = new();
    public static AbstractObjectType EnergyCoreType;
    public static bool TryGetCore(AbstractCreature creature, out AbstractEnergyCore core)
    {
        return cores.TryGetValue(creature, out core);
    }
    public static AbstractEnergyCore GetCore(AbstractCreature creature)
    {
        TryGetCore(creature, out AbstractEnergyCore core);
        return core;
    }
    public static void AddCore(AbstractCreature creature, out AbstractEnergyCore core)
    {
        RemoveCore(creature);
        
        core = new(creature);
        cores.Add(creature, core);
        creature.Room?.AddEntity(core);
    }
    public static void AddCore(AbstractCreature creature)
    {
        AddCore(creature, out _);
    }
    public static void RemoveCore(AbstractCreature creature)
    {
        if (TryGetCore(creature, out var core))
        {
            cores.Remove(creature);
            if (!core.slatedForDeletion)
            {
                core.Destroy();
            }
        }
    }

    public AbstractEnergyCore(AbstractCreature abstractPlayer, EnergyCore energyCore = null)
        : base(abstractPlayer.world, EnergyCoreType, energyCore, abstractPlayer.pos, abstractPlayer.world.game.GetNewID())
    {
        this.abstractPlayer = abstractPlayer;
        this.destroyOnAbstraction = false;
        this.energy = this.CoreMaxEnergy;

        this.active = true;
        this.isMeadow = false;
        this.isMeadowFakePlayer = false;

        if (energyCore != null)
        {
            this.realizedObject = energyCore;
            energyCore.abstractPhysicalObject = this;
            this.RealizedOnce = true;
        }
        
        if (Plugin.meadowEnabled)
        {
            try
            {
                MeadowCalls.CoreMeadow_Init(this);
            }
            catch (System.Exception ex)
            {
                Plugin.logger.LogError($"Error while adding meadow's option of core of player [{abstractPlayer}] : " + ex);
            }
        }
    }

    // ----------- Override funcitons
    public override void Destroy()
    {
        base.Destroy();
        this.realizedObject?.Destroy();
        RemoveCore(this.abstractPlayer);
    }
    public override void Realize()
    {
        // Plugin.Log((this.realizedObject != null) + "/" + (this.Player) + "/" + (this.Player.room == this.world?.GetAbstractRoom(this.pos)?.realizedRoom) + "/" + (this.world) + "/" + (this.world?.GetAbstractRoom(this.pos)) + "/" + (this.world?.GetAbstractRoom(this.pos)?.realizedRoom));
        if (this.realizedObject != null || this.Player == null 
            || this.world?.GetAbstractRoom(this.pos)?.realizedRoom == null
            || this.world?.GetAbstractRoom(this.pos)?.realizedRoom != this.Player.room) 
        {
            return;
        }

        Plugin.Log("Realizing Core...");
        this.realizedObject = new EnergyCore(this, this.Player);
        this.realizedObject.room = this.world.GetAbstractRoom(this.pos).realizedRoom;
        this.realizedObject.firstChunk.HardSetPosition(this.Player.mainBodyChunk.pos);

        if (!this.RealizedOnce && this.realizedObject != null && this.realizedObject.room != null) {
            this.RealizedOnce = true; 
            Plugin.Log("Realized for the first time " + this.realizedObject.ToString() + " of " + this.Player.ToString() + " !");
        }

    }
    public override void Update(int time)
    {
        base.Update(time);
        // Plugin.Log("An update in " + time.ToString()); 

        if (abstractPlayer == null)
        {
            this.realizedObject?.Destroy();
            this.Destroy();
            return;
        }

        if (abstractPlayer.Room != this.Room) // all of those function doing literally nothing.
        {
            this.Move(abstractPlayer.pos);
            Plugin.Log(this.ToString() + " of " + this.abstractPlayer.ToString() + " changed rooms !!");
        }

        if (!this.RealizedOnce)
        {
            Plugin.Log("Attempting to realize for the first time " + this.ToString() + " of " + this.abstractPlayer.ToString());
            if (this.Player == null || this.Room == null) { return; }
            this.RealizeInRoom();
            return;
        }

        if (this.Player == null && this.realizedObject != null)
        {
            this.Abstractize(abstractPlayer.pos);
        }
        else if (this.Player != null && this.realizedObject == null)
        {
            Plugin.Log("Attempting to realize " + this.ToString() + " of " + this.abstractPlayer.ToString());
            this.RealizeInRoom();
        }

        if (this.realizedObject != null && this.Player != null && this.Player.room != null &&
            this.Player.room != this.realizedObject.room) // why do you not log ???
        {
            Plugin.Log(this.ToString() + " of " + this.abstractPlayer.ToString() + " is not in the good room !!");
            this.Abstractize(abstractPlayer.pos);
            this.RealizeInRoom();
        }

        if (Plugin.meadowEnabled && this.isMeadow)
        {
            MeadowCalls.CoreMeadow_Update(this);
        }
    }

    // ----------- Variables

    // Objects
    public AbstractCreature abstractPlayer;

    // Basic
    public float scale = 1f;
    public byte state = 1;
    /// 0 : Deactivated
    /// 1 : Idle, 2 : Boosting, 3 : No Boost left
    /// 4 : Anti-Gravity OFF, 5 : Anti-gravity ON, 6 : SlowDown ON
    /// 7 : Oxygen ON
    /// 10 : Meltdown

    public bool IsBetaBoost = !BTWRemix.Core0GSpecialButton.Value;
    public bool RealizedOnce = false;

    public bool active = false;
    public bool isMeadow = false;
    public bool isMeadowFakePlayer = false;
    public bool isShockwaveEnabled = true;
    public bool isMeadowArenaTimerCountdown = false;
    
    public float energy = 100.0f;
    public int boostingCount = 0;
    public int repairCount = 0;
    public int antiGravityCount = 0;
    public int oxygenCount = 0;
    public int slowModeCount = 0;
    public int waterCorrectionCount = 0;
    public int coreBoostLeft = 2;

    public float CoreMaxEnergy = 1200.0f;
    public float CoreEnergyRecharge = 40.0f;
    public float CoreMeltdown = 600.0f;
    public float CoreShockwavePower = 300.0f;
    public float CoreOxygenEnergyUsage = 100.0f;
    public float Core0GWaterEnergyUsage = 40.0f;
    public float Core0GSpaceEnergyUsage = 10.0f;
    public float CoreAntiGravity = 0.85f;
    public int CoreMaxBoost = 2;
    public int CoreAntiGravityStartUp = 5;

    // Get - Set
    public Player Player
    {
        get
        {
            if (abstractPlayer.realizedCreature != null)
            {
                if (abstractPlayer.realizedCreature is Player player)
                {
                    return player;
                }
            }
            return null;
        }
    }
    public EnergyCore RealizedCore
    {
        get
        {
            return (EnergyCore)this.realizedObject;
        }
    }
}

public static class AbstractEnergyCoreHooks
{
    public static void ApplyHooks()
    {
        AbstractEnergyCore.EnergyCoreType = new("EnergyCore", true);

        On.Player.Jump += Player_CoreBetaJump;
        IL.Weapon.Update += Weapon_PassThroughCore;

        On.PlayerGraphics.PlayerObjectLooker.HowInterestingIsThisObject += Interest_Exepection_Hook;
        On.Player.SpitOutOfShortCut += Player_MoveAbstractCore;
        On.AbstractCreature.ChangeRooms += AbstractPlayer_ChangeRooms;
        On.ItemSymbol.SymbolDataFromItem += ItemSymbol_SetCoreDataToNone;
        On.Player.SlugSlamConditions += Player_CoreSLAM;
        On.Creature.Die += Player_ConsideredDead;

        Plugin.Log("AbstractEnergyCoreHooks ApplyHooks Done !");
    }

    private static void Player_CoreBetaJump(On.Player.orig_Jump orig, Player self)
    {
        // Plugin.Log("Jump ? (Core)");
        if (AbstractEnergyCore.TryGetCore(self.abstractCreature, out var AEC) && AEC.IsBetaBoost)
        {
            if ((AEC.coreBoostLeft > 0 || AEC.CoreMaxBoost <= 0) && AEC.boostingCount >= 0)
            {
                EnergyCore energyCore = AEC.RealizedCore;
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
            
            AEC.boostingCount = -20;
            AEC.antiGravityCount = -10;
        }
        orig(self);
        // Plugin.Log("Jumped ! (Core)");
    }
    
    public static bool DoNotDeflect(Weapon weapon, SharedPhysics.CollisionResult result)
    {
        // Plugin.Log("Testing spear deflect with " + weapon + " by " + weapon.thrownBy + " hitting " + result.obj);
        if (weapon != null && 
            result.obj != null && weapon.thrownBy != null &&
            weapon.thrownBy is Player player && player != null && result.obj is EnergyCore core
            && player == core.player)
        {
            Plugin.Log("Allowed spear to go through core");
            return true;
        }
        return false;
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
    private static void Player_ConsideredDead(On.Creature.orig_Die orig, Creature self)
    {
        orig(self);
        if (AbstractEnergyCore.TryGetCore(self.abstractCreature, out var abstractEnergyCore) 
            && abstractEnergyCore.realizedObject != null)
        {
            abstractEnergyCore.RealizedCore.consideredDead = true;
        }
    }
    private static bool Player_CoreSLAM(On.Player.orig_SlugSlamConditions orig, Player self, PhysicalObject otherObject)
    {
        if (AbstractEnergyCore.TryGetCore(self.abstractCreature, out var abstractEnergyCore) 
            && abstractEnergyCore.realizedObject != null
            && abstractEnergyCore.RealizedCore.canSlam
            && otherObject is Creature creature)
        {
            foreach (Creature.Grasp grasp in self.grabbedBy)
            {
                if (grasp.pacifying || grasp.grabber == creature)
                {
                    return false;
                }
            }
            if (creature.grabbedBy.Exists(x => x.grabber == self)) { return false; }
            if (!ModManager.CoopAvailable 
                || otherObject is not Player
                || Custom.rainWorld.options.friendlyFire 
                || (Plugin.meadowEnabled 
                    && MeadowFunc.IsMeadowArena() 
                    && !MeadowFunc.IsCreatureFriendlies(self, creature)))
            {
                self.stun = Mathf.Max((int)(BTWFunc.FrameRate * 1.5), self.stun);
                abstractEnergyCore.coreBoostLeft = 0;
                abstractEnergyCore.RealizedCore.canSlam = false;
                return true;
            }
            return false;
        }
        return orig(self, otherObject);
    }
    private static IconSymbol.IconSymbolData? ItemSymbol_SetCoreDataToNone(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
    {
        if (item is AbstractEnergyCore) { return null; }
        return orig(item);
    }
    private static float Interest_Exepection_Hook(On.PlayerGraphics.PlayerObjectLooker.orig_HowInterestingIsThisObject orig, PlayerGraphics.PlayerObjectLooker self, PhysicalObject obj)
    {
        if (obj is EnergyCore)
        {
            return 0f;
        }
        return orig(self, obj);
    }
    private static void Player_MoveAbstractCore(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
    {
        orig(self, pos, newRoom, spitOutAllSticks);
        if (AbstractEnergyCore.TryGetCore(self.abstractCreature, out var abstractEnergyCore))
        {
            Plugin.Log("Changing room of core "+ abstractEnergyCore.ToString() +" of player " + self.abstractCreature.ToString() + " !");
            Plugin.Log("Changing room of abstract core "+ abstractEnergyCore.ToString() +" of player " + self.ToString() + " !");
            if (abstractEnergyCore.world != null 
                && self.abstractCreature.pos != null 
                && abstractEnergyCore.world.GetAbstractRoom(abstractEnergyCore.pos) != null
                && abstractEnergyCore.world.GetAbstractRoom(self.abstractCreature.pos) != null)
            {
                abstractEnergyCore.Abstractize(self.abstractCreature.pos);
                abstractEnergyCore.RealizeInRoom();
            }
            else
            {
                Plugin.Log("Something wrong happened to the core "+ abstractEnergyCore.ToString() +" of " + self.ToString() + ".");
            }
        }
    }
    private static void AbstractPlayer_ChangeRooms(On.AbstractCreature.orig_ChangeRooms orig, AbstractCreature self, WorldCoordinate newCoord)
    {
        orig(self, newCoord);
        if (AbstractEnergyCore.TryGetCore(self, out var abstractEnergyCore))
        {
            Plugin.Log("Changing room of abstract core "+ abstractEnergyCore.ToString() +" of player " + self.ToString() + " !");
            if (abstractEnergyCore.world != null 
                && newCoord != null 
                && abstractEnergyCore.world.GetAbstractRoom(abstractEnergyCore.pos) != null
                && abstractEnergyCore.world.GetAbstractRoom(newCoord) != null)
            {
                // Plugin.Log(newCoord);
                // Plugin.Log(abstractEnergyCore);
                // Plugin.Log(abstractEnergyCore.world);
                // Plugin.Log(abstractEnergyCore.pos);
                // Plugin.Log(abstractEnergyCore.world.GetAbstractRoom(abstractEnergyCore.pos));
                // Plugin.Log(abstractEnergyCore.world.GetAbstractRoom(newCoord));
                abstractEnergyCore.Abstractize(newCoord);
            }
            else
            {
                Plugin.Log("Something wrong happened to the core "+ abstractEnergyCore.ToString() +" of " + self.ToString() + ".");
            }
        }
    }
}