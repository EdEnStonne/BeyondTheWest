using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;
using RainMeadow;
using BeyondTheWest.MeadowCompat.Data;
using System.Runtime.CompilerServices;
using System.Linq;
using BeyondTheWest.MeadowCompat.Gamemodes;

namespace BeyondTheWest.MeadowCompat;

public static class BTWMeadowExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OnlineVoidSpark GetOnlineVoidSpark(this VoidSpark voidSpark)
    {
        return OnlineVoidSpark.map.TryGetValue(voidSpark, out var ovs) ? ovs : null;
    }
    public static bool IsStockArenaMode(this ArenaOnlineGameMode arena, out StockArenaMode stockArenaMode)
    {
        return StockArenaMode.IsStockArenaMode(arena, out stockArenaMode);
    }
    public static bool IsStockArenaMode(this ArenaOnlineGameMode arena)
    {
        return arena.IsStockArenaMode(out _);
    }
}