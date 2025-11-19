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
using Mono.Cecil.Cil;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest;
using MonoMod.RuntimeDetour;

public class MeadowCompat
{
    //---------- Objects
    public class OnlineStaticChargeManagerData : OnlineEntity.EntityData
    {
        [UsedImplicitly]
        public OnlineStaticChargeManagerData() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(entity);
        }

        //-------- State

        public class State : EntityDataState
        {
            //--------- Variables
            [OnlineFieldHalf]
            public float charge;

            //--------- ctor

            [UsedImplicitly]
            public State() { }
            public State(OnlineEntity onlineEntity)
            {
                if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
                {
                    return;
                }

                if (!SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
                {
                    return;
                }

                charge = SCM.Charge;
            }
            //--------- Functions
            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var SCM = GetFakePlayerStaticChargeManager(onlineEntity);
                if (SCM == null) { return; }

                SCM.charge = this.charge;
            }
            public override Type GetDataType()
            {
                return typeof(OnlineStaticChargeManagerData);
            }

        }
    }
    public class OnlineAbstractCoreData : OnlineEntity.EntityData
    {
        [UsedImplicitly]
        public OnlineAbstractCoreData() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(entity);
        }

        //-------- State

        public class State : EntityDataState
        {
            //--------- Variables
            [OnlineFieldHalf]
            public float energy;
            [OnlineField]
            public int boostingCount;
            [OnlineField]
            public int antiGravityCount;
            [OnlineField]
            public byte state;

            //--------- ctor

            [UsedImplicitly]
            public State() { }
            public State(OnlineEntity onlineEntity)
            {
                if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
                {
                    return;
                }

                if (!CoreFunc.cwtCore.TryGetValue(player.abstractCreature, out var AEC))
                {
                    return;
                }

                this.energy = AEC.energy;
                this.boostingCount = AEC.boostingCount;
                this.antiGravityCount = AEC.antiGravityCount;
                this.state = AEC.state;
            }
            //--------- Functions
            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var AEC = GetFakePlayerAbstractEnergyCore(onlineEntity);
                if (AEC == null) { return; }

                AEC.energy = this.energy;
                AEC.boostingCount = this.boostingCount;
                AEC.antiGravityCount = this.antiGravityCount;
                AEC.state = this.state;
            }
            public override Type GetDataType()
            {
                return typeof(OnlineAbstractCoreData);
            }

        }
    }


    //---------- Functions

    // Meadow Check
    public static bool IsMeadowLobby()
    {
        // Plugin.Log("Checking if in Meadow lobby");
        return OnlineManager.lobby is not null;
    }
    public static bool IsMeadowArena()
    {
        return IsMeadowArena(out _);
    }
    public static bool IsMeadowArena(out ArenaOnlineGameMode arenaOnlineGameMode)
    {
        arenaOnlineGameMode = null;
        return IsMeadowLobby() && RainMeadow.RainMeadow.isArenaMode(out arenaOnlineGameMode);
    }

    // Fake Player Check
    public static Player GetPlayerFromOE(OnlineEntity playerOE)
    {
        var playerOpo = playerOE as OnlinePhysicalObject;
        // Plugin.Log(playerOpo);

        if (playerOpo?.apo?.realizedObject is not Player player)
        {
            Plugin.logger.LogError(playerOpo.apo.ToString() + " is not a player !!!");
            return null;
        }
        return player;
    }
    public static SparkObject.StaticChargeManager GetFakePlayerStaticChargeManager(OnlineEntity playerOE)
    {
        Player player = GetPlayerFromOE(playerOE);
        if (player == null) { Plugin.logger.LogError(playerOE.ToString() + " player is null !!!"); return null; }

        
        if (!(SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM) && SCM.init))
        {
            Plugin.logger.LogError("No StaticChargeManager detected on " + player.ToString());
            return null;
        }

        return SCM;
    }
    public static CoreObject.AbstractEnergyCore GetFakePlayerAbstractEnergyCore(OnlineEntity playerOE)
    {
        Player player = GetPlayerFromOE(playerOE);
        if (player == null) { Plugin.logger.LogError(playerOE.ToString() + " player is null !!!"); return null; }

        if (!CoreFunc.cwtCore.TryGetValue(player.abstractCreature, out var AEC))
        {
            Plugin.logger.LogError("No AbstractEnergyCore detected on " + player.ToString());
            
            return null;
        }

        return AEC;
    }
    public static CoreObject.EnergyCore GetFakePlayerEnergyCore(OnlineEntity playerOE)
    {
        CoreObject.AbstractEnergyCore AEC = GetFakePlayerAbstractEnergyCore(playerOE);
        if (AEC == null) { return null; }

        if (AEC.realizedObject == null || AEC.realizedObject is not CoreObject.EnergyCore core)
        {
            Plugin.logger.LogError("No EnergyCore detected on " + playerOE.ToString());
            return null;
        }
        return core;
    }

    // Creature Check
    public static bool IsCreatureMine(AbstractCreature abstractCreature)
    {
        return IsCreatureMine(abstractCreature, out _);
    }
    public static bool IsCreatureMine(AbstractCreature abstractCreature, out OnlineCreature onlineCreature)
    {
        onlineCreature = null;
        if (!IsMeadowLobby())
        {
            return true;
        }
        onlineCreature = abstractCreature.GetOnlineCreature();
        return onlineCreature == null || onlineCreature.isMine;
    }
    public static bool IsMine(AbstractPhysicalObject abstractPhysicalObject) // From PearlCat, works better than mine
    {
        return !IsMeadowLobby() || abstractPhysicalObject.IsLocal();
    }
    public static bool IsCreatureFriendlies(Creature creature, Creature friend)
    {
        return StoryModeExtensions.FriendlyFireSafetyCandidate(creature, friend);
    }

    // Arena
    public static bool ShouldHoldFireFromOnlineArenaTimer()
    {
        if (IsMeadowArena(out ArenaOnlineGameMode arenaOnlineGameMode))
        {
            return arenaOnlineGameMode.externalArenaGameMode.HoldFireWhileTimerIsActive(arenaOnlineGameMode);
        }
        return false;
    }

    // RPCs
    [RPCMethod]
    public static void Spark_SparkExplosion(RPCEvent _, OnlinePhysicalObject playerOpo, short size, Vector2 position, byte sparks, byte volumeCent)
    {
        // Plugin.Log("Opening the RPC : " + playerOpo.ToString() + "/" + size.ToString() + "/" + position.ToString() + "/" + sparks.ToString() + "/" + volumeCent.ToString());
        var SCM = GetFakePlayerStaticChargeManager(playerOpo);
        if (SCM == null) { return; }
        if (SCM.active || !SCM.isMeadowFakePlayer) { return; }
        // Plugin.Log("Checking some stuff :" + SCM.ToString() + "/" + SCM.active + "/" + SCM.isMeadowFakePlayer);

        float volume = volumeCent;
        volume /= 100f;
        Player player = SCM.Player;
        Room room = SCM.Room;
        Color color = player.ShortCutColor();
        SparkObject.MakeSparkExplosion(room, size, position, sparks, player.bodyMode == Player.BodyModeIndex.Swimming, color);
        room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, position, 0.5f + Math.Min(1f, volume), UnityEngine.Random.Range(1.1f, 1.5f));
        room.PlaySound(SoundID.Bomb_Explode, position, volume / 2f, UnityEngine.Random.Range(1.75f, 2.25f));
        Plugin.Log("Fake player [" + SCM.Player.ToString() + "] did a spark explosion !");
    }
    [RPCMethod]
    public static void Spark_DischargeHit(RPCEvent _, OnlineCreature playerOpo, OnlineCreature targetOc, float damage)
    {
        var SCM = GetFakePlayerStaticChargeManager(playerOpo);
        AbstractCreature abstractCreature = targetOc.abstractCreature;
        if (SCM == null || abstractCreature == null) { return; }
        
        Creature target = abstractCreature.realizedCreature;
        if (SCM.active || !SCM.isMeadowFakePlayer || target == null) { return; }

        SCM.ShockCreatureEffect(target.mainBodyChunk, damage, false);
        
        Plugin.Log("Fake player [" + SCM.Player.ToString() + "] hit "+ target.ToString() +" !");
    }
    [RPCMethod]
    public static void Core_Boost(RPCEvent _, OnlinePhysicalObject playerOpo, byte pow)
    {
        var core = GetFakePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        core.Boost(pow, false);
        Plugin.Log("Fake player [" + core.player.ToString() + "] did a leap ! (and maybe a flip)");
    }
    [RPCMethod]
    public static void Core_Shockwave(RPCEvent _, OnlinePhysicalObject playerOpo)
    {
        var core = GetFakePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        core.ShockWave(false);
        Plugin.Log("Fake player [" + core.player.ToString() + "] did a shockwave !");
    }
    [RPCMethod]
    public static void Core_Explode(RPCEvent _, OnlinePhysicalObject playerOpo)
    {
        var core = GetFakePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        core.Explode(false);
        Plugin.Log("Fake player [" + core.player.ToString() + "] did an explosion ! (ouch)");
    }
    [RPCMethod]
    public static void Core_Pop(RPCEvent _, OnlinePhysicalObject playerOpo)
    {
        var core = GetFakePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        core.Pop(false);
        Plugin.Log("Fake player [" + core.player.ToString() + "] did a pop !");
    }
    [RPCMethod]
    public static void Core_Disable(RPCEvent _, OnlinePhysicalObject playerOpo)
    {
        var core = GetFakePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (!core.AEC.active || core.AEC.isMeadowFakePlayer) { return; }

        core.Disable();
        Plugin.Log("Fake player [" + core.player.ToString() + "] got disabled !");
    }
    public static void InvokeAllOtherPlayerWithRPC(Delegate del, params object[] args)
    {
        foreach (var player in OnlineManager.players)
        {
            if (!player.isMe)
            {
                player.InvokeRPC(del, args);
            }
        }
    }
    public static void InvokeAllOtherPlayerWithRPCOnce(Delegate del, params object[] args)
    {
        foreach (var player in OnlineManager.players)
        {
            if (!player.isMe)
            {
                player.InvokeOnceRPC(del, args);
            }
        }
    }

    // Trailseeker
    public static void WallClimbMeadow_Init(WallClimbObject.WallClimbManager wallClimbManager)
    {
        if (MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var meadowArenaSettings))
        {
            wallClimbManager.MaxWallClimb = meadowArenaSettings.Trailseeker_MaxWallClimb;
            wallClimbManager.MaxWallGripCount = meadowArenaSettings.Trailseeker_WallGripTimer * BTWFunc.FrameRate;
        }
    }
    public static void ModifiedTech_Init(TrailseekerFunc.ModifiedTech modifiedTech)
    {
        if (MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var settings))
        {
            modifiedTech.techEnabled = settings.Trailseeker_AlteredMovementTech;
            modifiedTech.poleBonus = settings.Trailseeker_PoleClimbBonus;
        }
    }

    // Core
    public static OnlineCreature CoreMeadow_OnlineCreature(CoreObject.AbstractEnergyCore abstractEnergyCore)
    {
        bool IsMeadowLobby = MeadowCompat.IsMeadowLobby();
        return IsMeadowLobby ? abstractEnergyCore.abstractPlayer.GetOnlineCreature() : null;
    }
    public static void CoreMeadow_Init(CoreObject.AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        bool IsMine = MeadowCompat.IsMine(abstractEnergyCore.abstractPlayer);

        abstractEnergyCore.active = IsMine;
        abstractEnergyCore.isMeadow = IsMeadowLobby() && onlineCreature != null;
        abstractEnergyCore.isMeadowFakePlayer = !IsMine && onlineCreature != null;
        
        if (MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var meadowArenaSettings))
        {
            abstractEnergyCore.CoreMaxEnergy = meadowArenaSettings.Core_MaxEnergy;
            abstractEnergyCore.CoreEnergyRecharge = meadowArenaSettings.Core_RegenEnergy;
            abstractEnergyCore.CoreOxygenEnergyUsage = meadowArenaSettings.Core_OxygenEnergyUsage;
            abstractEnergyCore.CoreAntiGravity = meadowArenaSettings.Core_AntiGravityCent / 100f;
            abstractEnergyCore.CoreMaxBoost = meadowArenaSettings.Core_MaxLeap;
            abstractEnergyCore.isShockwaveEnabled = meadowArenaSettings.Core_Shockwave;

            abstractEnergyCore.energy = abstractEnergyCore.CoreMaxEnergy;
            abstractEnergyCore.coreBoostLeft = abstractEnergyCore.CoreMaxBoost;
        }
        if (ShouldHoldFireFromOnlineArenaTimer())
        {
            abstractEnergyCore.isMeadowArenaTimerCountdown = true;
            Plugin.Log(abstractEnergyCore.abstractPlayer +" In Timer !");
        }
        if (IsMine && abstractEnergyCore.isMeadow)
        {
            onlineCreature.AddData(new OnlineAbstractCoreData());
        }
    }
    public static void CoreMeadow_Update(CoreObject.AbstractEnergyCore abstractEnergyCore)
    {
        OnlinePhysicalObject onlinePhysicalObject = abstractEnergyCore.GetOnlineObject();
        if (onlinePhysicalObject != null) { onlinePhysicalObject.Deregister(); }

        // if (abstractEnergyCore.realizedObject != null && !onlinePhysicalObject.realized)
        // {
        //     onlinePhysicalObject.realized = true;
        // }
        // else if (abstractEnergyCore.realizedObject == null && onlinePhysicalObject.realized)
        // {
        //     onlinePhysicalObject.realized = false;
        // }
    }
    public static void CoreMeadow_BoostRPC(CoreObject.AbstractEnergyCore abstractEnergyCore, byte pow)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        InvokeAllOtherPlayerWithRPCOnce(
            typeof(MeadowCompat).GetMethod(nameof(Core_Boost)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject, byte>)), onlineCreature, 
                pow
        );
    }
    public static void CoreMeadow_ShockwaveRPC(CoreObject.AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        InvokeAllOtherPlayerWithRPC(
            typeof(MeadowCompat).GetMethod(nameof(Core_Shockwave)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
        );
    }
    public static void CoreMeadow_ExplodeRPC(CoreObject.AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        InvokeAllOtherPlayerWithRPC(
            typeof(MeadowCompat).GetMethod(nameof(Core_Explode)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
        );
    }
    public static void CoreMeadow_PopRPC(CoreObject.AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        InvokeAllOtherPlayerWithRPCOnce(
            typeof(MeadowCompat).GetMethod(nameof(Core_Pop)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
        );
    }
    public static void CoreMeadow_DisableRPC(CoreObject.AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        OnlinePlayer onlinePlayer = onlineCreature.owner;
        onlinePlayer.InvokeRPC(
            typeof(MeadowCompat).GetMethod(nameof(Core_Disable)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
        );
    }

    // Spark
    public static OnlineCreature SparkMeadow_OnlineCreature(SparkObject.StaticChargeManager staticChargeManager)
    {
        bool IsMeadowLobby = MeadowCompat.IsMeadowLobby();
        return IsMeadowLobby ? staticChargeManager.AbstractPlayer.GetOnlineCreature() : null;
    }
    public static void SparkMeadow_Init(SparkObject.StaticChargeManager staticChargeManager)
    {
        bool IsMine = !Plugin.meadowEnabled || MeadowCompat.IsMine(staticChargeManager.AbstractPlayer);
        bool IsMeadowLobby = Plugin.meadowEnabled && MeadowCompat.IsMeadowLobby();
        OnlineCreature onlineCreature = SparkMeadow_OnlineCreature(staticChargeManager);

        staticChargeManager.particles = true;
        staticChargeManager.active = !staticChargeManager.Player.dead && IsMine;
        staticChargeManager.isMeadow = IsMeadowLobby && onlineCreature != null;
        staticChargeManager.isMeadowFakePlayer = !IsMine && onlineCreature != null;

        if (IsMeadowArena())
        {
            staticChargeManager.isMeadowArena = true;
            if (MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var meadowArenaSettings))
            {
                staticChargeManager.FullECharge = meadowArenaSettings.Spark_MaxCharge;
                staticChargeManager.MaxECharge = meadowArenaSettings.Spark_MaxCharge + meadowArenaSettings.Spark_AdditionnalOvercharge;
                staticChargeManager.RechargeMult = meadowArenaSettings.Spark_ChargeRegenerationMult;
                staticChargeManager.MaxEBounce = meadowArenaSettings.Spark_MaxElectricBounce;
                staticChargeManager.DoDischargeDamagePlayers = meadowArenaSettings.Spark_DoDischargeDamage;
                staticChargeManager.RiskyOvercharge = meadowArenaSettings.Spark_RiskyOvercharge;
                staticChargeManager.DeathOvercharge = meadowArenaSettings.Spark_DeadlyOvercharge;

                staticChargeManager.eBounceLeft = staticChargeManager.MaxEBounce;
            }
            if (ShouldHoldFireFromOnlineArenaTimer())
            {
                staticChargeManager.dischargeCooldown = 10;
                staticChargeManager.isMeadowArenaTimerCountdown = true;
                Plugin.Log(staticChargeManager.AbstractPlayer + " In Timer !");
            }
        }
        if (IsMine && staticChargeManager.isMeadow)
        {
            onlineCreature.AddData(new OnlineStaticChargeManagerData());
        }
    }
    public static void SparkMeadow_DischargeRPC(SparkObject.StaticChargeManager staticChargeManager, short size, Vector2 position, byte sparks, byte volumeCent)
    {
        OnlineCreature onlineCreature = SparkMeadow_OnlineCreature(staticChargeManager);
        if (onlineCreature == null) { return; }

        InvokeAllOtherPlayerWithRPC(
            typeof(MeadowCompat).GetMethod(nameof(Spark_SparkExplosion)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject, short, Vector2, byte, byte>)), onlineCreature, 
                size, position, sparks, volumeCent
        );
    }
    public static void SparkMeadow_DischargeHitRPC(SparkObject.StaticChargeManager staticChargeManager, Creature creatureHit, float damage)
    {
        OnlineCreature onlineSpark = SparkMeadow_OnlineCreature(staticChargeManager);
        OnlineCreature onlineTarget = creatureHit.abstractCreature.GetOnlineCreature();
        if (onlineSpark == null || onlineTarget == null) { return; }

        InvokeAllOtherPlayerWithRPC(
            typeof(MeadowCompat).GetMethod(nameof(Spark_DischargeHit)).CreateDelegate(
                typeof(Action<RPCEvent, OnlineCreature, OnlineCreature, float>)), 
                onlineSpark, onlineTarget, damage
        );
    }
    //----------- Hooks
    public static void ApplyHooks()
    {
        foreach (var gamemodeDict in OnlineGameMode.gamemodes)
        {
            Type gameModeType = gamemodeDict.Value;
            new Hook(gameModeType.GetMethod("ShouldRegisterAPO"), OnlineGameMode_DoNotRegister);
            new Hook(gameModeType.GetMethod("ShouldSyncAPOInWorld"), OnlineGameMode_DoNotSyncInWorld);
            new Hook(gameModeType.GetMethod("ShouldSyncAPOInRoom"), OnlineGameMode_DoNotSyncInRoom);
        }
        new Hook(typeof(WorldSession).GetMethod(nameof(WorldSession.ApoLeavingWorld)), WorldSession_DoNotRegisterExit);
        new Hook(typeof(RoomSession).GetMethod(nameof(RoomSession.ApoLeavingRoom)), RoomSession_DoNotRegisterExit);
    }

    // public static bool IsDeniedSyncedObjects(AbstractPhysicalObject abstractPhysicalObject)
    // {
    //     if (abstractPhysicalObject is CoreObject.AbstractEnergyCore)
    //     {
    //         return true;
    //     }
    //     return false;
    // }
    public static HashSet<AbstractPhysicalObject.AbstractObjectType> deniedSyncedObjects = new()
    {
        CoreObject.EnergyCoreType
    };
    
    private static bool OnlineGameMode_DoNotRegister(Func<OnlineGameMode, OnlineResource, AbstractPhysicalObject, bool> orig, OnlineGameMode self, OnlineResource resource, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            Plugin.Log(apo.ToString() + " shall not be replicated !");
            return false; 
        }
        return orig(self, resource, apo);
    }
    private static bool OnlineGameMode_DoNotSyncInWorld(Func<OnlineGameMode, WorldSession, AbstractPhysicalObject, bool> orig, OnlineGameMode self, WorldSession ws, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            Plugin.Log(apo.ToString() + " shall not be sync (world) !");
            return false; 
        }
        return orig(self, ws, apo);
    }
    private static bool OnlineGameMode_DoNotSyncInRoom(Func<OnlineGameMode, RoomSession, AbstractPhysicalObject, bool> orig, OnlineGameMode self, RoomSession rs, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            Plugin.Log(apo.ToString() + " shall not be sync (room) !");
            return false; 
        }
        return orig(self, rs, apo);
    }
    private static void WorldSession_DoNotRegisterExit(Action<WorldSession, AbstractPhysicalObject> orig, WorldSession self, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            Plugin.Log(apo.ToString() + " shall not be accounted in deletion (world) !");
            return; 
        }
        orig(self, apo);
    }
    private static void RoomSession_DoNotRegisterExit(Action<RoomSession, AbstractPhysicalObject> orig, RoomSession self, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            Plugin.Log(apo.ToString() + " shall not be accounted in deletion (room) !");
            return; 
        }
        orig(self, apo);
    }
}
