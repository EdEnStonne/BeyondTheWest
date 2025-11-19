using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using BepInEx;
using BepInEx.Logging;
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
        On.Creature.Violence += Player_Electric_Absorb;
        IL.Centipede.Shock += Player_CentipedeShock_Absorb;
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
        if (cwtSpark.TryGetValue(self.abstractCreature, out var SCM))
        {
            SCM.Update();
            // Plugin.Log("Spark StaticChargeManager updating !");
        }
        orig(self, eu);
    }
    private static void Player_CentipedeShock_Absorb(ILContext il) // this is the first IL hook I made myself :D
    {
        Plugin.Log("Shock IL starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before, x => x.MatchCall<Centipede>("get_Small")))
            {
                static bool CanPlayerShockAbsorb(Creature creature) => creature is Player player && cwtSpark.TryGetValue(player.abstractCreature, out _);

                static void CheckPlayerAbsorb(Centipede self, Creature creature)
                {
                    if (CanPlayerShockAbsorb(creature) && cwtSpark.TryGetValue(creature.abstractCreature, out var SCM))
                    {
                        Player pl = creature as Player;
                        pl.SetKillTag(self.abstractCreature);

                        float diff = 0.45f;
                        float AddedCharge = 0f;
                        self.shockCharge = 0f;
                        if (self.Small)
                        {
                            AddedCharge = (SCM.FullECharge - SCM.Charge) * diff;
                        }
                        else
                        {
                            AddedCharge = ((SCM.FullECharge / 2) * (self.TotalMass / pl.TotalMass) - SCM.Charge) * diff;
                        }
                        
                        AddedCharge = Math.Max(0, AddedCharge);
                        SCM.Charge += AddedCharge;

                        self.Stun(6);
                        self.shockGiveUpCounter = Math.Max(self.shockGiveUpCounter, 30);
                        self.AI.annoyingCollisions = Math.Min(self.AI.annoyingCollisions / 2, 150);

                        Plugin.Log("SHOCKING ! " + AddedCharge);
                    }
                    else
                    {
                        Plugin.Log("Can't shock :<");
                    }
                }

                // Logger.LogDebug("Moving cursor");
                cursor.GotoPrev(MoveType.Before, x => x.MatchLdarg(0)); 
                var mark1 = cursor.Previous; 

                cursor.Emit(OpCodes.Ldarg_0); // add shock resist
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(CheckPlayerAbsorb);
                var mark2 = cursor.Previous; 
                
                // Logger.LogDebug("Get label 1");
                ILLabel label1 = cursor.DefineLabel(); // set label to ignore if not met
                cursor.MarkLabel(label1);

                // Logger.LogDebug("Get label 2");
                ILLabel label2 = cursor.DefineLabel(); // set label to ignore if met
                cursor.GotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<PhysicalObject>("get_Submersion"));
                cursor.GotoPrev(MoveType.Before, x => x.MatchLdarg(1)); 
                cursor.MarkLabel(label2);

                // Logger.LogDebug("Moving to mark 1");
                cursor.Goto(0, MoveType.After); //return to start
                cursor.GotoNext(MoveType.After, x => x == mark1); //add condition to ignore if not met
                cursor.Emit(OpCodes.Ldarg_1); 
                cursor.EmitDelegate(CanPlayerShockAbsorb);
                cursor.Emit(OpCodes.Brfalse, label1);

                // Logger.LogDebug("Moving to mark 2");
                cursor.Goto(0, MoveType.After); //return to start
                cursor.GotoNext(MoveType.After, x => x == mark2); //add ignore if met
                cursor.Emit(OpCodes.Br, label2);


                // Logger.LogDebug("Cursor moved !");
            }
            else
            {
                Plugin.logger.LogError("Couldn't find IL hook :<");
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("Shock IL ends");
    }
    private static void Player_Electric_Absorb(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self != null && self is Player player && type == Creature.DamageType.Electric && cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
        {
            SCM.Charge += 40f * damage;
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, 0f, stunBonus / 4);
        }
        else
        {
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
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