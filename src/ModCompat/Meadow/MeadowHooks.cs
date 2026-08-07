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
// using BeyondTheWest.MeadowCompat.Gamemodes;
using BeyondTheWest.MeadowCompat.BTWMenu;
using System.Linq;

namespace BeyondTheWest.MeadowCompat;
public static class MeadowHookHelper
{
    public static void ApplyHooks()
    {
        ArenaDeathTrackerHooks.ApplyHooks();
        MeadowDeniedSync.ApplyHooks();
        BTWMeadowArenaSettingsHooks.ApplyHooks();
        BTWVersionChecker.ApplyHooks();
        // StockArenaModeHook.ApplyHooks();

        new Hook (typeof(StoryOnlineMenu).GetMethod(nameof(StoryOnlineMenu.Update)), StoryOnlineMenu_LockWIPCampaigns);
        new Hook(typeof(ArenaOnlineGameMode).GetConstructor(new[] { typeof(Lobby) }), SetUpArenaDescription);
        
        On.Creature.Blind += Player_GetBlindedInArena;
        On.SporeCloud.Update += Player_GetDizzyInArena;
        On.FirecrackerPlant.PopLump += Player_GetStunYeetedByFirePlantPop;
        On.FirecrackerPlant.Explode += Player_GetStunYeetedByFirePlantExplode;

        BTWPlugin.Log("MeadowCompat ApplyHooks Done !");
    }
    private static void StoryOnlineMenu_LockWIPCampaigns(Action<StoryOnlineMenu> orig, StoryOnlineMenu self)
    {
        orig(self);
        if (WIPSlugLock.WIPLock.Contains(self.colorFromIndex(self.slugcatPageIndex).ToString()))
        {
	        self.startButton.menuLabel.text = self.Translate("WORK IN\nPROGRESS");
            self.startButton.GetButtonBehavior.greyedOut = true;
        }
        foreach (SlugcatSelectMenu.SlugcatPage page in self.slugcatPages)
        {
            if (page is SlugcatSelectMenu.SlugcatPageNewGame newpage 
                && newpage.infoLabel.label.color != Color.red
                && WIPSlugLock.WIPLock.Contains(page.slugcatNumber.ToString()))
            {
                newpage.infoLabel.text = "This campaign is still a work in progress.\nYou can still play this slugcat in arena, with Jolly Co-op or in Meadow.";
                newpage.infoLabel.label.color = Color.red;
            }
        }
    }

    private static void Player_GetStunYeetedByFirePlantExplode(On.FirecrackerPlant.orig_Explode orig, FirecrackerPlant self)
    {
        if (self?.room is Room room
            && BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings)
            && arenaSettings.ArenaBonus_ExtraItemUses)
        {
            var playerInRange = BTWFunc.GetAllObjectsInRadius(room, self.firstChunk.pos, 90f);
            for (int i = 0; i < playerInRange.Count; i++)
            {
                if (playerInRange[i].physicalObject is Player player && player.Local())
                {
                    BTWFunc.CustomKnockback(player, playerInRange[i].vectorDistance, 20f);
                    player.stun = Mathf.Max(player.stun, BTWFunc.FrameRate * 3);
                }
            }
        }
        orig(self);
    }

    private static void Player_GetStunYeetedByFirePlantPop(On.FirecrackerPlant.orig_PopLump orig, FirecrackerPlant self, int lmp)
    {
        orig(self, lmp);
        if (BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings)
            && arenaSettings.ArenaBonus_ExtraItemUses)
        {
            var playerInRange = BTWFunc.GetAllObjectsInRadius(self.room, self.firstChunk.pos, 30f);
            for (int i = 0; i < playerInRange.Count; i++)
            {
                if (playerInRange[i].physicalObject is Player player && player.Local())
                {
                    BTWFunc.CustomKnockback(playerInRange[i].closestBodyChunk, playerInRange[i].vectorDistance, 10f);
                    player.stun = Mathf.Max(player.stun, BTWFunc.FrameRate * 1);
                }
            }
        }
    }

    private static void Player_GetDizzyInArena(On.SporeCloud.orig_Update orig, SporeCloud self, bool eu)
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
                    && !bTWPlayerData.sporecloudsHit.Contains(self))
                {
                    bTWPlayerData.sporecloudsHit.Add(self);
                    if (bTWPlayerData.dizzy <= 0)
                    {
                        BTWPlugin.Log($"Making player [{player}] dissy for <{distortTime}> ticks !");
                        bTWPlayerData.dizzy = distortTime;
                        player.exhausted = true;
                        player.aerobicLevel = 1f;
                        ScreenDistord screenDistord = new(10, (int)(distortTime * 1/4f), (int)(distortTime * 3/4f - 10));
                        self.room.AddObject( screenDistord );
                    }
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
}