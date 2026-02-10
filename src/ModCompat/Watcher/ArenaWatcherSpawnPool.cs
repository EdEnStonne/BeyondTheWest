using UnityEngine;
using BeyondTheWest;
using Watcher;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BeyondTheWest.ArenaAddition;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;
using WatcherObjectType = Watcher.WatcherEnums.AbstractObjectType;

namespace BeyondTheWest.WatcherCompat;
public static class SpawnWatcherPool
{
    // Hooks
    public static void ApplyHooks()
    {
        InitWatcherPools();
        BTWPlugin.Log("SpawnWatchePool ApplyHooks Done !");
    }

    private static void InitWatcherPools()
    {
        ArenaItemSpawn.rockPool.AddToPool(WatcherObjectType.Boomerang, 45);
        ArenaItemSpawn.allPool.AddToPool(WatcherObjectType.Boomerang, 45);

        ArenaItemSpawn.spearPool.AddToPool(ObjectType.Spear, 4, 7);
        ArenaItemSpawn.allPool.AddToPool(ObjectType.Spear, 4, 7);

        ArenaItemSpawn.LogAllPools("Watcher");
    }

    //----------- Hooks
}