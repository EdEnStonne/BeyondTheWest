using UnityEngine;
using BeyondTheWest;
using MoreSlugcats;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest.MSCCompat;
public static class MSCCalls
{
    public static void ExplodeArtificer(Creature creature)
    {
        if (creature is Player player && MSCFunc.IsArtificer(player))
        {
            player.PyroDeath();
        }
    }
}