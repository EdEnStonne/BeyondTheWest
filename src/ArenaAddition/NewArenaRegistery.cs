using System;
using System.Collections.Generic;
using BeyondTheWest;
using JetBrains.Annotations;
using UnityEngine;

namespace BeyondTheWest.ArenaAddition;
public static class ArenaRegistery
{
    public static void ApplyHooks()
    {
        InitArenaNames();
        On.MultiplayerUnlocks.LevelDisplayName += SetNewArenaNames;
        Plugin.Log("ArenaRegistery ApplyHooks Done !");
    }

    public static Dictionary<string, string> ArenaNames = new();
    
    public static void InitArenaNames()
    {
        ArenaNames.Add("btw_arena_erodedpipes", "Eroded Pipes");
        ArenaNames.Add("btw_arena_strandedsandway1", "Standed Platforms");

        Plugin.Log("Added names of arenas!");
    }

    private static string SetNewArenaNames(On.MultiplayerUnlocks.orig_LevelDisplayName orig, string s)
    {
        // Plugin.Log(s + "/" + orig(s) + "/" + ArenaNames.ContainsKey(s));
        if (ArenaNames.ContainsKey(s)) { return ArenaNames[s]; }
        return orig(s);
    }
}