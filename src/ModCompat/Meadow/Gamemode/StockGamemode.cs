// using System.Collections.Generic;
// using UnityEngine;
// using System;
// using RainMeadow;
// using BeyondTheWest;
// using MonoMod.RuntimeDetour;
// using MonoMod.Cil;
// using Mono.Cecil.Cil;
// using Menu;
// using ArenaBehaviors;
// using BeyondTheWest.ArenaAddition;
// using ArenaMode = RainMeadow.ArenaOnlineGameMode;
// using System.Linq;
// using RWCustom;
// using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
// using RainMeadow.UI;

// namespace BeyondTheWest.MeadowCompat.Gamemodes;

// public partial class StockArenaMode : ExternalArenaGameMode
// {
//     public static ArenaSetup.GameTypeID StockArenaModeID = new("Stock Battle", register: false);
//     public static bool IsStockArenaMode(ArenaMode arena, out StockArenaMode stockArenaMode)
//     {
//         stockArenaMode = null;
//         if (arena.currentGameMode == StockArenaModeID.value)
//         {
//             stockArenaMode = arena.registeredGameModes.FirstOrDefault(x => x.Key == StockArenaModeID.value).Value as StockArenaMode;
//             return true;
//         }
//         return false;
//     }

//     public override ArenaSetup.GameTypeID GetGameModeId 
//     { 
//         get
//         {
//             return StockArenaModeID; 
//         }
//     }

//     public bool IsPlayerReviving(ArenaGameSession arenaGame, AbstractCreature abstractPlayer)
//     {
//         if (arenaGame.SessionStillGoing 
//             && (arenaGame.game?.world?.rainCycle == null 
//                 || arenaGame.game.world.rainCycle.TimeUntilRain > rainTimerToSuddentDeath)
//             && ArenaLives.IsPlayerRevivingInArena(abstractPlayer)
//             && abstractPlayer.GetOnlineCreature() is OnlineCreature onlineCreature
//             && onlineCreature.owner is OnlinePlayer onlinePlayer
//             && MeadowFunc.IsOwnerInSession(onlinePlayer))
//         {
//             return true;
//         }
//         return false;
//     }
//     public bool IsPlayerReviving(ArenaMode arena, AbstractCreature abstractPlayer)
//     {
//         return IsPlayerReviving(
//             (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.GetArenaGameSession, 
//             abstractPlayer);
//     }

//     public override bool IsExitsOpen(ArenaMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ExitManager self)
//     {
//         // For next Meadow Update I suppose
//         if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Always)
//         {
//             return true;
//         }

//         if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Score)
//         {
//             return orig(self) || (self.gameSession?.arenaSitting?.players?.Any(p => p?.score >= arena.denScore) ?? false);
//         }

//         int playersStillStanding = self.gameSession.Players?.Count(player =>
//             player.realizedCreature != null && player.realizedCreature.State.alive)
//                 + (self.gameSession.game.world.rainCycle.TimeUntilRain > rainTimerToSuddentDeath 
//                     ? ArenaLives.AdditionalPlayerInArenaCount(self.gameSession) 
//                     : 0) 
//             ?? 0;

//         if (playersStillStanding == 1 && arena.arenaSittingOnlineOrder.Count > 1 && !arena.countdownInitiatedHoldFire)
//         {
//             return true;
//         }

//         if (self.world.rainCycle.TimeUntilRain <= 100)
//         {
//             return true;
//         }

//         return orig(self);
//     }
//     public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
//     {
//         return false;
//     }

//     public override string TimerText()
//     {
//         if (this.isTeamBattle && TeamBattleGamemode is TeamBattleMode teamBattleMode)
//         {
//             return teamBattleMode.TimerText();
//         }
//         return BTWFunc.Translate("Prepare for combat,") + " " + BTWFunc.Translate(PlayingAsText());
//     }
//     public override int SetTimer(ArenaMode arena)
//     {
//         return arena.setupTime = RainMeadow.RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
//     }
//     public override int TimerDirection(ArenaMode arena, int timer)
//     {
//         return --arena.setupTime;
//     }
//     public override int TimerDuration
//     {
//         get { return _timerDuration; }
//         set { _timerDuration = value; }
//     }
//     private int _timerDuration;

