using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest;
public class SparkFunc
{
    public const string SparkID = "Spark";

    // Functions
    public static void ApplyHooks()
    {
        StaticChargeHooks.ApplyHooks();
        StaticChargeBatteryUIHooks.ApplyHooks();

        On.Player.ctor += Player_Electric_Charge_Init;
        On.Player.ThrownSpear += Player_Spear_Elec_Modifier;
        IL.Player.UpdateBodyMode += Player_SparkCrawlSpeed;
        BTWPlugin.Log("SparkFunc ApplyHooks Done !");
    }

    public static bool IsSpark(Player player)
    {
        return player.SlugCatClass.ToString() == SparkID;
    }

    // Hooks
    private static void Player_Electric_Charge_Init(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (IsSpark(self) && self.GetBTWData() is BTWCreatureData bTWCreatureData)
        {
            bTWCreatureData.electricExplosionImmune = true;
            BTWPlugin.Log("Registered Spark as electricExplosionImmune");
        }
        if (IsSpark(self) && !StaticChargeManager.TryGetManager(self.abstractCreature, out _))
        {
            BTWPlugin.Log("Spark StaticChargeManager initiated");
            StaticChargeManager.AddManager(abstractCreature);
            BTWPlugin.Log("Spark StaticChargeManager created !");
        }
        if (IsSpark(self) && self.GetBTWPlayerData() is BTWPlayerData bTWPlayerData)
        {
            bTWPlayerData.slugHeight = 15f;
            BTWPlugin.Log("Changed Spark Height to 15 !");
        }
    }
    private static void Player_Spear_Elec_Modifier(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        orig(self, spear);
        if (self == null || self.room == null) { return; }
        if (spear == null || spear.bugSpear) { return; }
        if (IsSpark(self) && StaticChargeManager.TryGetManager(self.abstractCreature, out var SCM))
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
    
    private static float BoostSparkCrawl(float orig, Player player)
    {
        if (player.IsSpark())
        {
            return 3.5f;
        }
        return orig;
    }
    private static void Player_SparkCrawlSpeed(ILContext il)
    {
        BTWPlugin.Log("Spark IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.bodyMode)),
                x => x.MatchLdsfld<Player.BodyModeIndex>(nameof(Player.BodyModeIndex.Crawl)),
                x => x.MatchCall(out _),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.dynamicRunSpeed)),
                x => x.MatchLdcI4(0)) 
            && cursor.TryGotoNext(MoveType.Before,
                x => x.MatchStelemR4()
            ))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(BoostSparkCrawl);
            }

            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("Spark IL 1 ends");
        // BTWPlugin.Log(il);
    }
}