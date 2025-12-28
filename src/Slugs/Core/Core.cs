using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest;
public class CoreFunc
{
    public const string CoreID = "Core";
    public static void ApplyHooks()
    {
        AbstractEnergyCoreHooks.ApplyHooks();

        On.Player.ctor += Player_coreInit;
        On.Player.Update += Player_CoreUpdate;
        On.Creature.Violence += Player_Explosion_Resistance;
        BTWPlugin.Log("CoreFunc ApplyHooks Done !");
    }

    public static bool IsCore(Player player)
    {
        return player.SlugCatClass.ToString() == CoreID;
    }
    public static void GiveCoreToPlayer(Player player)
    {
        BTWPlugin.Log("Trying to add core to " + player.ToString());

        AbstractEnergyCore.AddCore(player.abstractCreature);
        // abstractEnergyCore.RealizeInRoom();

        BTWPlugin.Log("Core of " + player.abstractCreature.ToString() + "(" + player.ToString() + ")" + " added in world !");
    }


    // Hooks

    private static void Player_CoreUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (IsCore(self))
        {
            bool hasCore = AbstractEnergyCore.TryGetCore(self.abstractCreature, out var AEC);
            if (!hasCore || AEC == null || AEC.world == null || AEC.pos == null)
            {
                BTWPlugin.Log("Something wrong happened to the core of " + self.ToString() + $". \nHasCore = <{hasCore}>, AEC world = [{AEC?.world}], AEC Pos = [{AEC?.pos}]. \nFixing it...");
                if (hasCore) {
                    if (AEC != null)
                    {
                        AEC.RealizedCore?.Destroy();
                        AEC.Destroy();
                    }
                    AbstractEnergyCore.RemoveCore(self.abstractCreature);
                }
                GiveCoreToPlayer(self);
            }
        }
        orig(self, eu);
    }
    private static void Player_Explosion_Resistance(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self != null && self is Player player && player != null && type == Creature.DamageType.Explosion && IsCore(player))
        {
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage * 0.55f, stunBonus * 0.5f);
        }
        else
        {
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }
    }
    private static void Player_coreInit(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (IsCore(self) && !AbstractEnergyCore.TryGetCore(self.abstractCreature, out _))
        {
            GiveCoreToPlayer(self);
        }
    }
}