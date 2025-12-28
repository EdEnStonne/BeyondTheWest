using System;
using RainMeadow;
using System.Linq;
using BeyondTheWest.ArenaAddition;
using System.Collections.Generic;
using Unity;

namespace BeyondTheWest.MeadowCompat;

public static class MeadowFunc
{
    // Meadow Check
    public static bool IsMeadowLobby()
    {
        return OnlineManager.lobby is not null;
    }
    public static bool IsMeadowHost()
    {
        return !IsMeadowLobby() || (IsMeadowArena(out var arenaOnline) ? 
            arenaOnline.currentLobbyOwner == OnlineManager.mePlayer : OnlineManager.lobby.isOwner);
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
            BTWPlugin.logger.LogError(playerOpo.apo.ToString() + " is not a player !!!");
            return null;
        }
        return player;
    }
    public static StaticChargeManager GetOnlinePlayerStaticChargeManager(OnlineEntity playerOE)
    {
        Player player = GetPlayerFromOE(playerOE);
        if (player == null) { BTWPlugin.logger.LogError(playerOE.ToString() + " player is null !!!"); return null; }

        
        if (!(StaticChargeManager.TryGetManager(player.abstractCreature, out var SCM) && SCM.init))
        {
            BTWPlugin.logger.LogError("No StaticChargeManager detected on " + player.ToString());
            return null;
        }

        return SCM;
    }
    public static AbstractEnergyCore GetOnlinePlayerAbstractEnergyCore(OnlineEntity playerOE)
    {
        Player player = GetPlayerFromOE(playerOE);
        if (player == null) { BTWPlugin.logger.LogError(playerOE.ToString() + " player is null !!!"); return null; }

        if (!AbstractEnergyCore.TryGetCore(player.abstractCreature, out var AEC))
        {
            BTWPlugin.logger.LogError("No AbstractEnergyCore detected on " + player.ToString());
            
            return null;
        }

        return AEC;
    }
    public static EnergyCore GetOnlinePlayerEnergyCore(OnlineEntity playerOE)
    {
        AbstractEnergyCore AEC = GetOnlinePlayerAbstractEnergyCore(playerOE);
        if (AEC == null) { return null; }

        if (AEC.realizedObject == null || AEC.realizedObject is not EnergyCore core)
        {
            BTWPlugin.logger.LogError("No EnergyCore detected on " + playerOE.ToString());
            return null;
        }
        return core;
    }
    public static bool IsPlayerAlive(AbstractCreature abstractPlayer)
    {
        if (abstractPlayer?.realizedCreature?.State != null)
        {
            return abstractPlayer.realizedCreature.State.alive;
        }
        else if (abstractPlayer?.state != null)
        {
            return abstractPlayer.state.alive;
        }
        return false;
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
    public static bool IsObjectMine(AbstractPhysicalObject abstractPhysicalObject)
    {
        return IsObjectMine(abstractPhysicalObject, out _);
    }
    public static bool IsObjectMine(AbstractPhysicalObject abstractPhysicalObject, out OnlinePhysicalObject onlinePhysicalObject)
    {
        onlinePhysicalObject = null;
        if (!IsMeadowLobby())
        {
            return true;
        }
        onlinePhysicalObject = abstractPhysicalObject.GetOnlineObject();
        return onlinePhysicalObject == null || onlinePhysicalObject.isMine;
    }
    public static bool IsMine(AbstractPhysicalObject abstractPhysicalObject) // From PearlCat, works better than mine
    {
        return !IsMeadowLobby() || abstractPhysicalObject.IsLocal();
    }
    public static bool IsCreatureFriendlies(Creature creature, Creature friend)
    {
        return creature.FriendlyFireSafetyCandidate(friend);
        // return GameplayExtensions.FriendlyFireSafetyCandidate(creature, friend);
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
    public static int GetPlayersReadyForArena()
    {
        if (IsMeadowArena())
        {
            return ArenaHelpers.GetReadiedPlayerCount(OnlineManager.players);
        }
        return 0;
    }
    public static int GetPlayersInLobby()
    {
        if (IsMeadowLobby())
        {
            return OnlineManager.players.Count;
        }
        return 0;
    }
    public static bool ShouldGiveNewPoleTechToEveryone()
    {
        if (BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings))
        {
            return arenaSettings.Trailseeker_EveryoneCanPoleTech;
        }
        return BTWRemix.EveryoneCanPoleTech.Value;
    }

    // Arena extension
    public static void ResetDeathMessage(AbstractCreature abstractPlayer)
    {
        if (abstractPlayer.world?.game != null 
            && abstractPlayer.GetOnlineObject() is OnlinePhysicalObject onlinePhysicalObject 
            && onlinePhysicalObject != null)
        {
            var onlineHuds = abstractPlayer.world.game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();
            foreach (var onlineHud in onlineHuds)
            {
                onlineHud.killFeed.RemoveAll(x => x == onlinePhysicalObject.id);
            }
        }
    }
    public static void ResetSlugcatIcon(AbstractCreature abstractPlayer)
    {
        if (abstractPlayer.world?.game != null 
            && abstractPlayer.GetOnlineObject() is OnlinePhysicalObject onlinePhysicalObject 
            && onlinePhysicalObject != null)
        {
            var onlineHuds = abstractPlayer.world.game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();
            foreach (var onlineHud in onlineHuds)
            {
                if (onlineHud.abstractPlayer == abstractPlayer)
                {
                    onlineHud.playerDisplay.slugIcon.SetElementByName(onlineHud.playerDisplay.iconString);
                }
            }
        }
    }
    public static bool CheckIfShouldAddItemManagerOnline()
    {
        if (IsMeadowHost() && BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings))
        {
            return arenaSettings.ArenaItems_NewItemSpawningSystem;
        }
        return false;
    }
    public static void SetArenaItemSpawnSettings(ref ArenaItemSpawn.ArenaItemSpawnSetting arenaItemSpawnSetting)
    {
        if (IsMeadowArena() && BTWMeadowArenaSettings.TryGetSettings(out var settings))
        {
            arenaItemSpawnSetting.newSystem = settings.ArenaItems_NewItemSpawningSystem;
            arenaItemSpawnSetting.multiplier = settings.ArenaItems_ItemSpawnMultiplierCent / 100f;
            arenaItemSpawnSetting.multiplierPerPlayer = settings.ArenaItems_ItemSpawnMultiplierPerPlayersCent / 100f;
            arenaItemSpawnSetting.doScalePerPlayer = true;
            arenaItemSpawnSetting.randomItem = settings.ArenaItems_ItemSpawnRandom;
            arenaItemSpawnSetting.diversity = settings.ArenaItems_ItemSpawnDiversity;
        }
    }
    public static void ReviveOnlinePlayer(ArenaGameSession arenaGame, AbstractCreature abstractPlayer, int exit = 0)
    {
        if (!IsMeadowArena(out var arenaOnlineGameMode)) { BTWPlugin.logger.LogError($"uh the online arena is not here to revive on...?"); return; }
        BTWPlugin.Log($"Reviving [{abstractPlayer}] in room [{arenaGame.room}], pipe <{exit}>, in meadow lobby !");


        // abstractPlayer.Room.AddEntity(abstractPlayer);

        Room room = arenaGame.room;
        if (room == null) { BTWPlugin.logger.LogError($"uh the room is not here...?"); return; }
        OnlineCreature onlineCreature = abstractPlayer.GetOnlineCreature();
        if (onlineCreature == null) { BTWPlugin.logger.LogError($"uh the onlineCreature is not here...?"); return; }
        if (!onlineCreature.isMine) { BTWPlugin.logger.LogError($"uh the onlineCreature is not yours..."); return; }
        abstractPlayer.pos.room = arenaGame.game.world.GetAbstractRoom(0).index;
        abstractPlayer.pos.abstractNode = arenaGame.room.ShortcutLeadingToNode(exit).destNode;

        // arenaGame.game.world.GetResource().ApoEnteringWorld(abstractPlayer);
        // abstractPlayer.Room.GetResource()?.ApoEnteringRoom(abstractPlayer, abstractPlayer.pos);

        arenaGame.game.cameras[0].followAbstractCreature = abstractPlayer;

        if (abstractPlayer.GetOnlineObject(out var oe) && oe.TryGetData<SlugcatCustomization>(out var customization))
        {
            abstractPlayer.state = new PlayerState(abstractPlayer, 0, customization.playingAs, isGhost: false);
            BTWPlugin.Log($"Gave customization to slugcat !");  
        }
        else
        {
            RainMeadow.RainMeadow.Error("Could not get online owner for spawned player on BTW revive!");
            abstractPlayer.state = new PlayerState(abstractPlayer, 0, arenaGame.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arenaOnlineGameMode, OnlineManager.mePlayer)].playerClass, isGhost: false);
        }

        abstractPlayer.Realize();
        // onlineCreature.realized = true;
        BTWPlugin.Log($"Realized Creature !");
        ShortcutHandler.ShortCutVessel shortCutVessel = new(room.ShortcutLeadingToNode(exit).DestTile, 
            abstractPlayer.realizedCreature, arenaGame.game.world.GetAbstractRoom(0), 0)
        {
            entranceNode = abstractPlayer.pos.abstractNode,
            room = arenaGame.game.world.GetAbstractRoom(abstractPlayer.Room.name)
        };

        arenaGame.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
        BTWPlugin.Log($"Player [{abstractPlayer.realizedCreature}] fully revived !");
        // arenaGame.AddPlayer(abstractPlayer);
    }

    // Slug on back
    public static bool TryGetBottomPlayer(Player player, out Player bottom)
    {
        return RainMeadow.RainMeadow.GetBottomPlayer(player, null, out bottom);
    }
    public static bool HasSlugcatClassNameOnBack(Player player, string ClassName, out Player onback) // From Meadow
    {
        onback = null;
        int i = 25;

        List<Player> checked_players = new() { null };

        while (player != null && ((--i) >= 0)) // builtin hard limit to prevent infinite loops.
        {
            if (player.slugOnBack is null) break;
            player = player.slugOnBack.slugcat;
            if (checked_players.Contains(player)) break;
            checked_players.Add(player);

            if (player.SlugCatClass.ToString() == ClassName)
            {
                onback = player;
            }
        }

        return onback != null;
    }
    public static bool HasCoreOnBack(Player player, out Player onback)
    {
        return HasSlugcatClassNameOnBack(player, CoreFunc.CoreID, out onback);
    }
    public static bool HasSparkOnBack(Player player, out Player onback)
    {
        return HasSlugcatClassNameOnBack(player, SparkFunc.SparkID, out onback);
    }
}