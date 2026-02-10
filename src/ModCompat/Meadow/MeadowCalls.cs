using UnityEngine;
using System;
using RainMeadow;
using BeyondTheWest.MSCCompat;
using BeyondTheWest.ArenaAddition;
using System.Collections.Generic;
using BeyondTheWest.MeadowCompat.Data;
using BeyondTheWest.Items;
using BepInEx;

namespace BeyondTheWest.MeadowCompat;
public static class MeadowCalls
{
    // Trailseeker
    public static void WallClimbMeadow_Init(WallClimbManager wallClimbManager)
    {
        if (BTWMeadowArenaSettings.TryGetSettings(out var meadowArenaSettings))
        {
            wallClimbManager.MaxWallClimb = meadowArenaSettings.Trailseeker_MaxWallClimb;
            wallClimbManager.MaxWallGripCount = meadowArenaSettings.Trailseeker_WallGripTimer * BTWFunc.FrameRate;
        }
    }
    public static void ModifiedTech_Init(ModifiedTechManager modifiedTech)
    {
        if (BTWMeadowArenaSettings.TryGetSettings(out var settings))
        {
            modifiedTech.poleBonus = settings.Trailseeker_PoleClimbBonus;
        }
    }
    public static void PoleKickManager_Init(PoleKickManager poleKickManager)
    {
        if (MeadowFunc.IsMeadowLobby() && poleKickManager.abstractPlayer.IsLocal())
        {
            OnlineCreature onlinePlayer = poleKickManager?.abstractPlayer?.GetOnlineCreature();
            if (onlinePlayer != null && !onlinePlayer.TryGetData<OnlinePoleKickManagerData>(out _))
            {
                poleKickManager.abstractPlayer.GetOnlineCreature()?.AddData(new Data.OnlinePoleKickManagerData());
            }
        }
    }
    public static void PoleKickManager_RPCKick(Player kicker, BodyChunk chuckHit, Vector2 knockback, float knockbackBonus)
    {
        if (kicker?.abstractCreature?.GetOnlineCreature() is not OnlineCreature onlineKicker) { return; }
        if (chuckHit?.owner is not Creature target) { return; }
        if (target.abstractCreature?.GetOnlineCreature()  is not OnlineCreature onlineTarget) { return; }
        if (target.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }
        if (chuckHit == null)  { return; }

        byte chuckIndex = (byte)chuckHit.index;

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.PoleKickManager_Kick,
                onlineKicker, onlineTarget, chuckIndex, knockback, 
                (byte)Mathf.Clamp(knockbackBonus * 100f, byte.MinValue, byte.MaxValue)
        );
    }

    // Core
    public static OnlineCreature CoreMeadow_OnlineCreature(AbstractEnergyCore abstractEnergyCore)
    {
        bool IsMeadowLobby = MeadowFunc.IsMeadowLobby();
        return IsMeadowLobby ? abstractEnergyCore.abstractPlayer.GetOnlineCreature() : null;
    }
    public static void CoreMeadow_Init(AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        bool IsMine = abstractEnergyCore.abstractPlayer.IsLocal();

        abstractEnergyCore.active = IsMine;
        abstractEnergyCore.isMeadow = MeadowFunc.IsMeadowLobby() && onlineCreature != null;
        abstractEnergyCore.isMeadowFakePlayer = !IsMine && onlineCreature != null;
        
        if (BTWMeadowArenaSettings.TryGetSettings(out var meadowArenaSettings))
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
        if (MeadowFunc.ShouldHoldFireFromOnlineArenaTimer())
        {
            abstractEnergyCore.isMeadowArenaTimerCountdown = true;
            BTWPlugin.Log(abstractEnergyCore.abstractPlayer +" In Timer !");
        }
        if (IsMine && abstractEnergyCore.isMeadow && !onlineCreature.TryGetData<OnlineAbstractCoreData>(out _))
        {
            onlineCreature.AddData(new Data.OnlineAbstractCoreData());
        }
    }
    public static void CoreMeadow_Update(AbstractEnergyCore abstractEnergyCore)
    {
        OnlinePhysicalObject onlinePhysicalObject = abstractEnergyCore.GetOnlineObject();
        onlinePhysicalObject?.Deregister();
    }
    public static void CoreMeadow_BoostRPC(AbstractEnergyCore abstractEnergyCore, byte pow)
    {
        if (CoreMeadow_OnlineCreature(abstractEnergyCore) is not OnlineCreature onlineCore) { return; }
        if (abstractEnergyCore?.Room?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCOnceInRoom(roomSession, MeadowRPCs.Core_Boost,
            onlineCore, pow
        );
    }
    public static void CoreMeadow_ShockwaveRPC(AbstractEnergyCore abstractEnergyCore)
    {
        if (CoreMeadow_OnlineCreature(abstractEnergyCore) is not OnlineCreature onlineCore) { return; }
        if (abstractEnergyCore?.Room?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.Core_Shockwave,
            onlineCore
        );
    }
    public static void CoreMeadow_ExplodeRPC(AbstractEnergyCore abstractEnergyCore)
    {
        if (CoreMeadow_OnlineCreature(abstractEnergyCore) is not OnlineCreature onlineCore) { return; }
        if (abstractEnergyCore?.Room?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.Core_Explode,
            onlineCore
        );
    }
    public static void CoreMeadow_PopRPC(AbstractEnergyCore abstractEnergyCore)
    {
        if (CoreMeadow_OnlineCreature(abstractEnergyCore) is not OnlineCreature onlineCore) { return; }
        if (abstractEnergyCore?.Room?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCOnceInRoom(roomSession, MeadowRPCs.Core_Pop,
            onlineCore
        );
    }
    public static void CoreMeadow_DisableRPC(AbstractEnergyCore abstractEnergyCore)
    {
        if (CoreMeadow_OnlineCreature(abstractEnergyCore) is not OnlineCreature onlineCore) { return; }
        if (abstractEnergyCore?.Room?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.Core_Disable,
            onlineCore
        );
    }
    public static void CoreMeadow_OxygenGiveRPC(AbstractEnergyCore abstractEnergyCore, Player target)
    {
        if (CoreMeadow_OnlineCreature(abstractEnergyCore) is not OnlineCreature onlineCore) { return; }
        if (target?.abstractCreature?.GetOnlineCreature() is not OnlineCreature onlineTarget) { return; }
        if (abstractEnergyCore?.Room?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.Core_GaveOxygenToOthers,
            onlineCore, onlineTarget
        );
    }

    // Spark
    public static OnlineCreature SparkMeadow_OnlineCreature(StaticChargeManager staticChargeManager)
    {
        bool IsMeadowLobby = MeadowFunc.IsMeadowLobby();
        return IsMeadowLobby ? staticChargeManager.AbstractPlayer.GetOnlineCreature() : null;
    }
    public static void SparkMeadow_Init(StaticChargeManager staticChargeManager)
    {
        bool IsMine = !BTWPlugin.meadowEnabled || staticChargeManager.AbstractPlayer.IsLocal();
        bool IsMeadowLobby = BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowLobby();
        OnlineCreature onlineCreature = SparkMeadow_OnlineCreature(staticChargeManager);

        staticChargeManager.particles = true;
        staticChargeManager.active = !staticChargeManager.Player.dead && IsMine;
        staticChargeManager.isMeadow = IsMeadowLobby && onlineCreature != null;
        staticChargeManager.isMeadowFakePlayer = !IsMine && onlineCreature != null;

        if (MeadowFunc.IsMeadowArena())
        {
            staticChargeManager.isMeadowArena = true;
            if (BTWMeadowArenaSettings.TryGetSettings(out var meadowArenaSettings))
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
            if (MeadowFunc.ShouldHoldFireFromOnlineArenaTimer())
            {
                // staticChargeManager.dischargeCooldown = 10;
                staticChargeManager.isMeadowArenaTimerCountdown = true;
                BTWPlugin.Log(staticChargeManager.AbstractPlayer + " In Timer !");
            }
        }
        if (IsMine && staticChargeManager.isMeadow && !onlineCreature.TryGetData<OnlineStaticChargeManagerData>(out _))
        {
            onlineCreature.AddData(new Data.OnlineStaticChargeManagerData());
        }
    }
    public static void SparMeadow_ElectricExplosionRPC(ElectricExplosion electricExplosion)
    {
        if (electricExplosion?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.Spark_ElectricExplosionSync,
                roomSession, electricExplosion.pos, 
                (byte)electricExplosion.lifeTime, (byte)electricExplosion.rad, (byte)(electricExplosion.backgroundNoise * 100f)
        );
    }
    public static void SparMeadow_ShockCreatureRPC(
        Creature target, BodyChunk closestBodyChunk, PhysicalObject sourceObject, 
        Creature killTagHolder, float killTagHolderDmgFactor, float damage, float stun, 
        Color color, bool doSpams = false)
    {
        if (target?.abstractCreature?.GetOnlineCreature() is not OnlineCreature onlineTarget) { return; }
        if (target?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }

        byte chuckIndex = 0;
        if (closestBodyChunk != null) { chuckIndex = (byte)closestBodyChunk.index; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.Spark_ElectricExplosionHit,
                onlineTarget, chuckIndex, sourceObject?.abstractPhysicalObject?.GetOnlineObject(), 
                killTagHolder?.abstractCreature?.GetOnlineCreature(), (byte)(killTagHolderDmgFactor * 100f),
                (ushort)(damage * 100f), (ushort)stun, color, doSpams
        );
    }
    public static void ElectricExplosion_SparkExplosionRPC(Room room, float size, 
        Vector2 position, byte sparks, float volume, bool underwater, Color color)
    {
        if (room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }
        
        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.Spark_SparkExplosion,
                roomSession, (short)Mathf.Clamp(size, 0, short.MaxValue), position,
                sparks, (byte)Mathf.Clamp(volume * 100f, 0, byte.MaxValue), underwater, color
        );
    }

    // BTWFunction
    public static void BTWFuncMeadow_RPCCustomKnockBack(PhysicalObject physicalObject, short chunkAffected, Vector2 force)
    {
        OnlinePhysicalObject onlinePhysicalObject = physicalObject?.abstractPhysicalObject?.GetOnlineObject();
        if (onlinePhysicalObject == null || force == Vector2.zero || physicalObject.abstractPhysicalObject.IsLocal()) { return; }

        OnlinePlayer onlinePlayer = onlinePhysicalObject.owner;
        onlinePlayer.InvokeRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWFunc_CustomKnockBack)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject, short, Vector2>)), 
                onlinePhysicalObject, chunkAffected, force
        );
    }

    // MSCCompat
    public static void MSCCompat_RPCSyncLightnightArc(UpdatableAndDeletable lightnightArc)
    {
        if (lightnightArc == null || !ModManager.MSC) { return; }
        if (lightnightArc?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }

        if (lightnightArc is LightingArc arc)
        {
            if (arc.from?.owner == null || arc.target?.owner == null) { 
                
            }
            else
            {
                if (arc.from.owner is not Creature from || arc.target.owner is not Creature target) { return; }
                if (from.abstractCreature?.GetOnlineCreature() is not OnlineCreature onlineFrom 
                    || target.abstractCreature?.GetOnlineCreature() is not OnlineCreature onlineTarget) { return; }

                MeadowRPCs.InvokeAllOtherPlayerWithRPCOnceInRoom(roomSession, MeadowRPCs.MSCCompat_Lightning,
                    onlineFrom, onlineTarget, (byte)(arc.width / 100f), (byte)(arc.intensity / 100f), (byte)arc.lifeTime, arc.color
                );
            }
            
        }
    }

    // Arena Additions
    public static void BTWArena_RPCArenaForcedDeathEffect(ArenaForcedDeath forcedDeath)
    {
        if (forcedDeath?.abstractTarget?.GetOnlineCreature() is not OnlineCreature onlineCreature) { return; }
        if (!onlineCreature.isMine) { return; }
        if (forcedDeath?.abstractTarget?.Room?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.BTWArenaAddition_AreneForcedDeathEffect,
            onlineCreature
        );
    }
    public static void BTWArena_RPCArenaForcefieldAdded(ArenaShield shield)
    {
        if (shield?.target?.abstractCreature?.GetOnlineCreature() is not OnlineCreature onlineCreature) { return; }
        if (!onlineCreature.isMine) { return; }
        if (shield?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }
        
        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.BTWArenaAddition_AddArenaShield,
            onlineCreature, (byte)(shield.shieldTime / BTWFunc.FrameRate)
        );
    }
    public static void BTWArena_RPCArenaForcefieldBlock(ArenaShield shield)
    {
        if (shield?.target?.abstractCreature?.GetOnlineCreature() is not OnlineCreature onlineCreature) { return; }
        if (shield?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.BTWArenaAddition_BlockArenaShield,
            onlineCreature
        );
    }
    public static void BTWArena_RPCArenaForcefieldDismiss(ArenaShield shield)
    {
        if (shield?.target?.abstractCreature?.GetOnlineCreature() is not OnlineCreature onlineCreature) { return; }
        if (!onlineCreature.isMine) { return; }
        if (shield?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.BTWArenaAddition_DismissArenaShield,
            onlineCreature
        );
    }
    
    public static void BTWArena_ArenaLivesInit(ArenaLives arenaLives)
    {
        if (arenaLives?.abstractTarget?.GetOnlineCreature() is not OnlineCreature onlineCreature) { return; }
        if (arenaLives?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }

        bool IsMine = arenaLives.abstractTarget.IsLocal();
        arenaLives.fake = !IsMine;

        if (MeadowFunc.IsMeadowLobby())
        {
            arenaLives.IsMeadowLobby = true;
            if (IsMine)
            {
                if (!onlineCreature.TryGetData<Data.OnlineArenaLivesData>(out _))
                {
                    onlineCreature.AddData(new Data.OnlineArenaLivesData());
                }
                MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.BTWArenaAddition_AddArenaLifes,
                    onlineCreature
                );
            }
        }
    }    
    public static void BTWArena_RPCArenaLivesDestroy(ArenaLives arenaLives)
    {
        if (arenaLives?.abstractTarget?.GetOnlineCreature() is not OnlineCreature onlineCreature) { return; }
        if (arenaLives?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }

        if (!arenaLives.fake)
        {
            MeadowRPCs.InvokeAllOtherPlayerWithRPCInRoom(roomSession, MeadowRPCs.BTWArenaAddition_DestroyArenaLifes,
                onlineCreature
            );
        }
    }
    public static void BTWArena_RPCAddItemSpawnerToRequested(OnlinePlayer onlinePlayer, ArenaItemSpawn itemSpawner)
    {
        if (itemSpawner?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }
        if (!roomSession.participants.Exists(x => x == onlinePlayer)) 
        { 
            BTWPlugin.LogError($"Trying to ping [{onlinePlayer}] when they're not even in the room [{roomSession}] !");
            return; 
        }

        onlinePlayer.InvokeRPC(MeadowRPCs.BTWArenaAddition_AddItemSpawn,
                roomSession, itemSpawner.pos, (ushort)Mathf.Clamp(itemSpawner.spawnTime, 0, ushort.MaxValue),
                (ushort)Mathf.Clamp(itemSpawner.spawnCount, 0, ushort.MaxValue), new OnlineObjectDataList(itemSpawner.objectList)
        );
    }
    public static void BTWArena_RPCAddItemSpawnerToAllInRoom(ArenaItemSpawn itemSpawner)
    {
        if (itemSpawner?.room?.abstractRoom?.GetResource() is not RoomSession roomSession) { return; }

        foreach (var participant in roomSession.participants)
        {
            if (!participant.isMe)
            {
                BTWArena_RPCAddItemSpawnerToRequested(participant, itemSpawner);
            }
        }
    }
    public static void BTWArena_RPCRequestItemSpawn(ArenaGameSession arena)
    {
        if (arena.room == null) { return; }
        if (!MeadowFunc.IsMeadowArena(out var arenaOnline)) { return; }
        if (MeadowFunc.IsMeadowHost()) { return; }

        RoomSession roomSession = arena.room.abstractRoom.GetResource();
        if (roomSession == null) { return; }

        arenaOnline.currentLobbyOwner.InvokeRPC(MeadowRPCs.BTWArenaAddition_RequestAllItemSpawn,
                roomSession
        );
    }
    public static void BTWArena_ArenaItemSpawnManagerInit(ArenaItemSpawnManager arenaItemSpawnManager)
    {
        if (BTWMeadowArenaSettings.TryGetSettings(out var meadowArenaSettings))
        {
            arenaItemSpawnManager.doRespawn = meadowArenaSettings.ArenaItems_ItemRespawn;
            arenaItemSpawnManager.respawnCount.Reset(meadowArenaSettings.ArenaItems_ItemRespawnTimer * BTWFunc.FrameRate);
            arenaItemSpawnManager.doCheckSpearsCount = meadowArenaSettings.ArenaItems_CheckSpearCount;
            arenaItemSpawnManager.doCheckThrowableCount = meadowArenaSettings.ArenaItems_CheckThrowableCount;
            arenaItemSpawnManager.doCheckMiscellaniousCount = meadowArenaSettings.ArenaItems_CheckMiscellaneousCount;
            arenaItemSpawnManager.noSpears = meadowArenaSettings.ArenaItems_NoSpear;
        }
        arenaItemSpawnManager.playersCount = MeadowFunc.GetPlayersInLobby();
    }
    
    public static void BTWStockArena_RequestLifeChange(OnlinePlayer onlinePlayer, int lives)
    {
        if (MeadowFunc.IsMeadowArena(out var arenaOnline) 
            && arenaOnline.IsStockArenaMode()
            && OnlineManager.lobby.isOwner)
        {
            onlinePlayer.InvokeRPC(MeadowRPCs.BTWStockArena_ChangeLifes, lives);
        }
    }

    // Tristors

    public static void BTWItems_AbstractTristorInit(AbstractTristor abstractTristor)
    {
        abstractTristor.isMeadowInit = true;
        bool IsMine = abstractTristor.IsLocal();
        OnlinePhysicalObject onlinePhysicalObject = abstractTristor.GetOnlineObject();

        if (MeadowFunc.IsMeadowLobby() && IsMine)
        {
            if (onlinePhysicalObject == null)
            {
                abstractTristor.isMeadowInit = false;
            }
            else if (!onlinePhysicalObject.TryGetData<OnlineTristorData>(out _))
            {
                onlinePhysicalObject.AddData(new OnlineTristorData());
            }
        }
    }

    // Void Crystal
    
    public static void BTWItems_AbstractVoidCrystalInit(AbstractVoidCrystal abstractVoidCrystal)
    {
        abstractVoidCrystal.isMeadowInit = true;
        bool IsMine = abstractVoidCrystal.IsLocal();
        OnlinePhysicalObject onlinePhysicalObject = abstractVoidCrystal.GetOnlineObject();

        if (MeadowFunc.IsMeadowLobby() && IsMine)
        {
            if (onlinePhysicalObject == null)
            {
                abstractVoidCrystal.isMeadowInit = false;
            }
            else if (!onlinePhysicalObject.TryGetData<OnlineVoidCrystal>(out _))
            {
                onlinePhysicalObject.AddData(new OnlineVoidCrystal());
            }
        }
    }

    // Void Spear
    public static void BTWItems_AbstractCrystalSpearInit(AbstractCrystalSpear abstractCrystalSpear)
    {
        abstractCrystalSpear.isMeadowInit = true;
        bool IsMine = abstractCrystalSpear.IsLocal();
        OnlinePhysicalObject onlinePhysicalObject = abstractCrystalSpear.GetOnlineObject();

        if (MeadowFunc.IsMeadowLobby() && IsMine)
        {
            if (onlinePhysicalObject == null)
            {
                abstractCrystalSpear.isMeadowInit = false;
            }
            // else if (!onlinePhysicalObject.TryGetData<OnlineCrystalSpear>(out _))
            // {
            //     onlinePhysicalObject.AddData(new OnlineCrystalSpear());
            // }
        }
    }
    public static void BTWItems_PopCrystalSpear(CrystalSpear crystalSpear)
    {
        if (crystalSpear?.abstractCrystalSpear?.GetOnlineObject() is OnlinePhysicalObject onlinePhysicalObject
            && crystalSpear.Local())
        {
            onlinePhysicalObject.BroadcastRPCInRoom(MeadowRPCs.BTWItems_CrystalSpearPop, 
                onlinePhysicalObject, crystalSpear.firstChunk.pos);
        }
    }
    public static void BTWItems_ExplodeCrystalSpear(CrystalSpear crystalSpear)
    {
        if (crystalSpear?.abstractCrystalSpear?.GetOnlineObject() is OnlinePhysicalObject onlinePhysicalObject
            && crystalSpear.Local())
        {
            onlinePhysicalObject.BroadcastRPCInRoom(MeadowRPCs.BTWItems_CrystalSpearExplode, 
                onlinePhysicalObject, crystalSpear.firstChunk.pos);
        }
    }

    // Void Spark
    public static void BTWItems_VoidSparkEnterRoom(VoidSpark voidSpark)
    {
        voidSpark.meadowInit = true;
        RoomSession roomSession = voidSpark.room?.abstractRoom?.GetResource();
        if (MeadowFunc.IsMeadowLobby() && roomSession is not null)
        {
            if (!roomSession.isAvailable || !roomSession.isActive) 
            {
                voidSpark.meadowInit = false;
            }
            else
            {
                if (OnlineVoidSpark.map.TryGetValue(voidSpark, out var ovs))
                {
                    ovs.EnterResource(roomSession);
                }
                else
                {
                    RainMeadow.RainMeadow.Debug($"{roomSession} - registering {voidSpark}");
                    ovs = OnlineVoidSpark.RegisterVoidSpark(voidSpark);
                    ovs.EnterResource(roomSession);
                }
            }
        }
    }
    public static void BTWItems_VoidSparkLeaveRoom(VoidSpark voidSpark)
    {
        RoomSession roomSession = voidSpark.room?.abstractRoom?.GetResource();
        if (MeadowFunc.IsMeadowLobby() && roomSession is not null)
        {
            if (roomSession.isAvailable && roomSession.isActive)
            {
                if (OnlineVoidSpark.map.TryGetValue(voidSpark, out var ovs))
                {
                    ovs.ExitResource(roomSession);
                }
                else
                {
                    RainMeadow.RainMeadow.Error($"Unregistered void spark leaving {roomSession} : {voidSpark}<{voidSpark.ID}> - {Environment.StackTrace}");
                }
            }
        }
    }
    public static void BTWItems_VoidSparkDissipate(VoidSpark voidSpark)
    {
        OnlineVoidSpark onlineVoidSpark = voidSpark.GetOnlineVoidSpark();
        if (MeadowFunc.IsMeadowLobby() && onlineVoidSpark != null && onlineVoidSpark.isMine)
        {
            onlineVoidSpark.BroadcastRPCInRoom(onlineVoidSpark.Dissipate, voidSpark.position);
        }
    }
    public static void BTWItems_VoidSparkExplode(VoidSpark voidSpark)
    {
        OnlineVoidSpark onlineVoidSpark = voidSpark.GetOnlineVoidSpark();
        if (MeadowFunc.IsMeadowLobby() && onlineVoidSpark != null && onlineVoidSpark.isMine)
        {
            onlineVoidSpark.BroadcastRPCInRoom(onlineVoidSpark.Explode, voidSpark.position);
        }
    }
    public static void BTWItems_VoidSparkHitSomething(VoidSpark voidSpark)
    {
        OnlineVoidSpark onlineVoidSpark = voidSpark.GetOnlineVoidSpark();
        OnlineCreature onlineKillTagHolder = voidSpark.killTagHolder?.GetOnlineCreature();
        OnlineEntity onlineTarget = null;
        if (voidSpark.target is PhysicalObject physicalObject)
        {
            onlineTarget = physicalObject.abstractPhysicalObject.GetOnlineObject();
        }
        if (MeadowFunc.IsMeadowLobby() && onlineTarget is not null)
        {
            if (onlineVoidSpark != null)
            {
                if (onlineVoidSpark.isMine)
                {
                    onlineVoidSpark.BroadcastRPCInRoom(OnlineVoidSpark.HitSomething, onlineVoidSpark,
                        onlineTarget, (ushort)(voidSpark.damage * 100), voidSpark.direction, voidSpark.lastPosition, onlineKillTagHolder);
                }
            }
            else
            {
               onlineVoidSpark.BroadcastRPCInRoom(OnlineVoidSpark.HitSomethingSparkless,
                    onlineTarget, (ushort)(voidSpark.damage * 100), voidSpark.direction, voidSpark.lastPosition, onlineKillTagHolder);
            }
        }
    }

}