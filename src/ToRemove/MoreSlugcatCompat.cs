// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using UnityEngine;
// using BeyondTheWest;
// using MoreSlugcats;
// using System;
// using MonoMod.Cil;
// using Mono.Cecil.Cil;

// public class MoreSlugcatCompat
// {
//     //---------- Objects
//     public class LightingArc : UpdatableAndDeletable
//     {
//         public LightingArc(float width, float intensity, int lifeTime, Color color)
//         {
//             this.width = width;
//             this.intensity = intensity;
//             this.lifeTime = lifeTime <= 0 ? 1 : lifeTime;
//             this.color = color;
//         }
//         public LightingArc(BodyChunk from, BodyChunk target, float width, float intensity, int lifeTime, Color color)
//             : this(width, intensity, lifeTime, color)
//         {
//             this.from = from;
//             this.target = target;

//             CreateLightingArc();
//         }
//         public LightingArc(Vector2 fromPos, BodyChunk target, float width, float intensity, int lifeTime, Color color)
//             : this(width, intensity, lifeTime, color)
//         {
//             this.fromOffset = fromPos;
//             this.target = target;

//             CreateLightingArc();
//         }
//         public LightingArc(BodyChunk from, Vector2 targetPos, float width, float intensity, int lifeTime, Color color)
//             : this(width, intensity, lifeTime, color)
//         {
//             this.from = from;
//             this.targetOffset = targetPos;

//             CreateLightingArc();
//         }
//         public LightingArc(Vector2 fromPos, Vector2 targetPos, float width, float intensity, int lifeTime, Color color)
//             : this(width, intensity, lifeTime, color)
//         {
//             this.fromOffset = fromPos;
//             this.targetOffset = targetPos;

//             CreateLightingArc();
//         }

//         private void CreateLightingArc()
//         {
//             this.lightningArc = new LightningBolt(Vector2.zero, Vector2.zero, 0, width, lifeTime /  30f, 0.64f, 0.64f, true)
//             {
//                 intensity = intensity
//             };
//             if (color != null)
//             {
//                 this.lightningArc.color = color;
//             }
//             SetLightingArcPos();
//         }
//         private void SetLightingArcPos()
//         {
//             if (this.lightningArc != null)
//             {
//                 Vector2 fromPos = this.fromOffset + (this.from != null ? this.from.pos : Vector2.zero);
//                 Vector2 toPos = this.targetOffset + (this.target != null ? this.target.pos : Vector2.zero);
//                 this.lightningArc.from = fromPos;
//                 this.lightningArc.target = toPos;
//             }
//         }
//         public override void Update(bool eu)
//         {
//             base.Update(eu);
//             if (!this.lightningAddedToRoom)
//             {
//                 if (this.lightningArc == null) { this.Destroy(); return; }
//                 if (this.room != null)
//                 {
                    
//                     this.room.AddObject(this.lightningArc);
//                     this.lightningAddedToRoom = true;
//                     // Plugin.Log("Lighting Arc added to room !");
//                 }
//                 return;
//             }
//             if (this.timelived < this.lifeTime
//                 && this.lightningArc != null 
//                 && !this.lightningArc.slatedForDeletetion)
//             {
//                 SetLightingArcPos();
//                 timelived++;
//             }
//             else
//             {
//                 this.Destroy();
//                 // Plugin.Log("Lighting Arc removed !");
//             }
//         }

//         public LightningBolt lightningArc;
//         public BodyChunk from;
//         public BodyChunk target;
//         public Vector2 fromOffset = Vector2.zero;
//         public Vector2 targetOffset = Vector2.zero;
//         public Color color;
//         public bool mustDestroy = false;
//         public bool lightningAddedToRoom = false;
//         public int timelived = 0;
//         public int lifeTime;
//         public float width;
//         public float intensity;
//     }

//     //---------- Functions

//     public static bool IsElectricSpear(Spear spear)
//     {
//         return spear != null && spear is ElectricSpear electricSpear && electricSpear != null;
//     }
//     public static bool IsArtificer(Player player)
//     {
//         return player != null && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer;
//     }
//     public static bool IsSpearmaster(Player player)
//     {
//         return player != null && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear;
//     }

//     // Spark
//     public static void StaticManager_CheckIfArtififerShouldExplode(Creature creature)
//     {
//         if (creature is Player player && IsArtificer(player))
//         {
//             player.PyroDeath();
//         }
//     }
    
//     public static bool StaticManager_CanCraftSpear(SparkObject.StaticChargeManager SCM, Spear spear)
//     {
//         return spear != null && spear.abstractSpear.electric 
//             && (SCM.IsOvercharged || spear.abstractSpear.electricCharge > 0);
//     }
    
//     // Hooks
//     public static void ApplyHooks()
//     {
//         On.MoreSlugcats.ElectricSpear.CheckElectricCreature += ElectricSpear_SparkIsElectric;
//         On.Player.CraftingResults += Player_StaticChargeManager_CraftingResult;
//         On.Player.GraspsCanBeCrafted += Player_StaticChargeManager_GraspsCanBeCrafted;
//         On.Player.SpitUpCraftedObject += Player_StaticChargeManager_SpitUpCraftedObject;
//         IL.Player.GrabUpdate += Player_StaticChargeManager_CanCraftObject;
//         Plugin.Log("MoreSlugcatCompat ApplyHooks Done !");
//     }




