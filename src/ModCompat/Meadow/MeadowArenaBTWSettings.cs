using UnityEngine;
using System;
using RainMeadow;
using BeyondTheWest;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Runtime.CompilerServices;
using MonoMod.RuntimeDetour;
using RainMeadow.UI.Components;
using RainMeadow.UI;
using System.Reflection;
using BeyondTheWest.ArenaAddition;
using BeyondTheWest.MeadowCompat.Gamemodes;

namespace BeyondTheWest.MeadowCompat;

public class BTWMeadowArenaSettings
{
    public static ConditionalWeakTable<ArenaMode, BTWMeadowArenaSettings> meadowArenaBTWData = new();
    public static bool TryGetSettings(out BTWMeadowArenaSettings arenaSettings)
    {
        arenaSettings = null;
        if (MeadowFunc.IsMeadowArena(out var arenaOnlineGameMode) 
            && meadowArenaBTWData.TryGetValue(arenaOnlineGameMode, out arenaSettings))
        {
            return true;
        }
        return false;
    }
    public static BTWMeadowArenaSettings GetSettings()
    {
        TryGetSettings(out BTWMeadowArenaSettings arenaSettings);
        return arenaSettings;
    }
    public static BTWMeadowArenaSettings AddSettings(ArenaMode arena)
    {
        if (meadowArenaBTWData.TryGetValue(arena, out _))
        {
            meadowArenaBTWData.Remove(arena);
        }
        BTWMeadowArenaSettings bTWMeadowArenaSettings = new(arena);
        meadowArenaBTWData.Add(arena, bTWMeadowArenaSettings);
        return bTWMeadowArenaSettings;
    }

    public BTWMeadowArenaSettings(ArenaMode arena) { this.arena = arena; }
    public ArenaMode arena;
    public ArenaStockClientSettings arenaStockClientSettings;

    // TrailSeeker
    public bool Trailseeker_EveryoneCanPoleTech = BTWRemix.MeadowEveryoneCanPoleTech.Value;
    // public int Trailseeker_PoleClimbBonus = BTWRemix.MeadowTrailseekerPoleClimbBonus.Value;
    // public int Trailseeker_MaxWallClimb = BTWRemix.MeadowTrailseekerMaxWallClimb.Value;
    public int Trailseeker_WallGripTimer = BTWRemix.MeadowTrailseekerWallGripTimer.Value;

    // Core
    // public int Core_RegenEnergy = BTWRemix.MeadowCoreRegenEnergy.Value;
    // public int Core_MaxEnergy = BTWRemix.MeadowCoreMaxEnergy.Value;
    public int Core_MaxLeap = BTWRemix.MeadowCoreMaxLeap.Value;
    // public int Core_AntiGravityCent = BTWRemix.MeadowCoreAntiGravityCent.Value;
    // public int Core_OxygenEnergyUsage = BTWRemix.MeadowCoreOxygenEnergyUsage.Value;
    public bool Core_Shockwave = BTWRemix.MeadowCoreShockwave.Value;
    
    // Spark
    // public int Spark_MaxCharge = BTWRemix.MeadowSparkMaxCharge.Value;
    // public int Spark_AdditionnalOvercharge = BTWRemix.MeadowSparkAdditionnalOvercharge.Value;
    // public int Spark_ChargeRegenerationMult = BTWRemix.MeadowSparkChargeRegenerationMult.Value;
    public int Spark_MaxElectricBounce = BTWRemix.MeadowSparkMaxElectricBounce.Value;
    public bool Spark_DoDischargeDamage = BTWRemix.MeadowSparkDoDischargeDamage.Value;
    public bool Spark_RiskyOvercharge = BTWRemix.MeadowSparkRiskyOvercharge.Value;
    public bool Spark_DeadlyOvercharge = BTWRemix.MeadowSparkDeadlyOvercharge.Value;
    
    // Arena Item Spawn 
    public bool ArenaItems_NewItemSpawningSystem = BTWRemix.MeadowNewItemSpawningSystem.Value;
    public int ArenaItems_ItemSpawnMultiplierCent = BTWRemix.MeadowItemSpawnMultiplierCent.Value;
    public int ArenaItems_ItemSpawnMultiplierPerPlayersCent = BTWRemix.MeadowItemSpawnMultiplierPerPlayersCent.Value;
    public bool ArenaItems_ItemSpawnDiversity = BTWRemix.MeadowItemSpawnDiversity.Value;
    public bool ArenaItems_ItemSpawnRandom = BTWRemix.MeadowItemSpawnRandom.Value;
    public bool ArenaItems_ItemRespawn = BTWRemix.MeadowItemRespawn.Value;
    public int ArenaItems_ItemRespawnTimer = BTWRemix.MeadowItemRespawnTimer.Value;
    public bool ArenaItems_CheckSpearCount = BTWRemix.MeadowItemCheckSpearCount.Value;
    public bool ArenaItems_CheckThrowableCount = BTWRemix.MeadowItemCheckThrowableCount.Value;
    public bool ArenaItems_CheckMiscellaneousCount = BTWRemix.MeadowItemCheckMiscellaneousCount.Value;
    public bool ArenaItems_NoSpear = BTWRemix.MeadowItemNoSpear.Value;
    
