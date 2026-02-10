using System.Collections.Generic;
using UnityEngine;
using System;
using RainMeadow;
using BeyondTheWest;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Menu;
using ArenaBehaviors;
using BeyondTheWest.ArenaAddition;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Linq;
using RWCustom;

namespace BeyondTheWest.MeadowCompat.Gamemodes;

public partial class StockArenaMode : ExternalArenaGameMode
{
    public static ArenaSetup.GameTypeID StockArenaModeID = new("Stock Battle", register: false);
    public static bool IsStockArenaMode(ArenaMode arena, out StockArenaMode stockArenaMode)
    {
        stockArenaMode = null;
        if (arena.currentGameMode == StockArenaModeID.value)
        {
            stockArenaMode = arena.registeredGameModes.FirstOrDefault(x => x.Key == StockArenaModeID.value).Value as StockArenaMode;
            return true;
        }
        return false;
    }

    public override ArenaSetup.GameTypeID GetGameModeId 
    { 
        get
        {
            return StockArenaModeID; 
        }
        set { GetGameModeId = value; }
    }

    public bool IsPlayerReviving(ArenaGameSession arenaGame, AbstractCreature abstractPlayer)
    {
        if (arenaGame.SessionStillGoing 
            && (arenaGame.game?.world?.rainCycle == null 
                || arenaGame.game.world.rainCycle.TimeUntilRain > rainTimerToSuddentDeath)
            && ArenaLives.PlayerCountedAsAliveInArena(abstractPlayer))
        {
            return true;
        }
        return false;
    }
    public bool IsPlayerReviving(ArenaMode arena, AbstractCreature abstractPlayer)
    {
        return IsPlayerReviving(
            (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.GetArenaGameSession, 
            abstractPlayer);
    }

    public override bool IsExitsOpen(ArenaMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ExitManager self)
    {
        int playersStillStanding = self.gameSession.Players?.Count(player =>
            (player.realizedCreature != null && player.realizedCreature.State.alive)
            || IsPlayerReviving(self?.gameSession, player)) ?? 0;

        if (playersStillStanding == 1 && arena.arenaSittingOnlineOrder.Count > 1)
        {
            return true;
        }

        if (self.world.rainCycle.TimeUntilRain <= 100)
        {
            return true;
        }

        return orig(self);
    }
    public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
    {
        return false;
    }
    
    public override string TimerText()
    {
        return BTWFunc.Translate("Prepare for combat,") + " " + BTWFunc.Translate(PlayingAsText());
    }
    public override int SetTimer(ArenaMode arena)
    {
        return arena.setupTime = RainMeadow.RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
    }
    public override int TimerDirection(ArenaMode arena, int timer)
    {
        return --arena.setupTime;
    }
    public override int TimerDuration
    {
        get { return _timerDuration; }
        set { _timerDuration = value; }
    }
    private int _timerDuration;

    public override bool HoldFireWhileTimerIsActive(ArenaMode arena)
    {
        if (arena.setupTime > 0)
        {
            return arena.countdownInitiatedHoldFire = true;
        }
        else
        {
            return arena.countdownInitiatedHoldFire = false;
        }
    }
    public override void LandSpear(ArenaMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
    {
        aPlayer.AddSandboxScore(self.GameTypeSetup.spearHitScore);
    }
    public override string AddIcon(ArenaMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
    {

        if (owner.clientSettings.owner == OnlineManager.lobby.owner)
        {
            return "ChieftainA";
        }
        return base.AddIcon(arena, owner, customization, player);

    }

    public override Color IconColor(ArenaMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
    {
        if (owner.PlayerConsideredDead)
        {
            if (IsPlayerReviving(arena, owner.abstractPlayer))
            {
                return new Color(0.8f, 0.8f, 0.8f);
            }
            return new Color(0.2f, 0.2f, 0.2f);
        }
        if (arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
        {
            return Color.yellow;
        }

        return base.IconColor(arena, display, owner, customization, player);
    }
    public override Dialog AddGameModeInfo(ArenaMode arena, Menu.Menu menu)
    {
        return new DialogNotify(menu.LongTranslate("A free for all with a second chance, or more."), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
    }

    public override void Killing(ArenaMode arena, On.ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player killer, Creature killedCrit, int playerIndex)
    {
        if (killedCrit is Player killedPlayer && killedPlayer != killer)
        {
            BTWPlugin.Log($"Oh no ! Player [{killedPlayer}]<{ArenaLives.TryGetLives(killedPlayer.abstractCreature, out _)}><{(ArenaLives.TryGetLives(killedPlayer.abstractCreature, out var a1) ? a1.killChain : false)}> got killed by [{killer}]<{ArenaLives.TryGetLives(killer.abstractCreature, out _)}><{(ArenaLives.TryGetLives(killer.abstractCreature, out var a2) ? a2.killChain : false)}> !");
            
            if (killer.abstractCreature.GetOnlineCreature() is OnlineCreature onlineKiller
                && onlineKiller.owner is OnlinePlayer onlinePlayer)
            {
                onlinePlayer.InvokeRPC(GetKillCredit, onlineKiller);
            }

            if (ArenaLives.TryGetLives(killedPlayer.abstractCreature, out var killedArenaLives))
            {
                if (killedArenaLives.killChain >= 25)
                {
                    ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 44, true);
                }
                else if (killedArenaLives.killChain >= 15)
                {
                    ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 43, true);
                }
                else if (killedArenaLives.killChain >= 10)
                {
                    ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 42, true);
                }
                else if (killedArenaLives.killChain >= 5)
                {
                    ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 41, true);
                }
                else if (killedArenaLives.killChain == 0 && BTWFunc.Chance(0.05f))
                {
                    ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 45, true);
                }
                killedArenaLives.killChain = 0;
            }
        }
        base.Killing(arena, orig, self, killer, killedCrit, playerIndex);
    }
    [RPCMethod]
    public static void GetKillCredit(RPCEvent rpc, OnlineCreature onlineKiller)
    {
        if (onlineKiller?.owner == OnlineManager.mePlayer 
            && onlineKiller?.abstractCreature?.realizedCreature is Player killer
            && !killer.dead
            && MeadowFunc.IsMeadowArena(out var arenaOnline) 
            && arenaOnline.IsStockArenaMode(out var stockArenaMode)
            && ArenaLives.TryGetLives(killer.abstractCreature, out var arenaLives)) 
        { 
            arenaLives.killChain++;
            if (stockArenaMode.killGiveLife && arenaLives.killChain % stockArenaMode.killAmountForLife == 0)
            {
                arenaLives.lifesleft++;
                arenaLives.DisplayLives();
            }
            if (stockArenaMode.killGiveProtection 
                && arenaLives.killChain % stockArenaMode.killAmountForProtection == 0
                && !arenaLives.reinforced)
            {
                arenaLives.reinforced = true;
                arenaLives.DisplayLives();
            }
            BTWPlugin.Log($"Hell yeah, got kill credit <{arenaLives.killChain}> !");
        }
    }
}

public static class StockArenaModeHook
{
    public static void ApplyHooks()
    {
        new Hook(typeof(ArenaMode).GetConstructor(new[] { typeof(Lobby) }), SetUpNewGamemode);
        On.Player.ctor += Player_AddArenaLivesFromSettings;
    }

