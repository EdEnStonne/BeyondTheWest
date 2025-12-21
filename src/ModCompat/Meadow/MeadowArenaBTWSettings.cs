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
    public static void AddSettings(ArenaMode arena)
    {
        if (meadowArenaBTWData.TryGetValue(arena, out _))
        {
            meadowArenaBTWData.Remove(arena);
        }
        meadowArenaBTWData.Add(arena, new(arena));
    }

    public BTWMeadowArenaSettings(ArenaMode arena) { this.arena = arena; }
    public ArenaMode arena;
    // TrailSeeker
    public bool Trailseeker_EveryoneCanPoleTech = BTWRemix.MeadowEveryoneCanPoleTech.Value;
    public int Trailseeker_PoleClimbBonus = BTWRemix.MeadowTrailseekerPoleClimbBonus.Value;
    public int Trailseeker_MaxWallClimb = BTWRemix.MeadowTrailseekerMaxWallClimb.Value;
    public int Trailseeker_WallGripTimer = BTWRemix.MeadowTrailseekerWallGripTimer.Value;

    // Core
    public int Core_RegenEnergy = BTWRemix.MeadowCoreRegenEnergy.Value;
    public int Core_MaxEnergy = BTWRemix.MeadowCoreMaxEnergy.Value;
    public int Core_MaxLeap = BTWRemix.MeadowCoreMaxLeap.Value;
    public int Core_AntiGravityCent = BTWRemix.MeadowCoreAntiGravityCent.Value;
    public int Core_OxygenEnergyUsage = BTWRemix.MeadowCoreOxygenEnergyUsage.Value;
    public bool Core_Shockwave = BTWRemix.MeadowCoreShockwave.Value;
    
    // Spark
    public int Spark_MaxCharge = BTWRemix.MeadowSparkMaxCharge.Value;
    public int Spark_AdditionnalOvercharge = BTWRemix.MeadowSparkAdditionnalOvercharge.Value;
    public int Spark_ChargeRegenerationMult = BTWRemix.MeadowSparkChargeRegenerationMult.Value;
    public int Spark_MaxElectricBounce = BTWRemix.MeadowSparkMaxElectricBounce.Value;
    public bool Spark_DoDischargeDamage = BTWRemix.MeadowSparkDoDischargeDamage.Value;
    public bool Spark_RiskyOvercharge = BTWRemix.MeadowSparkRiskyOvercharge.Value;
    public bool Spark_DeadlyOvercharge = BTWRemix.MeadowSparkDeadlyOvercharge.Value;
    
    // Arena Lives
    public int ArenaLives_AdditionalReviveTime = BTWRemix.MeadowArenaLivesAdditionalReviveTime.Value;
    public int ArenaLives_Amount = BTWRemix.MeadowArenaLivesAmount.Value;
    public int ArenaLives_ReviveTime = BTWRemix.MeadowArenaLivesReviveTime.Value;
    public bool ArenaLives_BlockWin = BTWRemix.MeadowArenaLivesBlockWin.Value;
    public bool ArenaLives_ReviveFromAbyss = false; //BTWRemix.MeadowArenaLivesReviveFromAbyss.Value;
    public bool ArenaLives_Strict = BTWRemix.MeadowArenaLivesStrict.Value;
    public int ArenaLives_RespawnShieldDuration = BTWRemix.MeadowArenaLivesRespawnShieldDuration.Value;
    
    // Arena Item Spawn 
    public bool ArenaItems_NewItemSpawningSystem = BTWRemix.MeadowNewItemSpawningSystem.Value;
    public int ArenaItems_ItemSpawnMultiplierCent = BTWRemix.MeadowItemSpawnMultiplierCent.Value;
    public int ArenaItems_ItemSpawnMultiplierPerPlayersCent = BTWRemix.MeadowItemSpawnMultiplierPerPlayersCent.Value;
    public bool ArenaItems_ItemSpawnDiversity = BTWRemix.MeadowItemSpawnDiversity.Value;
    public bool ArenaItems_ItemSpawnRandom = BTWRemix.MeadowItemSpawnRandom.Value;
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
            new Hook(typeof(RainMeadow.UI.Pages.ArenaMainLobbyPage).GetMethod("ShouldOpenSlugcatAbilitiesTab"), ArenaMainLobbyPage_ShouldOpenSlugcatAbilitiesTab);
            On.Player.ctor += Player_AddArenaLivesFromSettings;
            new Hook(typeof(ArenaOnlineLobbyMenu).GetMethod(nameof(ArenaOnlineLobbyMenu.ShutDownProcess)), ArenaMode_SaveData);
        }
        catch (Exception e)
        {
            Plugin.logger.LogError(e);
        }
        Plugin.Log("BTWMeadowArenaSettingsHooks ApplyHooks Done !");
    }

    private static void ArenaDataHook()
    {
        Plugin.Log("Meadow BTW Arena Data starts");
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
            Plugin.Log("Meadow hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("Meadow BTW Arena Data ends");
    }
    private static void OnlineSlugcatAbilitiesInterface_AddAllSettings(Action<OnlineSlugcatAbilitiesInterface, string> orig, OnlineSlugcatAbilitiesInterface self, string painCatName)
    {
        orig(self, painCatName);
        ArenaMenu.BTWEssentialSettingsPage essentialSettingsPage = new(self.menu, self, new Vector2(0f, 30f), 300f);
        self.AddSettingsTab(essentialSettingsPage, "BTWESSSETTINGS");
        ArenaMenu.BTWAdditionalSettingsPage additionalSettingsPage = new(self.menu, self, new Vector2(0f, 28.5f), 300f);
        self.AddSettingsTab(additionalSettingsPage, "BTWADDSETTINGS");
        ArenaMenu.BTWArenaSettingsPage arenaSettingsPage = new(self.menu, self, new Vector2(0f, 28.5f), 300f);
        self.AddSettingsTab(arenaSettingsPage, "BTWARESETTINGS");
    }
    private static void ArenaMode_AddData(Action<ArenaMode, Lobby> orig, ArenaMode self, Lobby lobby)
    {
        orig(self, lobby);
        BTWMeadowArenaSettings.AddSettings(self);
    }
    private static bool ArenaMainLobbyPage_ShouldOpenSlugcatAbilitiesTab(Func<RainMeadow.UI.Pages.ArenaMainLobbyPage, bool> orig, RainMeadow.UI.Pages.ArenaMainLobbyPage self)
    {
        return true;
    }
    private static void Player_AddArenaLivesFromSettings(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (BTWMeadowArenaSettings.TryGetSettings(out var meadowArenaSettings))
        {
            if (meadowArenaSettings.ArenaLives_Amount > 0 && self.room != null)
            {
                ArenaLives arenaLives = new(
                    abstractCreature, 
                    meadowArenaSettings.ArenaLives_Amount,
                    meadowArenaSettings.ArenaLives_ReviveTime * BTWFunc.FrameRate,
                    meadowArenaSettings.ArenaLives_AdditionalReviveTime * BTWFunc.FrameRate,
                    meadowArenaSettings.ArenaLives_BlockWin, !abstractCreature.IsLocal());
                self.room.AddObject( arenaLives );
            }
        }
    }
    private static void ArenaMode_SaveData(Action<ArenaOnlineLobbyMenu> orig, ArenaOnlineLobbyMenu self)
    {
        orig(self);
        BTWRemix.instance._SaveConfigFile();
    }
}