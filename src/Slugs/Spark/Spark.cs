using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using MonoMod.Cil;
using BeyondTheWest;
using Mono.Cecil.Cil;

public class SparkFunc
{
    public static ConditionalWeakTable<AbstractCreature, SparkObject.StaticChargeManager> cwtSpark = new();

    // Functions
    public static void ApplyHooks()
    {
        On.Player.ctor += Player_Electric_Charge_Init;
        On.Player.Update += Player_Electric_Charge_Update;
        On.Player.ThrownSpear += Player_Spear_Elec_Modifier;
        Plugin.Log("SparkFunc ApplyHooks Done !");
    }

    public static bool IsSpark(Player player)
    {
        return player.SlugCatClass.ToString() == "Spark";
    }

    // Hooks
    private static void Player_Electric_Charge_Init(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (IsSpark(self) && !cwtSpark.TryGetValue(self.abstractCreature, out _))
        {
            Plugin.Log("Spark StaticChargeManager initiated");
            cwtSpark.Add(abstractCreature, new SparkObject.StaticChargeManager(abstractCreature));
            Plugin.Log("Spark StaticChargeManager created !");
        }
    }
    private static void Player_Electric_Charge_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (cwtSpark.TryGetValue(self.abstractCreature, out var SCM))
        {
            SCM.Update();
            // Plugin.Log("Spark StaticChargeManager updating !");
        }
    }
    private static void Player_Spear_Elec_Modifier(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        orig(self, spear);
        if (self == null || self.room == null) { return; }
        if (IsSpark(self) && cwtSpark.TryGetValue(self.abstractCreature, out var SCM))
        {
            float FractCharge = SCM.Charge / SCM.FullECharge;
            BodyChunk firstChunk = spear.firstChunk;
            var color = self.ShortCutColor();
            var room = self.room;
            var body = self.mainBodyChunk;
            var pos = body.pos;

            var frontPos = pos;
            frontPos.x += self.ThrowDirection * 9f;

            float mult;
            if (SCM.Charge < 20f)
            {
                spear.spearDamageBonus = 0.25f;
                spear.throwModeFrames = 3;
                firstChunk.vel.x *= 0.5f;

                return;
            }
            else if (FractCharge < 1.0)
            {
                SCM.Charge -= 20f;
                mult = 0.5f;
            }
            else
            {
                SCM.Charge -= 40f;

                spear.spearDamageBonus *= 2f;
                spear.throwModeFrames = (int)(spear.throwModeFrames * 1.5f);
                firstChunk.vel.x *= 1.25f;
                firstChunk.vel.y *= 1.25f;
                body.vel.x += UnityEngine.Random.Range(3f, 5f) * self.ThrowDirection;

                mult = 2.5f;
            }

            for (int i = (int)(UnityEngine.Random.Range(3f, 10f) * mult); i >= 0; i--)
            {
                room.AddObject(new MouseSpark(frontPos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)) * mult, 10f * mult, color));
            }
            room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, frontPos, 0.35f * mult, UnityEngine.Random.Range(1.25f, 2f));
            room.PlaySound(SoundID.Fire_Spear_Pop, frontPos, 0.15f * mult, UnityEngine.Random.Range(0.75f, 0.85f));
        }
    }
}