    private static void SetUpNewGamemode(Action<ArenaMode, Lobby> orig, ArenaMode self, Lobby lobby)
    {
        orig(self, lobby);
        self.AddExternalGameModes(StockArenaMode.StockArenaModeID, new StockArenaMode());
    }
    private static void Player_AddArenaLivesFromSettings(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        if (MeadowFunc.IsMeadowArena(out var arenaOnline) 
            && arenaOnline.IsStockArenaMode(out var stockArenaMode)
            && BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings)
        )
        {
            if (stockArenaMode.LivesDefaultAmount > 0 
                && self.room != null 
                && !ArenaLives.TryGetLives(abstractCreature, out _))
            {
                ArenaLives arenaLives = new(
                    abstractCreature, 
                    arenaSettings.arenaStockClientSettings.lives,
                    stockArenaMode.reviveTime * BTWFunc.FrameRate,
                    stockArenaMode.additionalReviveTime * BTWFunc.FrameRate,
                    stockArenaMode.blockWin, !abstractCreature.IsLocal())
                {
                    enforceAfterReachingZero = stockArenaMode.strictEnforceAfter0Lives,
                    shieldTime = stockArenaMode.respawnShieldToggle ? stockArenaMode.respawnShieldDuration * BTWFunc.FrameRate : 0
                };
                self.room.AddObject( arenaLives );
            }
        }
    }
}