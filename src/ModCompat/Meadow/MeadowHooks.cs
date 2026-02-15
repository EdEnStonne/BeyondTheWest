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
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using BeyondTheWest.ArenaAddition;
using BeyondTheWest.MeadowCompat.Gamemodes;
using BeyondTheWest.MeadowCompat.BTWMenu;

namespace BeyondTheWest.MeadowCompat;
public static class MeadowHookHelper
{
    public static void ApplyHooks()
    {
        ArenaDeathTrackerHooks.ApplyHooks();
        MeadowDeniedSync.ApplyHooks();
        BTWMeadowArenaSettingsHooks.ApplyHooks();
        BTWVersionChecker.ApplyHooks();
        StockArenaModeHook.ApplyHooks();

        // new ILHook(typeof(FFA).GetMethod(nameof(FFA.IsExitsOpen)), FFA_DontOpenExitIfPlayerIsReviving);
        // new ILHook(typeof(TeamBattleMode).GetMethod(nameof(TeamBattleMode.IsExitsOpen)), TeamBattleMode_DontOpenExitIfPlayerIsReviving);
        
        new Hook(typeof(ArenaOnlineGameMode).GetConstructor(new[] { typeof(Lobby) }), SetUpArenaDescription);
        
        On.ArenaGameSession.Initiate += ArenaGameSession_RequestAllItemSpawner;
        On.Creature.Blind += Player_GetBlindedInArena;
        On.SporeCloud.Update += Player_GetSmokedInArena;
        BTWPlugin.Log("MeadowCompat ApplyHooks Done !");
    }