//     //----------- Hooks
//     private static void Player_StaticChargeManager_CanCraftObject(ILContext il)
//     {
//         Plugin.Log("MSC IL 1 starts");
//         try
//         {
//             Plugin.Log("Trying to hook IL");
//             ILCursor cursor = new(il);
//             if (cursor.TryGotoNext(MoveType.After,
//                 x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)),
//                 x => x.MatchBrfalse(out _),
//                 x => x.MatchLdarg(0),
//                 x => x.MatchCall<Player>(nameof(Player.FreeHand)),
//                 x => x.MatchLdcI4(out _),
//                 x => x.MatchBeq(out _),
//                 x => x.MatchLdarg(0),
//                 x => x.MatchLdfld<Player>(nameof(Player.SlugCatClass)),
//                 x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer)),
//                 x => x.MatchCall(out _)
//             ))
//             {
//                 static bool HasSCM(bool orig, Player player)
//                 {
//                     // Plugin.Log("MAKE IT CRAFT !!!");
//                     if (SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
//                     {
//                         return true;
//                     }
//                     return orig;
//                 }
//                 cursor.Emit(OpCodes.Ldarg_0);
//                 cursor.EmitDelegate(HasSCM);
//             }
//             else
//             {
//                 Plugin.logger.LogError("Couldn't find IL hook :<");
//             }
//             Plugin.Log("IL hook ended");
//         }
//         catch (Exception ex)
//         {
//             Plugin.logger.LogError(ex);
//         }
//         Plugin.Log("MSC IL 1 ends");
//     }
//     private static bool ElectricSpear_SparkIsElectric(On.MoreSlugcats.ElectricSpear.orig_CheckElectricCreature orig, ElectricSpear self, Creature otherObject)
//     {
//         return SparkFunc.cwtSpark.TryGetValue(otherObject.abstractCreature, out var _) || orig(self, otherObject);
//     }
//     private static void Player_StaticChargeManager_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
//     {
//         if (SparkFunc.cwtSpark.TryGetValue(self.abstractCreature, out var SCM))
//         {
//             self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);
//             for (int i = 0; i < self.grasps.Length; i++)
//             {
//                 if (self.grasps[i] != null)
//                 {
//                     AbstractPhysicalObject abstractPhysicalObject = self.grasps[i].grabbed.abstractPhysicalObject;
//                     if (abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Spear && (abstractPhysicalObject as AbstractSpear).electric)
//                     {
//                         AbstractSpear abstractSpear = abstractPhysicalObject as AbstractSpear;
//                         if (abstractSpear.realizedObject is ElectricSpear electricSpear)
//                         {
//                             if (SCM.IsOvercharged)
//                             {
//                                 SCM.Charge -= SCM.FullECharge > 0 ? SCM.FullECharge : SCM.MaxECharge;

//                                 self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.firstChunk);
//                                 self.room.AddObject(new Explosion.ExplosionLight(electricSpear.firstChunk.pos, 50f, 1f, 4, new Color(0.7f, 1f, 1f)));
//                                 electricSpear.Spark();
//                                 electricSpear.Zap();
//                                 abstractSpear.electricCharge++;

//                                 if (abstractSpear.electricCharge > 3 && UnityEngine.Random.value < 0.1f * abstractSpear.electricCharge)
//                                 {
//                                     BTWFunc.CustomKnockback(self, electricSpear.firstChunk.pos - self.mainBodyChunk.pos, 3f + UnityEngine.Random.value * 3f);
//                                     electricSpear.ExplosiveShortCircuit();
//                                     self.Stun(BTWFunc.FrameRate * 5);
//                                 }
//                                 SCM.Discharge(50f, 0.25f, 0, self.mainBodyChunk.pos, 1f);
//                             }
//                             else if (abstractSpear.electricCharge > 0)
//                             {
//                                 SCM.overchargeImmunity = Mathf.Max(SCM.overchargeImmunity, BTWFunc.FrameRate * 20);
//                                 SCM.Charge += SCM.MaxECharge > SCM.FullECharge ? (SCM.MaxECharge - SCM.FullECharge) : SCM.FullECharge;

//                                 SCM.Discharge(80f, 0.85f, 0, self.mainBodyChunk.pos, 1f);

//                                 abstractSpear.electricCharge--;
//                             }
//                         }
//                     }
//                 }
//             }
// 			return;
//         }
//         orig(self);
//     }
//     private static AbstractPhysicalObject.AbstractObjectType Player_StaticChargeManager_CraftingResult(On.Player.orig_CraftingResults orig, Player self)
//     {
//         if (SparkFunc.cwtSpark.TryGetValue(self.abstractCreature, out var SCM))
//         {
//             Creature.Grasp[] grasps = self.grasps;
//             for (int i = 0; i < grasps.Length; i++)
//             {
//                 if (grasps[i] != null && grasps[i].grabbed is IPlayerEdible && (grasps[i].grabbed as IPlayerEdible).Edible)
//                 {
//                     return null;
//                 }
//             }
//             if (grasps[0] != null && grasps[0].grabbed is Spear spear1 && StaticManager_CanCraftSpear(SCM, spear1))
//             {
//                 return AbstractPhysicalObject.AbstractObjectType.Spear;
//             }
//             if (self.objectInStomach == null && grasps[0] == null && grasps[1] != null && grasps[1].grabbed is Spear spear2 && StaticManager_CanCraftSpear(SCM, spear2))
//             {
//                 return AbstractPhysicalObject.AbstractObjectType.Spear;
//             }
// 			return null;
//         }
//         return orig(self);
//     }
//     private static bool Player_StaticChargeManager_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
//     {
//         if (SparkFunc.cwtSpark.TryGetValue(self.abstractCreature, out var _))
//         {
//             return self.CraftingResults() != null;
//         }
//         return orig(self);
//     }
// }
