using UnityEngine;
using System;
using RainMeadow;
using BeyondTheWest.MSCCompat;
using BeyondTheWest.ArenaAddition;
using System.Collections.Generic;
using BeyondTheWest.MeadowCompat.Data;

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
        OnlineCreature onlinePlayer = kicker?.abstractCreature?.GetOnlineCreature();
        Creature target = chuckHit?.owner as Creature;
        if (onlinePlayer == null || target == null) { return; }

        OnlineCreature onlineTarget = target.abstractCreature?.GetOnlineCreature();
        if (onlineTarget == null || chuckHit == null)  { return; }

        byte chuckIndex = (byte)chuckHit.index;;

        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.PoleKickManager_Kick)).CreateDelegate(
                typeof(Action<RPCEvent, OnlineCreature, OnlineCreature, byte, Vector2, byte>)),
                onlinePlayer, onlineTarget, chuckIndex, knockback, 
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
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCOnce(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.Core_Boost)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject, byte>)), onlineCreature, 
                pow
        );
    }
    public static void CoreMeadow_ShockwaveRPC(AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.Core_Shockwave)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
        );
    }
    public static void CoreMeadow_ExplodeRPC(AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.Core_Explode)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
        );
    }
    public static void CoreMeadow_PopRPC(AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCOnce(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.Core_Pop)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
        );
    }
    public static void CoreMeadow_DisableRPC(AbstractEnergyCore abstractEnergyCore)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        if (onlineCreature == null) { return; }

        OnlinePlayer onlinePlayer = onlineCreature.owner;
        onlinePlayer.InvokeRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.Core_Disable)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
        );
    }
    public static void CoreMeadow_OxygenGiveRPC(AbstractEnergyCore abstractEnergyCore, Player target)
    {
        OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
        OnlineCreature targetOnlineCreature = target.abstractCreature.GetOnlineCreature();
        if (onlineCreature == null || targetOnlineCreature == null) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPCOnce(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.Core_GaveOxygenToOthers)).CreateDelegate(
                typeof(Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject>)), onlineCreature,
                targetOnlineCreature
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
                staticChargeManager.dischargeCooldown = 10;
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
        if (electricExplosion == null || electricExplosion.room == null) { return; }

        RoomSession roomSession = electricExplosion.room.abstractRoom.GetResource();
        if (roomSession == null) { return; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.Spark_ElectricExplosionSync)).CreateDelegate(
                typeof(Action<RPCEvent, RoomSession, Vector2, byte, byte, byte>)),
                roomSession, electricExplosion.pos, 
                (byte)electricExplosion.lifeTime, (byte)electricExplosion.rad, (byte)(electricExplosion.backgroundNoise * 100f)
        );
    }
    public static void SparMeadow_ShockCreatureRPC(
        Creature target, BodyChunk closestBodyChunk, PhysicalObject sourceObject, 
        Creature killTagHolder, float killTagHolderDmgFactor, float damage, float stun, 
        Color color, bool doSpams = false)
    {
        if (target == null || target.abstractCreature == null) { return; }
        OnlineCreature onlineTarget = target.abstractCreature.GetOnlineCreature();
        if (onlineTarget == null || onlineTarget.owner == null) { return; }

        byte chuckIndex = 0;
        if (closestBodyChunk != null) { chuckIndex = (byte)closestBodyChunk.index; }

        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.Spark_ElectricExplosionHit)).CreateDelegate(
                typeof(Action<RPCEvent, OnlineCreature, byte, OnlinePhysicalObject, OnlineCreature, byte, ushort, ushort, Color, bool>)),
                onlineTarget, chuckIndex, sourceObject?.abstractPhysicalObject?.GetOnlineObject(), 
                killTagHolder?.abstractCreature?.GetOnlineCreature(), (byte)(killTagHolderDmgFactor * 100f),
                (ushort)(damage * 100f), (ushort)stun, color, doSpams
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

        if (lightnightArc is LightingArc arc)
        {
            if (arc.from?.owner == null || arc.target?.owner == null) { 
                
            }
            else
            {
                if (arc.from.owner is not Creature from || arc.target.owner  is not Creature target) { return; }

                OnlineCreature onlineFrom = from.abstractCreature.GetOnlineCreature();
                OnlineCreature onlineTarget = target.abstractCreature.GetOnlineCreature();
                if (onlineFrom == null || onlineTarget == null) { return; }

                MeadowRPCs.InvokeAllOtherPlayerWithRPCOnce(
                typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.MSCCompat_Lightning)).CreateDelegate(
                    typeof(Action<RPCEvent, OnlineCreature, OnlineCreature, byte, byte, byte, Color>)),
                    onlineFrom, onlineTarget, (byte)(arc.width / 100f), (byte)(arc.intensity / 100f), (byte)arc.lifeTime, arc.color
                );
            }
            
        }
    }

    // Arena Additions
    public static void BTWArena_RPCArenaForcedDeathEffect(ArenaForcedDeath forcedDeath)
    {
        if (forcedDeath == null || forcedDeath.abstractTarget == null) { return; }
        OnlineCreature onlineCreature = forcedDeath.abstractTarget.GetOnlineCreature();

        if (onlineCreature == null) { return; }
        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
        typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWArenaAddition_AreneForcedDeathEffect)).CreateDelegate(
            typeof(Action<RPCEvent, OnlineCreature>)),
            onlineCreature
        );
    }
    public static void BTWArena_RPCArenaForcefieldAdded(ArenaShield shield)
    {
        if (shield == null || shield.target == null || !shield.target.abstractCreature.IsLocal()) { return; }
        OnlineCreature onlineCreature = shield.target.abstractCreature.GetOnlineCreature();

        if (onlineCreature == null) { return; }
        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
        typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWArenaAddition_AddArenaShield)).CreateDelegate(
            typeof(Action<RPCEvent, OnlineCreature, byte>)),
            onlineCreature, (byte)(shield.shieldTime / BTWFunc.FrameRate)
        );
    }
    public static void BTWArena_RPCArenaForcefieldBlock(ArenaShield shield)
    {
        if (shield == null || shield.target == null) { return; }
        OnlineCreature onlineCreature = shield.target.abstractCreature.GetOnlineCreature();

        if (onlineCreature == null) { return; }
        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
        typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWArenaAddition_BlockArenaShield)).CreateDelegate(
            typeof(Action<RPCEvent, OnlineCreature>)),
            onlineCreature
        );
    }
    public static void BTWArena_RPCArenaForcefieldDismiss(ArenaShield shield)
    {
        if (shield == null || shield.target == null || !shield.target.abstractCreature.IsLocal()) { return; }
        OnlineCreature onlineCreature = shield.target.abstractCreature.GetOnlineCreature();

        if (onlineCreature == null) { return; }
        MeadowRPCs.InvokeAllOtherPlayerWithRPC(
        typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWArenaAddition_DismissArenaShield)).CreateDelegate(
            typeof(Action<RPCEvent, OnlineCreature>)),
            onlineCreature
        );
    }
    
    public static void BTWArena_ArenaLivesInit(ArenaLives arenaLives)
    {
        if (arenaLives.abstractTarget == null) { return; }
        OnlineCreature onlineCreature = arenaLives.abstractTarget.GetOnlineCreature();
        if (onlineCreature == null) { return; }

        bool IsMine = arenaLives.abstractTarget.IsLocal();

        arenaLives.fake = !IsMine;

        if (MeadowFunc.IsMeadowLobby())
        {
            arenaLives.IsMeadowLobby = true;
            // arenaLives.canRespawn = false;
            if (arenaLives.target is Player player && BTWMeadowArenaSettings.TryGetSettings(out var meadowArenaSettings))
            {
                arenaLives.lifes = meadowArenaSettings.ArenaLives_Amount;
                arenaLives.lifesleft = meadowArenaSettings.ArenaLives_Amount;
                arenaLives.reviveAdditionnalTime = meadowArenaSettings.ArenaLives_AdditionalReviveTime * BTWFunc.FrameRate;
                arenaLives.blockArenaOut = meadowArenaSettings.ArenaLives_BlockWin;
                arenaLives.reviveTime = meadowArenaSettings.ArenaLives_ReviveTime * BTWFunc.FrameRate;
                arenaLives.enforceAfterReachingZero = meadowArenaSettings.ArenaLives_Strict;
                arenaLives.shieldTime = meadowArenaSettings.ArenaLives_RespawnShieldDuration * BTWFunc.FrameRate;
            }
            if (IsMine && !arenaLives.fake)
            {
                if (!onlineCreature.TryGetData<Data.OnlineArenaLivesData>(out _))
                {
                    onlineCreature.AddData(new Data.OnlineArenaLivesData());
                }
                MeadowRPCs.InvokeAllOtherPlayerWithRPCOnce(
                    typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWArenaAddition_AddArenaLifes)).CreateDelegate(
                        typeof(Action<RPCEvent, OnlineCreature>)),
                        onlineCreature
                );
            }
        }
    }    
    public static void BTWArena_RPCArenaLivesDestroy(ArenaLives arenaLives)
    {
        if (arenaLives.abstractTarget == null) { return; }
        OnlineCreature onlineCreature = arenaLives.abstractTarget.GetOnlineCreature();
        if (onlineCreature == null) { return; }

        if (!arenaLives.fake)
        {
            MeadowRPCs.InvokeAllOtherPlayerWithRPCOnce(
                typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWArenaAddition_DestroyArenaLifes)).CreateDelegate(
                    typeof(Action<RPCEvent, OnlineCreature>)),
                    onlineCreature
            );
        }
    }
    public static void BTWArena_RPCAddItemSpawnerToRequested(OnlinePlayer onlinePlayer, ArenaItemSpawn itemSpawner)
    {
        if (itemSpawner == null || itemSpawner.room == null) { return; }

        RoomSession roomSession = itemSpawner.room.abstractRoom.GetResource();
        if (roomSession == null) { return; }

        onlinePlayer.InvokeRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWArenaAddition_AddItemSpawn)).CreateDelegate(
                typeof(Action<RPCEvent, RoomSession, Vector2, ushort, ushort, OnlineObjectDataList>)),
                roomSession, itemSpawner.pos, (ushort)Mathf.Clamp(itemSpawner.spawnTime, 0, ushort.MaxValue),
                (ushort)Mathf.Clamp(itemSpawner.spawnCount, 0, ushort.MaxValue), new OnlineObjectDataList(itemSpawner.objectList)
        );
    }
    public static void BTWArena_RPCAddItemSpawnerToAll(ArenaItemSpawn itemSpawner)
    {
        foreach (var player in OnlineManager.players)
        {
            if (!player.isMe)
            {
                BTWArena_RPCAddItemSpawnerToRequested(player, itemSpawner);
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

        arenaOnline.currentLobbyOwner.InvokeRPC(
            typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWArenaAddition_RequestAllItemSpawn)).CreateDelegate(
                typeof(Action<RPCEvent, RoomSession>)),
                roomSession
        );
    }
}