    private static void Player_GetSmokedInArena(On.SporeCloud.orig_Update orig, SporeCloud self, bool eu)
    {
        orig(self, eu);
        int distortTime = (int)(self.lifeTime * self.life * 2);
        if (!self.nonToxic
            && distortTime > BTWFunc.FrameRate * 1 
            && BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings)
            && arenaSettings.ArenaBonus_ExtraItemUses)
        {
            var playerInRange = BTWFunc.GetAllObjectsInRadius(self.room, self.pos, self.rad);
            for (int i = 0; i < playerInRange.Count; i++)
            {
                if (playerInRange[i].physicalObject is Player player
                    && player.Local()
                    && player.GetBTWPlayerData() is BTWPlayerData bTWPlayerData
                    && bTWPlayerData.onlineDizzy <= 0)
                {
                    BTWPlugin.Log($"Making player [{player}] dissy for <{distortTime}> ticks !");
                    bTWPlayerData.onlineDizzy = distortTime + 10;
                    ScreenDistord screenDistord = new(10, (int)(distortTime * 1/4f), (int)(distortTime * 3/4f - 10));
                    self.room.AddObject( screenDistord );
                }
            }
        }
    }

    private static void Player_GetBlindedInArena(On.Creature.orig_Blind orig, Creature self, int blnd)
    {
        orig(self, blnd);
        if (blnd > BTWFunc.FrameRate * 1 
            && self is Player player
            && player.Local()
            && player.GetBTWPlayerData() is BTWPlayerData bTWPlayerData
            && bTWPlayerData.onlineBlind <= 0
            && BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings)
            && arenaSettings.ArenaBonus_ExtraItemUses)
        {
            BTWPlugin.Log($"Blinding player [{self}] for <{blnd}> ticks !");
            bTWPlayerData.onlineBlind = blnd / 2;
            ScreenBlind screenBlind = new(5, (int)(blnd * 1/4f), (int)(blnd * 3/4f - 5), Color.white);
            self.room.AddObject( screenBlind );;
        }
    }

    private static void ArenaGameSession_RequestAllItemSpawner(On.ArenaGameSession.orig_Initiate orig, ArenaGameSession self)
    {
        orig(self);
        if (MeadowFunc.IsMeadowArena() && !MeadowFunc.IsMeadowHost())
        {
            MeadowCalls.BTWArena_RPCRequestItemSpawn(self);
        }
    }

    private static void SetUpArenaDescription(Action<ArenaOnlineGameMode, Lobby> orig, ArenaOnlineGameMode self, Lobby lobby)
    {
        orig(self, lobby);

        self.slugcatSelectMenuScenes.Remove("Trailseeker");
        self.slugcatSelectMenuScenes.Add("Trailseeker", MenuScene.SceneID.Landscape_SI);
        self.slugcatSelectMenuScenes.Remove("Core");
        self.slugcatSelectMenuScenes.Add("Core", MenuScene.SceneID.Landscape_SS);
        self.slugcatSelectMenuScenes.Remove("Spark");
        self.slugcatSelectMenuScenes.Add("Spark", MenuScene.SceneID.Landscape_UW);

        self.slugcatSelectDisplayNames.Remove("Trailseeker");
        self.slugcatSelectDisplayNames.Add("Trailseeker", "THE TRAILSEEKER");
        self.slugcatSelectDisplayNames.Remove("Core");
        self.slugcatSelectDisplayNames.Add("Core", "THE CORE");
        self.slugcatSelectDisplayNames.Remove("Spark");
        self.slugcatSelectDisplayNames.Add("Spark", "THE SPARK");

        self.slugcatSelectDescriptions.Remove("Trailseeker");
        self.slugcatSelectDescriptions.Add("Trailseeker", "Your journey gave you the experience to deal with that threat.<LINE>Attack from angles they can't reach.");
        self.slugcatSelectDescriptions.Remove("Core");
        self.slugcatSelectDescriptions.Add("Core", "A last threat between you and your mission.<LINE>Leap yourself to victory.");
        self.slugcatSelectDescriptions.Remove("Spark");
        self.slugcatSelectDescriptions.Add("Spark", "Cornered, but not powerless.<LINE>Zap them with agility.");
    }
    
    private static int ChangePlayerCount(int orig, ExitManager exitManager)
    {
        // Plugin.Log($"The current player count is <{orig}>, with exit manager [{exitManager}] of arena [{exitManager?.gameSession}] <{exitManager?.gameSession?.initiated}>."); 
        if (exitManager?.gameSession != null)
        {
            int addcount = ArenaLives.AdditionalPlayerInArenaCount(exitManager.gameSession);
            // if (addcount > 0) { Plugin.Log($"Hold on ! They say there's {orig} player but I say there's {orig + addcount} actually !"); }
            // else { Plugin.Log($"The current player count is {orig}, and no one else is reviving (count = {addcount}).");  }
            return orig + addcount;
        }
        return orig;
    }
    private static void FFA_DontOpenExitIfPlayerIsReviving(ILContext il)
    {
        BTWPlugin.Log("MeadowCompat IL 3 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);

            if (cursor.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(0),
                x => x.MatchLdloc(0),
                x => x.MatchLdcI4(1),
                x => x.MatchBneUn(out _)
            ))
            {
                cursor.GotoNext(MoveType.After, x => x.MatchLdloc(0));
                cursor.Emit(OpCodes.Ldarg_3);
                cursor.EmitDelegate(ChangePlayerCount);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }

            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("MeadowCompat IL 3 ends");
    }
    
    private static bool CheckPlayerAsAlive(bool orig, AbstractCreature abstractCreature, ExitManager exitManager)
    {
        if (!CompetitiveAddition.ReachedMomentWhenLivesAreSetTo0(exitManager?.gameSession))
        {
            return orig || ArenaLives.IsPlayerRevivingInArena(abstractCreature);
        }
        return orig;
    }
    private static void TeamBattleMode_DontOpenExitIfPlayerIsReviving(ILContext il)
    {
        BTWPlugin.Log("MeadowCompat IL 4 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);

            if (cursor.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(0),
                x => x.MatchLdloc(0),
                x => x.MatchLdcI4(1),
                x => x.MatchBneUn(out _)
            ))
            {
                cursor.GotoNext(MoveType.After, x => x.MatchLdloc(0));
                cursor.Emit(OpCodes.Ldarg_3);
                cursor.EmitDelegate(ChangePlayerCount);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook 1 :<");
            }

            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(8),
                x => x.MatchCallOrCallvirt(typeof(AbstractCreature).GetProperty(nameof(AbstractCreature.realizedCreature)).GetGetMethod()),
                x => x.MatchCallOrCallvirt(typeof(Creature).GetProperty(nameof(Creature.State)).GetGetMethod()),
                x => x.MatchCallOrCallvirt(typeof(CreatureState).GetProperty(nameof(CreatureState.alive)).GetGetMethod())
            ))
            {
                cursor.Emit(OpCodes.Ldloc_S, (byte)8);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate(CheckPlayerAsAlive);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook 2 :<");
            }

            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("MeadowCompat IL 4 ends");
    }
}