using UnityEngine;
using BeyondTheWest;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using PushToMeowMod;
using MonoMod.RuntimeDetour;
using BeyondTheWest.MSCCompat;

namespace BeyondTheWest.PushToMeowCompat;
public static class BTWMeow
{
    // Hooks
    public static void ApplyHooks()
    {
        new Hook(typeof(MeowUtils).GetMethod(nameof(MeowUtils.DoMeowAnim)), OnMeow);
        BTWPlugin.Log("BTWMeow ApplyHooks Done !");
    }

    //----------- Hooks
    private static void OnMeow(Action<Player, bool> orig, Player player, bool isShortMeow)
    {
        orig(player, isShortMeow);
        if (player.IsCore() && player.GetAEC()?.RealizedCore is EnergyCore energyCore)
        {
            energyCore.meowBlink = isShortMeow ? 9 : 11;
        }
        else if (player.IsSpark() && player.room is Room room)
        {
            ElectricExplosion.MakeSparks(room, 10f, player.bodyChunks[0].pos, 5, player.ShortCutColor());
            room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, player.bodyChunks[0], false, isShortMeow ? 0.1f : 0.2f, BTWFunc.Random(1.1f, 1.5f));
        }
    }
}