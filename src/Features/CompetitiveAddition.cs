using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using BepInEx;
using BepInEx.Logging;
using System;
using MonoMod.Cil;
using RWCustom;
using BeyondTheWest;
using Mono.Cecil.Cil;

public class CompetitiveAddition
{
    public static void ApplyHooks()
    {
        On.ArenaGameSession.SpawnItem += ItemMultCompetitive;
        Plugin.Log("CompetitiveAddition ApplyHooks Done !");
    }

    public static bool MeadowCheckIfShouldMultiplyItems()
    {
        if (Plugin.meadowEnabled)
        {
            return !MeadowCompat.IsMeadowLobby();
        }
        return true;
    }

    // Hooks
    private static void ItemMultCompetitive(On.ArenaGameSession.orig_SpawnItem orig, ArenaGameSession self, Room room, PlacedObject placedObj)
    {
        if (!Plugin.meadowEnabled || MeadowCheckIfShouldMultiplyItems())
        {
            float loop = //(BeyondTheWestRemixMenu.DoItemSpawnScalePerPlayers.Value ? BeyondTheWestRemixMenu.ItemSpawnMultiplierPerPlayers.Value : 1f)
                1f * BTWRemix.ItemSpawnMultiplier.Value;

            while (loop > 0f)
            {
                if (loop < 1f)
                {
                    if (UnityEngine.Random.value > loop)
                    {
                        return;
                    }
                }
                orig(self, room, placedObj);
                loop--;
            }
        }
        else
        {
            orig(self, room, placedObj);
        }
    }
}
