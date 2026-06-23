using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using DressMySlugcat;
using RWCustom;

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
        On.PlayerGraphics.ctor += PlayerGraphics_ctor_MakeSparkCuter;
        On.Player.ThrownSpear += Player_Spear_Elec_Modifier;
        IL.Player.UpdateBodyMode += Player_SparkCrawlSpeed;
        IL.Player.ThrowObject += Player_ThrowObject_MakeSparkThrowWeak;
        BTWPlugin.Log("SparkFunc ApplyHooks Done !");
    }

    private static float WeakThrow(float speed, Player player, int grasp)
    {
        // BTWPlugin.Log($"Checking throw for {player} with {player.grasps[grasp].grabbed as Weapon}");
        if (player.IsSpark() && StaticChargeManager.TryGetManager(player.abstractCreature, out var SCM))
        {
            float FractCharge = SCM.Charge / SCM.FullECharge;
            Weapon weapon = player.grasps[grasp].grabbed as Weapon;
            var color = player.ShortCutColor();
            var room = player.room;
            var body = player.mainBodyChunk;
            var pos = body.pos;

            var frontPos = pos;
            frontPos.x += player.ThrowDirection * 9f;

            float mult;
            if (SCM.Charge < StaticChargeManager.ChargeToThrowSpear)
            {
                // BTWPlugin.Log($"Throw weakened for {player} with {weapon}");
                return speed / (weapon is Spear ? 3 : 2);
            }
            else if (FractCharge < 1.0)
            {
                SCM.Charge -= StaticChargeManager.ChargeToThrowSpear / (weapon is Spear ? 2 : 8);
                mult = weapon is Spear ? 0.5f : 0.2f;
            }
            else
            {
                SCM.Charge -= StaticChargeManager.ChargeToThrowSpear / (weapon is Spear ? 1 : 3);
                mult = weapon is Spear ? 2f : 0.8f;
            }

            for (int i = (int)Mathf.Ceil(UnityEngine.Random.Range(3f, 10f) * mult); i >= 0; i--)
            {
                room.AddObject(new MouseSpark(frontPos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)) * mult, 10f * mult, color));
            }
            room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, frontPos, 0.35f * mult, UnityEngine.Random.Range(1.25f, 2f));
            if (weapon is Spear) room.PlaySound(SoundID.Fire_Spear_Pop, frontPos, 0.15f * mult, UnityEngine.Random.Range(0.75f, 0.85f));
        }
        return speed;
    }
    private static void Player_ThrowObject_MakeSparkThrowWeak(ILContext il)
    {
        BTWPlugin.Log("Spark IL 2 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);

            if (cursor.TryGotoNext(MoveType.After,
                    x => x.MatchLdloc(0),
                    x => x.MatchLdcR4(0.5f),
                    x => x.MatchLdcR4(0.75f),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(Player).GetProperty(nameof(Player.Adrenaline)).GetGetMethod()),
                    x => x.MatchCall<Mathf>(nameof(Mathf.Lerp)))
                && cursor.TryGotoNext(MoveType.After,
                    x => x.MatchLdloc(0),
                    x => x.MatchLdcR4(1f),
                    x => x.MatchLdcR4(1.5f),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(Player).GetProperty(nameof(Player.Adrenaline)).GetGetMethod()),
                    x => x.MatchCall<Mathf>(nameof(Mathf.Lerp)))
                )
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(WeakThrow);
            }
            else
            {
                BTWPlugin.LogError("Couldn't find IL hook :<");
            }

            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("Spark IL 2 ends");
    }
    private static void PlayerGraphics_ctor_MakeSparkCuter(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        orig(self, ow);
        if (IsSpark(self.player))
        {
            for (int i = 0; i < self.tail.Length; i++)
            {
                self.tail[i].rad *= 1.3f;
                self.tail[i].connectionRad *= 0.7f;
            }
        }
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
            var body = self.mainBodyChunk;

            if (FractCharge > 1.0)
            {
                spear.spearDamageBonus = 1f + 0.25f * Mathf.Pow(BTWFunc.random, 4f);;
                spear.throwModeFrames = (int)(spear.throwModeFrames * 1.5f);
                firstChunk.vel.x *= 1.25f;
                body.vel.x += 5f * self.ThrowDirection;
            }
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
            else
            {
                BTWPlugin.LogError("Couldn't find IL hook :<");
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