//     public override bool HoldFireWhileTimerIsActive(ArenaMode arena)
//     {
//         if (arena.setupTime > 0)
//         {
//             return arena.countdownInitiatedHoldFire = true;
//         }
//         else
//         {
//             return arena.countdownInitiatedHoldFire = false;
//         }
//     }
//     public override void LandSpear(ArenaMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
//     {
//         aPlayer.AddSandboxScore(self.GameTypeSetup.spearHitScore);
//     }
//     public override string AddIcon(ArenaMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
//     {
//        if (this.isTeamBattle && TeamBattleGamemode is TeamBattleMode teamBattleMode)
//         {
//             return teamBattleMode.AddIcon(arena, display, owner, customization, player);
//         }
//         if (owner.clientSettings.owner == OnlineManager.lobby.owner)
//         {
//             return "ChieftainA";
//         }
//         return base.AddIcon(arena, display, owner, customization, player);
//     }

//     public override Color IconColor(ArenaMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
//     {
//         if (owner.PlayerConsideredDead)
//         {
//             if (IsPlayerReviving(arena, owner.abstractPlayer))
//             {
//                 return new Color(0.8f, 0.8f, 0.8f);
//             }
//             return new Color(0.2f, 0.2f, 0.2f);
//         }
//         if (arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
//         {
//             return Color.yellow;
//         }

//         if (this.isTeamBattle) { return this.GetTeamBattleMode(arena).IconColor(arena, display, owner, customization, player); }
//         return base.IconColor(arena, display, owner, customization, player);
//     }

//     public override void Killing(ArenaMode arena, On.ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player killer, Creature killedCrit)
//     {
//         if (killedCrit is Player killedPlayer && killedPlayer != killer)
//         {
//             BTWPlugin.Log($"Oh no ! Player [{killedPlayer}]<{ArenaLives.TryGetLives(killedPlayer.abstractCreature, out _)}><{(ArenaLives.TryGetLives(killedPlayer.abstractCreature, out var a1) ? a1.killChain : false)}> got killed by [{killer}]<{ArenaLives.TryGetLives(killer.abstractCreature, out _)}><{(ArenaLives.TryGetLives(killer.abstractCreature, out var a2) ? a2.killChain : false)}> !");
            
//             if (killer.abstractCreature.GetOnlineCreature() is OnlineCreature onlineKiller
//                 && onlineKiller.owner is OnlinePlayer onlinePlayer)
//             {
//                 onlinePlayer.InvokeRPC(GetKillCredit, onlineKiller);
//             }

//             if (ArenaLives.TryGetLives(killedPlayer.abstractCreature, out var killedArenaLives))
//             {
//                 if (killedArenaLives.killChain >= 25)
//                 {
//                     ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 44, true);
//                 }
//                 else if (killedArenaLives.killChain >= 15)
//                 {
//                     ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 43, true);
//                 }
//                 else if (killedArenaLives.killChain >= 10)
//                 {
//                     ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 42, true);
//                 }
//                 else if (killedArenaLives.killChain >= 5)
//                 {
//                     ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 41, true);
//                 }
//                 else if (killedArenaLives.killChain == 0 && BTWFunc.Chance(0.05f))
//                 {
//                     ArenaDeathTracker.SetDeathTrackerOfCreature(killedPlayer.abstractCreature, 45, true);
//                 }
//                 killedArenaLives.killChain = 0;
//             }
//         }
//         base.Killing(arena, orig, self, killer, killedCrit);
//     }
//     [RPCMethod]
//     public static void GetKillCredit(RPCEvent rpc, OnlineCreature onlineKiller)
//     {
//         if (onlineKiller?.owner == OnlineManager.mePlayer 
//             && onlineKiller?.abstractCreature?.realizedCreature is Player killer
//             && !killer.dead
//             && MeadowFunc.IsMeadowArena(out var arenaOnline) 
//             && arenaOnline.IsStockArenaMode(out var stockArenaMode)
//             && ArenaLives.TryGetLives(killer.abstractCreature, out var arenaLives)) 
//         { 
//             arenaLives.killChain++;
//             if (stockArenaMode.killGiveLife && arenaLives.killChain % stockArenaMode.killAmountForLife == 0)
//             {
//                 arenaLives.lifesleft++;
//                 arenaLives.DisplayLives();
//             }
//             if (stockArenaMode.killGiveProtection 
//                 && arenaLives.killChain % stockArenaMode.killAmountForProtection == 0
//                 && !arenaLives.reinforced)
//             {
//                 arenaLives.reinforced = true;
//                 arenaLives.DisplayLives();
//             }
//             BTWPlugin.Log($"Hell yeah, got kill credit <{arenaLives.killChain}> !");
//         }
//     }

