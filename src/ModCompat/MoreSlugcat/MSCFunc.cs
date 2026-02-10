using UnityEngine;
using BeyondTheWest;
using MoreSlugcats;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest.MSCCompat;
public static class MSCFunc
{
    public static bool IsElectricSpear(Spear spear)
    {
        return spear != null && spear is ElectricSpear electricSpear && electricSpear != null;
    }
    public static bool IsArtificer(Player player)
    {
        return player != null && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer;
    }
    public static bool IsSpearmaster(Player player)
    {
        return player != null && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear;
    }
    public static bool IsRivulet(Player player)
    {
        return player != null && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Rivulet;
    }
    public static bool IsSaint(Player player)
    {
        return player != null && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint;
    }
}