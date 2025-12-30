using UnityEngine;
using BeyondTheWest;
using MoreSlugcats;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BeyondTheWest.ArenaAddition;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;
using MSCObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using DLCObjectType = DLCSharedEnums.AbstractObjectType;

namespace BeyondTheWest.MSCCompat;
public static class SpawnMSCPool
{
    // Hooks
    public static void ApplyHooks()
    {
        InitMSCPools();
        BTWPlugin.Log("SpawnMSCPool ApplyHooks Done !");
    }

    private static void InitMSCPools()
    {
        ArenaItemSpawn.rockPool.AddToPool(DLCObjectType.SingularityBomb, 1);
        ArenaItemSpawn.allPool.AddToPool(DLCObjectType.SingularityBomb, 1);

        ArenaItemSpawn.spearPool.AddToPool(DLCObjectType.LillyPuck, 7);
        ArenaItemSpawn.allPool.AddToPool(DLCObjectType.LillyPuck, 7);
        ArenaItemSpawn.spearPool.AddToPool(ObjectType.Spear, 2, 7);
        ArenaItemSpawn.allPool.AddToPool(ObjectType.Spear, 2, 7);
        ArenaItemSpawn.spearPool.AddToPool(ObjectType.Spear, 3, 2);
        ArenaItemSpawn.allPool.AddToPool(ObjectType.Spear, 3, 2);
        
        ArenaItemSpawn.othersPool.AddToPool(MSCObjectType.FireEgg, 3);
        ArenaItemSpawn.allPool.AddToPool(MSCObjectType.FireEgg, 3);
    }

    //----------- Hooks
}