//     public override void ResetOnSessionEnd()
//     {
//         if (this.isTeamBattle && TeamBattleGamemode is TeamBattleMode teamBattleMode)
//         {
//             teamBattleMode.ResetOnSessionEnd();
//         }
//         base.ResetOnSessionEnd();
//     }
//     public override void ArenaSessionCtor(ArenaMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
//     {
//         if (this.isTeamBattle && TeamBattleGamemode is TeamBattleMode teamBattleMode)
//         {
//             teamBattleMode.ArenaSessionCtor(arena, orig, self, game);
//             return;
//         }
//         base.ArenaSessionCtor(arena, orig, self, game);
//     }
//     public override bool PlayerSittingResultSort(ArenaMode arena, On.ArenaSitting.orig_PlayerSittingResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
//     {
//         if (this.isTeamBattle && TeamBattleGamemode is TeamBattleMode teamBattleMode)
//         {
//             return teamBattleMode.PlayerSittingResultSort(arena, orig, self, A, B);
//         }
//         return base.PlayerSittingResultSort(arena, orig, self, A, B);
//     }
//     public override bool PlayerSessionResultSort(ArenaMode arena, On.ArenaSitting.orig_PlayerSessionResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
//     {
//         if (this.isTeamBattle && TeamBattleGamemode is TeamBattleMode teamBattleMode)
//         {
//             return teamBattleMode.PlayerSessionResultSort(arena, orig, self, A, B);
//         }
//         return base.PlayerSessionResultSort(arena, orig, self, A, B);
//     }
//     public override void ArenaSessionEnded(ArenaMode arena, On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session)
//     {
//         if (this.isTeamBattle && TeamBattleGamemode is TeamBattleMode teamBattleMode)
//         {
//             teamBattleMode.ArenaSessionEnded(arena, orig, self, session);
//             return;
//         }
//         base.ArenaSessionEnded(arena, orig, self, session);
//     }
//     public override void SpawnPlayer(ArenaMode arena, ArenaGameSession self, Room room, List<int> suggestedDens)
//     {
//         if (this.isTeamBattle && TeamBattleGamemode is TeamBattleMode teamBattleMode)
//         {
//             teamBattleMode.SpawnPlayer(arena, self, room, suggestedDens);
//             return;
//         }
//         base.SpawnPlayer(arena, self, room, suggestedDens);
//     }
//     public override void ArenaSessionNextLevel(ArenaMode arena, On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager process)
//     {
//         base.ArenaSessionNextLevel(arena, orig, self, process);
//     }
// }

// public static class StockArenaModeHook
// {
//     public static void ApplyHooks()
//     {
//         new Hook(typeof(ArenaMode).GetConstructor(new[] { typeof(Lobby) }), SetUpNewGamemode);
//         new Hook(typeof(ArenaRPCs).GetMethod(nameof(ArenaRPCs.Arena_RemovePlayerWhoQuit)), DismissLivesOfThoseWhoQuit);
//         new Hook(typeof(TeamBattleMode).GetMethod(nameof(TeamBattleMode.isTeamBattleMode)), StockTeamBattle);
//         new Hook(typeof(ArenaOnlineLobbyMenu).GetMethod(nameof(ArenaOnlineLobbyMenu.UpdateOnlineUI)), EnableStockTeamBattleUI);
//         new Hook(typeof(ArenaOnlineLobbyMenu).GetMethod(nameof(ArenaOnlineLobbyMenu.RemoveAndAddNewExtGameModeTab)), EnableStockTeamBattleUIOnStart);
//         On.Menu.PauseMenu.Singal += ArenaMenu_OnArenaExit;
//         On.Player.ctor += Player_AddArenaLivesFromSettings;
//     }