    // Arena Bonus
    public bool ArenaBonus_InstantDeath = BTWRemix.MeadowArenaInstantDeath.Value;
    public bool ArenaBonus_ExtraItemUses = BTWRemix.MeadowArenaExtraItemUses.Value;
}
public static class BTWMeadowArenaSettingsHooks
{
    public static void ApplyHooks()
    {
        try
        {
            ArenaDataHook();
            new Hook(typeof(OnlineSlugcatAbilitiesInterface).GetMethod(nameof(OnlineSlugcatAbilitiesInterface.AddAllSettings)), OnlineSlugcatAbilitiesInterface_AddAllSettings);
            new Hook(typeof(ArenaMode).GetConstructor(new[] { typeof(Lobby) }), ArenaMode_AddData);
            new Hook(typeof(ArenaMode).GetMethod(nameof(ArenaMode.AddClientData)), ArenaMode_AddClientData);
            new Hook(typeof(RainMeadow.UI.Pages.ArenaMainLobbyPage).GetMethod("ShouldOpenSlugcatAbilitiesTab"), ArenaMainLobbyPage_ShouldOpenSlugcatAbilitiesTab);
            new Hook(typeof(ArenaOnlineLobbyMenu).GetMethod(nameof(ArenaOnlineLobbyMenu.ShutDownProcess)), ArenaMode_SaveData);
            On.Creature.Violence += Player_InstantDeath;
        }
        catch (Exception e)
        {
            BTWPlugin.logger.LogError(e);
        }
        BTWPlugin.Log("BTWMeadowArenaSettingsHooks ApplyHooks Done !");
    }

    private static void Player_InstantDeath(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        if (self is Player player
            && !player.dead
            && BTWMeadowArenaSettings.TryGetSettings(out var settings)
            && settings.ArenaBonus_InstantDeath
            && !(ArenaShield.TryGetShield(player, out var shield) && shield.Shielding)
            && !player.room.updateList.Exists(x => x is ArenaForcedDeath death && death.target == player))
        {
            ArenaForcedDeath arenaForcedDeath = new(self.abstractCreature, 10, true, player.killTag);
            self.room.AddObject( arenaForcedDeath );
        }
    }

    private static void ArenaDataHook()
    {
        BTWPlugin.Log("Meadow BTW Arena Data starts");
        try
        {
            new Hook(typeof(Lobby).GetMethod("ActivateImpl", BindingFlags.NonPublic | BindingFlags.Instance), (Action<Lobby> orig, Lobby self) =>
            {
                orig(self);
                if (MeadowFunc.IsMeadowArena(out var arenaOnlineGameMode))
                {
                    OnlineManager.lobby.AddData(new Data.BTWArenaLobbyRessourceData());
                }
            });
            BTWPlugin.Log("Meadow hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("Meadow BTW Arena Data ends");
    }
    private static void OnlineSlugcatAbilitiesInterface_AddAllSettings(Action<OnlineSlugcatAbilitiesInterface, string> orig, OnlineSlugcatAbilitiesInterface self, string painCatName)
    {
        orig(self, painCatName);
        BTWMenu.BTWEssentialSettingsPage essentialSettingsPage = new(self.menu, self, new Vector2(0f, 30f), 300f);
        self.AddSettingsTab(essentialSettingsPage, "BTWESSSETTINGS");
        // BTWMenu.BTWAdditionalSettingsPage additionalSettingsPage = new(self.menu, self, new Vector2(0f, 28.5f), 300f);
        // self.AddSettingsTab(additionalSettingsPage, "BTWADDSETTINGS");
        BTWMenu.BTWArenaSettingsPage arenaSettingsPage = new(self.menu, self, new Vector2(0f, 27f), 300f);
        self.AddSettingsTab(arenaSettingsPage, "BTWARESETTINGS");
    }
    private static void ArenaMode_AddData(Action<ArenaMode, Lobby> orig, ArenaMode self, Lobby lobby)
    {
        orig(self, lobby);
        BTWMeadowArenaSettings settings = BTWMeadowArenaSettings.AddSettings(self);
        settings.arenaStockClientSettings = new();
    }
    private static void ArenaMode_AddClientData(Action<ArenaMode> orig, ArenaMode self)
    {
        orig(self);
        if (BTWMeadowArenaSettings.TryGetSettings(out var settings))
        {
            self.clientSettings.AddData(settings.arenaStockClientSettings);
        }
    }
    private static bool ArenaMainLobbyPage_ShouldOpenSlugcatAbilitiesTab(Func<RainMeadow.UI.Pages.ArenaMainLobbyPage, bool> orig, RainMeadow.UI.Pages.ArenaMainLobbyPage self)
    {
        return true;
    }
    private static void ArenaMode_SaveData(Action<ArenaOnlineLobbyMenu> orig, ArenaOnlineLobbyMenu self)
    {
        orig(self);
        BTWRemix.instance._SaveConfigFile();
    }
}