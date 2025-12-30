using System;
using BeyondTheWest;
using UnityEngine;
using BeyondTheWest.MeadowCompat;

namespace BeyondTheWest.ArenaAddition;
public static class ArenaHookHelper
{
    public static void ApplyHooks()
    {
        ArenaLivesHooks.ApplyHooks();
        ArenaShieldHooks.ApplyHooks();
        ArenaRegistery.ApplyHooks();
        CompetitiveAddition.ApplyHooks();
        ArenaItemSpawnHooks.ApplyHooks();
        ArenaItemSpawnManagerHooks.ApplyHooks();

        BTWPlugin.Log("ArenaHookHelper ApplyHooks Done !");
    }
    public static void ApplyPostHooks()
    {
        ArenaLivesHooks.ApplyPostHooks();
        BTWPlugin.Log("ArenaHookHelper ApplyPostHooks Done !");
    }
}