//     private static void EnableStockTeamBattleUIOnStart(Action<ArenaOnlineLobbyMenu, ExternalArenaGameMode> orig, ArenaOnlineLobbyMenu self, ExternalArenaGameMode gameMode)
//     {
//         orig(self, gameMode);
//         if ((ArenaMode)OnlineManager.lobby?.gameMode is ArenaMode arenaOnline
//             && gameMode is StockArenaMode stockArenaMode
//             && stockArenaMode.isTeamBattle
//             && stockArenaMode.GetTeamBattleMode(arenaOnline) is TeamBattleMode teamBattleMode)
//         {
//             teamBattleMode.OnUIEnabled(self);
//         }
//     }
//     private static void EnableStockTeamBattleUI(Action<ArenaOnlineLobbyMenu> orig, ArenaOnlineLobbyMenu self)
//     {
//         orig(self);
//         if ((ArenaMode)OnlineManager.lobby?.gameMode is ArenaMode arenaOnline
//             && arenaOnline.IsStockArenaMode(out var stockArenaMode)
//             && stockArenaMode.GetTeamBattleMode(arenaOnline) is TeamBattleMode teamBattleMode)
//         {
//             if (stockArenaMode.isTeamBattle)
//             {
//                 if (teamBattleMode.myTab == null)
//                 {
//                     teamBattleMode.OnUIEnabled(self);
//                 }
//                 teamBattleMode.OnUIUpdate(self);
//             }
//             else if (!stockArenaMode.isTeamBattle && teamBattleMode.myTab != null)
//             {
//                 teamBattleMode.OnUIDisabled(self);
//             }
//         }
//     }
//     private delegate bool isTeamBattleModeDelegate(ArenaMode arena, out TeamBattleMode tb);
//     private static bool StockTeamBattle(isTeamBattleModeDelegate orig, ArenaMode arena, out TeamBattleMode tb)
//     {
//         if (arena.IsStockArenaMode(out var stockArenaMode) && stockArenaMode.isTeamBattle)
//         {
//             tb = stockArenaMode.GetTeamBattleMode(arena);
//             return true;
//         }
//         return orig(arena, out tb);
//     }
//     private static void ArenaMenu_OnArenaExit(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
//     {
//         if (message == "EXIT" && MeadowFunc.IsMeadowArena(out var arena)
//             && (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.GetArenaGameSession is ArenaGameSession arenaGameSession)
//         {
//             foreach (var absPlayer in arenaGameSession.Players.FindAll(x => x.IsLocal()))
//             {
//                 if (ArenaLives.TryGetLives(absPlayer, out var arenaLives))
//                 {
//                     BTWPlugin.Log($"Dismissing live of [{absPlayer}] : i'm heading out !");
//                     arenaLives.Destroy();
//                 }
//             }
//         }
//         orig(self, sender, message);
//     }

//     private static void DismissLivesOfThoseWhoQuit(Action<OnlinePlayer> orig, OnlinePlayer earlyQuitterOrLatecomer)
//     {
//         if (MeadowFunc.IsMeadowArena(out var arena) 
//             && (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.GetArenaGameSession is ArenaGameSession arenaGameSession)
//         {
//             foreach (var absPlayer in arenaGameSession.Players.FindAll(x => x.GetOnlineCreature()?.owner == earlyQuitterOrLatecomer))
//             {
//                 if (ArenaLives.TryGetLives(absPlayer, out var arenaLives))
//                 {
//                     BTWPlugin.Log($"Dismissing live of [{absPlayer}] : [{earlyQuitterOrLatecomer}] is leaving !");
//                     arenaLives.fake = false;
//                     arenaLives.Destroy();
//                 }
//             }
//         }
//         orig(earlyQuitterOrLatecomer);
//     }
//     private static void SetUpNewGamemode(Action<ArenaMode, Lobby> orig, ArenaMode self, Lobby lobby)
//     {
//         orig(self, lobby);
//         self.AddExternalGameModes(StockArenaMode.StockArenaModeID, new StockArenaMode());
//     }
//     private static void Player_AddArenaLivesFromSettings(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
//     {
//         orig(self, abstractCreature, world);

//         if (MeadowFunc.IsMeadowArena(out var arenaOnline) 
//             && arenaOnline.IsStockArenaMode(out var stockArenaMode)
//             && BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings)
//         )
//         {
//             if (stockArenaMode.LivesDefaultAmount > 0 
//                 && self.room != null 
//                 && !ArenaLives.TryGetLives(BTWFunc.GetPlayerArenaNumber(abstractCreature), out _))
//             {
//                 ArenaLives arenaLives = new(
//                     BTWFunc.GetPlayerArenaNumber(abstractCreature), 
//                     arenaSettings.arenaStockClientSettings.lives,
//                     stockArenaMode.reviveTime * BTWFunc.FrameRate,
//                     stockArenaMode.additionalReviveTime * BTWFunc.FrameRate,
//                     stockArenaMode.blockWin, !abstractCreature.IsLocal())
//                 {
//                     enforceAfterReachingZero = stockArenaMode.strictEnforceAfter0Lives,
//                     shieldTime = stockArenaMode.respawnShieldToggle ? stockArenaMode.respawnShieldDuration * BTWFunc.FrameRate : 0
//                 };
//                 self.room.AddObject( arenaLives );
//             }
//         }
//     }
// }