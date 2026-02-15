using System;
using RainMeadow;
using System.Linq;
using BeyondTheWest.ArenaAddition;
using System.Collections.Generic;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;
using Unity;
using RWCustom;
using BeyondTheWest.Items;

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
            // BTWPlugin.logger.LogError(playerOpo.apo.ToString() + " is not a player !!!");
            return null;
        }
        return player;
    }
    public static StaticChargeManager GetOnlinePlayerStaticChargeManager(OnlineEntity playerOE)
    {
        Player player = GetPlayerFromOE(playerOE);
        if (player == null) {  return null; } //BTWPlugin.logger.LogError(playerOE.ToString() + " player is null !!!");

        
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
        if (player == null) { return null; } // BTWPlugin.logger.LogError(playerOE.ToString() + " player is null !!!");

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
    public static AbstractCreature GetPlayerFromOwnerInArena(OnlinePlayer onlinePlayer)
    {
        if (IsMeadowArena(out var arenaOnline))
        {
            Room room = (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.GetArenaGameSession?.room;
            if (room != null)
            {
                List<AbstractCreature> players = room.abstractRoom.creatures.FindAll(
                    x => x.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat
                        && !(ModManager.MSC && x.creatureTemplate.TopAncestor().type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC));
                
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].GetOnlineCreature() is OnlineCreature onlineCreature
                        && onlineCreature.owner == onlinePlayer)
                    {
                        return players[i];
                    }
                }
            }
        }
        return null;
    }
    public static bool HasOwner(AbstractCreature abstractCreature)
    {
        return abstractCreature?.GetOnlineCreature()?.owner is not null;
    }
    public static bool IsOwnerInSession(AbstractCreature abstractCreature)
    {
        if (abstractCreature.GetOnlineCreature() is OnlineCreature onlineCreature
            && onlineCreature.owner is OnlinePlayer onlinePlayer)
        {
            return IsOwnerInSession(onlinePlayer);
        }
        return false;
    }
    public static bool IsOwnerInSession(OnlinePlayer onlinePlayer)
    {
        if (onlinePlayer is not null)
        {
            int index = OnlineManager.lobby.playerAvatars.FindIndex(x => x.Key == onlinePlayer);
            if (index >= 0 && OnlineManager.lobby.playerAvatars[index].Value.type != (byte)OnlineEntity.EntityId.IdType.none)
            {
                return true;
            }
        }
        return false;
    }
    
    
    // Creature Check
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
    public static int ArenaCountdownTimerTotal()
    {
        if (IsMeadowArena(out var arenaOnline))
        {
            return arenaOnline.trackSetupTime * BTWFunc.FrameRate;
        }
        return RainMeadow.RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value * BTWFunc.FrameRate;
    }
    public static int ArenaCountdownTimerCurrent()
    {
        if (IsMeadowArena(out var arenaOnline))
        {
            return arenaOnline.setupTime * BTWFunc.FrameRate;
        }
        return RainMeadow.RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value * BTWFunc.FrameRate;
    }
    public static int GetPlayerArenaOnlineNumber(Player player)
    {
        if (IsMeadowArena(out var arenaOnline) 
            && player?.abstractCreature?.GetOnlineCreature()?.owner is OnlinePlayer onlinePlayer)
        {
            return ArenaHelpers.FindOnlinePlayerNumber(arenaOnline, onlinePlayer);
        }
        return player.abstractCreature.ID.number;
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
            arenaItemSpawnSetting.noSpears = settings.ArenaItems_NoSpear;
        }
    }
    public static void ReviveOnlinePlayer(ArenaGameSession arenaGame, AbstractCreature abstractPlayer, int exit = 0)
    {
        if (!IsMeadowArena(out var arenaOnlineGameMode)) { BTWPlugin.logger.LogError($"uh the online arena is not here to revive on...?"); return; }
        BTWPlugin.Log($"Reviving [{abstractPlayer}] in room [{arenaGame.room}], pipe <{exit}>, in meadow lobby !");


        // abstractPlayer.Room.AddEntity(abstractPlayer);

        Room room = arenaGame.room;
        if (room == null) { BTWPlugin.logger.LogError($"uh the room is not here...?"); return; }
        if (room.abstractRoom.GetResource() == null) { BTWPlugin.logger.LogError($"uh the online room is not here...?"); }
        OnlineCreature onlineCreature = abstractPlayer.GetOnlineCreature();
        if (onlineCreature == null) { BTWPlugin.logger.LogError($"uh the onlineCreature is not here...?"); return; }
        if (!onlineCreature.isMine) { BTWPlugin.logger.LogError($"uh the onlineCreature is not yours..."); return; }
        abstractPlayer.Move(room.ToWorldCoordinate(BTWFunc.ExitPos(arenaGame, exit)));
        abstractPlayer.pos.room = room.abstractRoom.index;
        abstractPlayer.pos.abstractNode = room.ShortcutLeadingToNode(exit).destNode;

        // arenaGame.game.world.GetResource().ApoEnteringWorld(abstractPlayer);

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
        onlineCreature.realized = true;
        room.abstractRoom.GetResource()?.ApoEnteringRoom(abstractPlayer, abstractPlayer.pos);
        BTWPlugin.Log($"Realized Creature !");
        
        ShortcutHandler.ShortCutVessel shortCutVessel = new(room.ShortcutLeadingToNode(exit).DestTile, 
            abstractPlayer.realizedCreature, arenaGame.game.world.GetAbstractRoom(0), 0)
        {
            entranceNode = abstractPlayer.pos.abstractNode,
            room = arenaGame.game.world.GetAbstractRoom(abstractPlayer.Room.name)
        };
        arenaGame.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);

        if ((abstractPlayer.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Night)
        {
            (abstractPlayer.realizedCreature as Player).slugcatStats.throwingSkill = 1;
        }
        if (ModManager.MSC)
        {
            if ((abstractPlayer.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
            {
                arenaGame.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.75f);
                arenaGame.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.5f);
            }

            if ((abstractPlayer.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
            {
                arenaGame.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, 0.75f);
                arenaGame.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.3f);
            }

            if ((abstractPlayer.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                arenaGame.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.5f);
                arenaGame.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, -1f);
            }

            if ((abstractPlayer.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
            {
                (abstractPlayer.realizedCreature as Player).slugcatStats.throwingSkill = 1;
            }

            if ((abstractPlayer.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                (abstractPlayer.realizedCreature as Player).slugcatStats.throwingSkill = arenaOnlineGameMode.painCatThrowingSkill;
            }


            if ((abstractPlayer.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                if (!arenaOnlineGameMode.sainot)
                {
                    (abstractPlayer.realizedCreature as Player).slugcatStats.throwingSkill = 0;
                }
                else
                {
                    (abstractPlayer.realizedCreature as Player).slugcatStats.throwingSkill = 1;

                }
            }
        }
        if (ModManager.Watcher && (abstractPlayer.realizedCreature as Player).SlugCatClass == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
        {
            (abstractPlayer.realizedCreature as Player).enterIntoCamoDuration = 40;
        }

        BTWPlugin.Log($"Player [{abstractPlayer.realizedCreature}] fully revived !");
        // arenaGame.AddPlayer(abstractPlayer);
    }
    public static void RemoveRestrictedItemsInArenaFromPool(ref ObjectDataPool itemPool)
    {
        if (IsMeadowArena(out var arenaOnline))
        {
            itemPool.RemoveFromPool(x => ArenaOnlineGameMode.blockList.Contains(x.objectData.objectType));
            if (!arenaOnline.enableBombs)
            {
                itemPool.RemoveFromPool(x => 
                    x.objectData.objectType == ObjectType.FirecrackerPlant
                    || x.objectData.objectType == ObjectType.ScavengerBomb
                    || x.objectData.objectType == ObjectType.PuffBall
                    || x.objectData.objectType == ObjectType.FlareBomb
                    || x.objectData.objectType == AbstractTristor.TristorType
                    || x.objectData.objectType == AbstractVoidCrystal.VoidCrystalType);
                if (ModManager.MSC)
                {
                    itemPool.RemoveFromPool(x => 
                        x.objectData.objectType == DLCSharedEnums.AbstractObjectType.SingularityBomb
                        || x.objectData.objectType == MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.FireEgg);
                }
            }
            if (!arenaOnline.enableBees)
            {
                itemPool.RemoveFromPool(x => x.objectData.objectType == ObjectType.SporePlant);
            }
        }
    }
    public static void HandleKarmaFlowerInArena(ArenaLives arenaLives)
    {
        if (IsMeadowArena(out var arenaOnline) && arenaOnline.IsStockArenaMode(out var stockArenaMode))
        {
            if (stockArenaMode.karmaFlowerGiveLife)
            {
                arenaLives.lifesleft++;
                arenaLives.DisplayLives();
            }
            if (stockArenaMode.karmaFlowerGiveProtection && !arenaLives.reinforced)
            {
                arenaLives.reinforced = true;
                arenaLives.DisplayLives();
            }
        }
        else if (!arenaLives.reinforced)
        {
            arenaLives.reinforced = true;
            arenaLives.DisplayLives();
        }
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