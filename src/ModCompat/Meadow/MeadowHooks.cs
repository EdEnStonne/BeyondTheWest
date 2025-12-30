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

namespace BeyondTheWest.MeadowCompat;
public static class MeadowHookHelper
{
    public static void ApplyHooks()
    {
        ArenaDeathTrackerHooks.ApplyHooks();
        MeadowDeniedSync.ApplyHooks();
        BTWMeadowArenaSettingsHooks.ApplyHooks();

        new ILHook(typeof(FFA).GetMethod(nameof(FFA.IsExitsOpen)), FFA_DontOpenExitIfPlayerIsReviving);
        new ILHook(typeof(TeamBattleMode).GetMethod(nameof(TeamBattleMode.IsExitsOpen)), TeamBattleMode_DontOpenExitIfPlayerIsReviving);
        
        new Hook(typeof(ArenaOnlineGameMode).GetConstructor(new[] { typeof(Lobby) }), SetUpArenaDescription);
        
        On.ArenaGameSession.Initiate += ArenaGameSession_RequestAllItemSpawner;
        BTWPlugin.Log("MeadowCompat ApplyHooks Done !");
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
        self.slugcatSelectDescriptions.Add("Spark", "Cornered, by not powerless.<LINE>Zap them with agility.");
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
                BTWPlugin.Log(il);
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
            return orig || ArenaLives.PlayerCountedAsAliveInArena(abstractCreature);
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
                BTWPlugin.Log(il);
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
                BTWPlugin.Log(il);
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