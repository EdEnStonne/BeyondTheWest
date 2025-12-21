// using UnityEngine;
// using System;
// using MonoMod.Cil;
// using RWCustom;
// using Mono.Cecil.Cil;
// using BeyondTheWest;
// using System.Collections.Generic;
// using Noise;
// using RainMeadow;

// public class SparkObject
// {
//     // Variables

//     // Objects
//     public class StaticChargeManager
//     {
//         public StaticChargeManager(AbstractCreature abstractpl)
//         {
//             this.AbstractPlayer = abstractpl;
//             InitPlayerStaticCharge();
//         }

//         //-------------- Local Functions

//         // Init
//         private void InitPlayerStaticCharge()
//         {
//             if (this.AbstractPlayer != null && this.Player != null && this.Room != null)
//             {
//                 this.init = true;
//                 if (SparkFunc.IsSpark(this.Player))
//                 {
//                     this.particles = true;
//                     this.active = true;
//                     this.isMeadow = false;
//                     this.isMeadowFakePlayer = false;
//                     this.Charge = 0f;
//                     this.dischargeCooldown = 1;
//                     this.DoDischargeDamagePlayers = BTWRemix.DoSparkShockSlugs.Value || this.Room.game.IsArenaSession;
//                     this.RiskyOvercharge = BTWRemix.SparkRiskyOvercharge.Value;
//                     this.DeathOvercharge = BTWRemix.SparkDeadlyOvercharge.Value;

//                     if (BTWRemix.DoDisplaySparkBattery.Value && this.active)
//                     {
//                         this.displayBattery = true;
//                     }
//                     if (Plugin.meadowEnabled)
//                     {
//                         MeadowCompat.SparkMeadow_Init(this);
//                     }
//                 }
//                 else
//                 {
//                     this.active = false;
//                 }
//                 Plugin.Log("Spark manager Init ! " + this.init + "/" + this.particles + "/" + this.active + "/" + this.isMeadow + "/" + this.isMeadowFakePlayer + "/" + this.displayBattery + "/" + this.dischargeCooldown);
//             }
//         }

//         // Updates
//         private void ParticlesUpdate()
//         {
//             float FractCharge = this.Charge / this.FullECharge;
//             Player player = this.Player;
//             Room room = this.Room;
//             Color color = player.ShortCutColor();
//             Vector2 pos = player.mainBodyChunk.pos;

//             if (UnityEngine.Random.Range(0f, 1f) * 5f < FractCharge)
//             {
//                 room.AddObject(new MouseSpark(pos, new Vector2(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f)), 15f, color));
//             }
//             if (IsOvercharged)
//             {
//                 float FractOvercharge = (this.Charge - this.FullECharge) / (this.MaxECharge - this.FullECharge);
//                 if (UnityEngine.Random.Range(0f, 1f) < Mathf.Pow(FractOvercharge, 2)/2)
//                 {
//                     room.AddObject(new Spark(pos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)) * FractOvercharge, color, null, 5, 20));
                    
//                     room.PlaySound(SoundID.Centipede_Shock, player.mainBodyChunk, false, Mathf.Pow(FractOvercharge, 1f/2f) / 2f, UnityEngine.Random.Range(1.25f, 1.75f));
//                     if (this.active)
//                     {
//                         this.Charge -= FractOvercharge * 0.25f;
//                         foreach (BodyChunk bodyChunk in this.Player.bodyChunks)
//                         {
//                             bodyChunk.vel.x += UnityEngine.Random.Range(-1f, 1f) * FractOvercharge * 0.5f;
//                             bodyChunk.vel.y += UnityEngine.Random.Range(-1f, 1f) * FractOvercharge * 0.2f;
//                         }
//                     }
//                 }
//             }
//         }
//         private void BounceUpdate()
//         {
//             Player player = this.Player;
//             Player.InputPackage inputs = this.Player.input[0];
//             Player.InputPackage lastInputs = this.Player.input[1];
//             Vector2 intInput = this.IntDirectionalInput;

//             if (this.Landed)
//             {
//                 this.eBounceLeft = this.MaxEBounce;
//             }
//             else if (this.eBounceLeft > 0 && this.dischargeCooldown <= 0 && player.stun <= 0)
//             {
//                 if (
//                     (player.animation == Player.AnimationIndex.Flip || this.rocketJumpFromBounceJump) &&
//                     inputs.spec && !lastInputs.spec
//                 )
//                 {
//                     this.eBounceLeft--;
//                     this.BounceUp(IsOvercharged);
//                 }
//                 else if (
//                     player.animation == Player.AnimationIndex.RocketJump &&
//                     inputs.spec && !lastInputs.spec && intInput.x * player.mainBodyChunk.vel.x < 0
//                 )
//                 {
//                     this.eBounceLeft--;
//                     Plugin.Log("Spark Jump Tech");
//                     this.BounceBack(IsOvercharged);
//                 }
//             }
//         }
//         private void DischargeUpdate()
//         {
//             Player player = this.Player;
//             Vector2 pos = player.mainBodyChunk.pos;
//             Player.InputPackage inputs = this.Player.input[0];
//             Vector2 dirInput = this.DirectionalInput;
//             Vector2 intInput = this.IntDirectionalInput;

//             bool overcharged = this.IsOvercharged;
//             if (this.rocketJumpFromBounceJump && this.Player.canJump > 0)
//             {
//                 this.rocketJumpFromBounceJump = false;
//             }
//             if (inputs.spec && this.dischargeCooldown <= 0)
//             {
//                 if (player.animation == Player.AnimationIndex.ClimbOnBeam)
//                 {
//                     bool success = Discharge(
//                         overcharged ? 125f : 75f,
//                         overcharged ? 0.35f : 0.25f,
//                         overcharged ? 80f : 50.0f,
//                         pos
//                     );
//                     if (overcharged && success && !this.isMeadowFakePlayer)
//                     {
//                         foreach (BodyChunk b in player.bodyChunks)
//                         {
//                             b.vel.x += UnityEngine.Random.Range(-10f, 10f);
//                             b.vel.y += UnityEngine.Random.Range(-10f, 10f);
//                         }
//                     }
//                 }
//                 else if (player.animation == Player.AnimationIndex.Roll || player.animation == Player.AnimationIndex.BellySlide)
//                 {
//                     if (player.animation == Player.AnimationIndex.BellySlide 
//                         && intInput.x == -player.rollDirection 
//                         && intInput.y == -1
//                         && this.boostSlideBuffer < -20)
//                     {
//                         SlideBoost(overcharged);
//                     }
//                     else
//                     {
//                         BounceJump(overcharged);
//                         this.rocketJumpFromBounceJump = true;
//                     }
//                 }
//                 else
//                 {
//                     Vector2 lookPos = intInput != Vector2.zero ? dirInput : new Vector2(player.ThrowDirection, 0);
//                     Vector2 dischargePos = pos + (player.bodyMode == Player.BodyModeIndex.WallClimb ? lookPos * -15f : lookPos * 25f);
//                     bool success = Discharge(
//                         overcharged ? 45f : 30f,
//                         overcharged ? 1.75f : 1.15f,
//                         overcharged ? 50f : 30f,
//                         dischargePos
//                     );

//                     if (success && !this.isMeadowFakePlayer)
//                     {
//                         if (player.bodyMode != Player.BodyModeIndex.WallClimb)
//                         {
//                             foreach (BodyChunk b in player.bodyChunks)
//                             {
//                                 b.vel.x -= UnityEngine.Random.Range(5f, 7f) * lookPos.x;
//                                 b.vel.y -= UnityEngine.Random.Range(6f, 8f) * lookPos.y;
//                             }
//                         }
//                         if (overcharged && this.endlessCharge <= 0 && this.overchargeImmunity <= 0)
//                         {
//                             player.stun = Mathf.Max(player.stun, 30);;
//                         }
//                     }
//                 }
//             }
//         }
//         private void OverchargeUpdate()
//         {
//             float FractOvercharge = (this.Charge - this.FullECharge) / (this.MaxECharge - this.FullECharge);

//             Player player = this.Player;
//             Room room = this.Room;
//             Vector2 pos = player.mainBodyChunk.pos;
//             Color color = player.ShortCutColor();
//             bool forceDischarge = false;

//             if (this.Charge >= this.MaxECharge)
//             {
//                 if (this.DeathOvercharge && this.CrawlChargeRatio <= 0)
//                 {
//                     for (int i = (int)UnityEngine.Random.Range(15f, 25f); i >= 0; i--)
//                     {
//                         room.AddObject(new MouseSpark(pos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)), 50f, color));
//                     }

//                     room.ScreenMovement(pos, default, 1.1f);
//                     room.PlaySound(SoundID.Bomb_Explode, pos);
//                     room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, pos);
//                     room.InGameNoise(new Noise.InGameNoise(pos, 900f, player, 1f));
//                     Discharge(this.FullECharge * 1.5f, 1.0f, 0, pos, 1.5f);
//                     this.Charge = 0;
//                     Plugin.Log("Seems like Spark "+ player.ToString() +" couldn't handle the charge...");
//                     player.Die();
//                     return;
//                 }
//                 else
//                 {
//                     this.Charge = this.MaxECharge;
//                     forceDischarge = true;
//                 }
//             }
//             if (this.RiskyOvercharge)
//             {
//                 bool isSwimming = player.bodyMode == Player.BodyModeIndex.Swimming;
//                 if (forceDischarge 
//                     || (this.CrawlChargeRatio <= 0 
//                         && (
//                             player.bodyMode == Player.BodyModeIndex.Stand ||
//                             player.bodyMode == Player.BodyModeIndex.Crawl ||
//                             isSwimming
//                         ) 
//                         && UnityEngine.Random.Range(0f, 1f) < Mathf.Pow(FractOvercharge, 4) * (isSwimming ? 0.01f : 0.025f)))
//                 {
//                     DischargeStun();
//                 }
//             }
//         }
//         private void RechargeUpdate()
//         {
//             if (this.Player != null && !this.Player.dead && (this.dischargeCooldown <= 0 || this.CrawlChargeConditionMet || this.isMeadowArenaTimerCountdown))
//             {
//                 if (this.isMeadowArenaTimerCountdown && this.IsOvercharged)
//                 {
//                     return;
//                 }
//                 this.Charge += this.ChargePerSecond / 40f;
//             }
//         }
//         private void SlideUpdate()
//         {
//             if (this.Player != null && this.Player.rollCounter > 8 && this.Player.animation == Player.AnimationIndex.BellySlide)
//             {
//                 bool overcharged = this.IsOvercharged;
//                 BodyChunk bodyChunk = this.Player.bodyChunks[0];
//                 Player player = this.Player;

//                 player.rollCounter = 12;
//                 bodyChunk.vel.x = this.CurrentSlideMomentum * player.rollDirection;

//                 if (this.slideSpeedframes < this.SlideAccelerationFrames) { this.slideSpeedframes++; }
//                 if (this.slideSpearBounceFrames > 0) { this.slideSpearBounceFrames--; }
//                 if (this.slideSpeedMult > 1f) 
//                     { this.slideSpeedMult = Mathf.Clamp(Mathf.Lerp(this.slideSpeedMult, 1f, 0.05f), 1f, 5f); }

//                 if (overcharged && this.active)
//                 {
//                     float amountOvercharge = this.Charge - this.FullECharge;
//                     float fractOvercharge = amountOvercharge / (this.MaxECharge - this.FullECharge);
//                     const float criticAmount = 0.25f;

//                     this.slideSpeedMult = Math.Max(this.slideSpeedMult, 1f + fractOvercharge / 2f);
//                     if (fractOvercharge >= criticAmount)
//                     {
//                         this.Charge -= amountOvercharge * Mathf.Pow((fractOvercharge - criticAmount) / (1 - criticAmount), 4);
//                     }
//                 }

//                 if (this.boostSlideBuffer > 0)
//                 {
//                     player.exitBellySlideCounter = 0;
//                 }

//                 if (player.whiplashJump)
//                 {
//                     if (player.input[0].x == -player.rollDirection)
//                     {
//                         this.whiplashJumpBuffer = this.MaxWhiplashJumpBuffer;
//                     }
//                     else
//                     {
//                         if (this.whiplashJumpBuffer > 0) { this.whiplashJumpBuffer--; }
//                         else { player.whiplashJump = false; }
//                     }
//                 }
//                 else if (this.whiplashJumpBuffer > 0) { this.whiplashJumpBuffer = 0; }
//                 if (this.boostSlideBuffer > -100) { this.boostSlideBuffer--; }
//             }
//             else
//             {
//                 // this.slideOverchargedBoost = false;
//                 this.slideSpeedframes = 0;
//                 this.slideSpeedMult = 1f;
//                 this.slideSpearBounceFrames = 0;
//                 this.whiplashJumpBuffer = 0;
//                 this.boostSlideBuffer = -100;
//             }
//         }
//         private void MovementUpdate()
//         {
//             Player player = this.Player;
//             oldpos = newpos;

//             if (player != null && player.room != null && player.bodyChunks != null && !player.inShortcut)
//             {
//                 Vector2 pos = Vector2.zero;
//                 foreach (BodyChunk bodyChunk in player.bodyChunks)
//                 {
//                     pos += bodyChunk.pos;
//                 }
//                 pos /= player.bodyChunks.Length;

//                 if (oldpos == Vector2.zero)
//                 {
//                     oldpos = pos;
//                 }
//                 newpos = pos;
//             }
//             else
//             {
//                 oldpos = Vector2.zero;
//                 newpos = Vector2.zero;
//             }
//         }
//         private void CrawlChargeUpdate()
//         {
//             Player player = this.Player;
//             Room room = this.Room;

//             if (this.CrawlChargeConditionMet)
//             {
//                 Vector2 pos = player.mainBodyChunk.pos;
//                 Color color = player.ShortCutColor();
//                 float chargeFraction = this.CrawlChargeRatio;
//                 float chargelikeelectricRatio = this.Charge / (this.FullECharge > 0 ? this.FullECharge : this.MaxECharge);

//                 this.dischargeCooldown = Mathf.Max(10, this.dischargeCooldown);
//                 if (!this.IsOvercharged)
//                 {
//                     this.crawlCharge++;
//                     player.Blink(5);
//                 }
//                 else
//                 {
//                     this.crawlCharge--;
//                 }
                
//                 if (ModManager.MSC && BTWFunc.Random() < 0.05f + 0.1f * chargelikeelectricRatio)
//                 {
//                     MoreSlugcatCompat.LightingArc lightingArc = new MoreSlugcatCompat.LightingArc(
//                         player.bodyChunks[0], player.bodyChunks[1],
//                         1f + 3f * chargelikeelectricRatio, 0.25f + 0.25f * chargelikeelectricRatio, 5, color
//                     )
//                     {
//                         fromOffset = BTWFunc.RandomCircleVector(player.bodyChunks[0].rad * BTWFunc.Random() * 1.5f),
//                         targetOffset = BTWFunc.RandomCircleVector(player.bodyChunks[1].rad * BTWFunc.Random() * 1.5f)
//                     };
//                     room.AddObject(lightingArc);
//                 }
                
//                 if (BTWFunc.Random() < 0.25f)
//                 {
//                     room.AddObject(new Spark(pos, BTWFunc.RandomCircleVector(10f), color, null, 5, 20));
//                     room.PlaySound(SoundID.Centipede_Shock, player.mainBodyChunk, false, 0.15f * chargeFraction, UnityEngine.Random.Range(1.25f, 1.75f));
//                     if (this.active) { room.InGameNoise(new Noise.InGameNoise(pos, 1000f * chargeFraction, player, 1f)); }
//                 }
//             }
//             else if (this.crawlCharge > 0)
//             {
//                 if (this.crawlCharge > 200) { this.crawlCharge = 200; }
//                 this.crawlCharge--;
//             }
//         }
//         private void DebugUpdate()
//         {
//             Player player = this.Player;
//             Room room = this.Room;
//             if (player == null || room == null || !room.game.devToolsActive)
//             {
//                 return;
//             }
            
//             try
//             {
//                 if (Input.GetKeyDown(KeyCode.T))
//                 {
//                     if (Input.GetKey(KeyCode.LeftShift))
//                     {
//                         this.overchargeImmunity = this.overchargeImmunity > 0 ? 0 : int.MaxValue;
//                     }
//                     else if (Input.GetKey(KeyCode.LeftControl))
//                     {
//                         this.endlessCharge = this.endlessCharge > 0 ? 0 : int.MaxValue;
//                     }
//                     else
//                     {
//                         this.Charge = this.FullECharge > 0 ? this.FullECharge : this.MaxECharge;
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Plugin.logger.LogError(ex);
//             }
//         }
        
//         //-------------- Override Functions
//         public void Update()
//         {
//             if (!this.init)
//             {
//                 InitPlayerStaticCharge();
//                 return;
//             }

//             if (this.displayBattery && this.Room != null)
//             {
//                 if (this.staticChargeBatteryUI == null)
//                 {
//                     this.staticChargeBatteryUI = new StaticChargeBatteryUI(this);
//                     this.Room.AddObject(this.staticChargeBatteryUI);
//                 }
//                 if (this.staticChargeBatteryUI != null && this.Room != this.staticChargeBatteryUI.room)
//                 {
//                     this.staticChargeBatteryUI.RemoveFromRoom();
//                     this.Room.AddObject(this.staticChargeBatteryUI);
//                 }
//             }
//             else if (!this.displayBattery && this.Room != null && this.staticChargeBatteryUI != null)
//             {
//                 this.Room.RemoveObject(this.staticChargeBatteryUI);
//                 this.staticChargeBatteryUI.Destroy();
//                 this.staticChargeBatteryUI = null;
//             }

//             if (this.Room != null && this.Player != null)
//             {
//                 CrawlChargeUpdate();
//                 if (this.active && !this.Player.dead)
//                 {
//                     if (!this.consideredAlive)
//                     {
//                         this.consideredAlive = true;
//                         this.overchargeImmunity = Mathf.Max(this.overchargeImmunity, 10);
//                     }
//                     if (this.Room.game.devToolsActive) { DebugUpdate(); }
//                     if (Plugin.meadowEnabled && this.isMeadowArenaTimerCountdown && OnlineTimerOn())
//                     {
//                         this.dischargeCooldown = 3;
//                     }
//                     else
//                     {
//                         if (this.dischargeCooldown > 0) { this.dischargeCooldown--; }
//                         if (this.isMeadowArenaTimerCountdown) { this.isMeadowArenaTimerCountdown = false; }
//                     }
//                     if (this.MaxEBounce > 0 && !this.isMeadowArenaTimerCountdown) { BounceUpdate(); }
//                     if (!this.isMeadowArenaTimerCountdown) { DischargeUpdate(); }
//                     if (this.RechargeMult > 0 && !this.isMeadowFakePlayer) { MovementUpdate(); RechargeUpdate(); }
//                     if (!this.isMeadowFakePlayer) { SlideUpdate(); }
//                     if (this.IsOvercharged 
//                         && !this.isMeadowFakePlayer 
//                         && !this.isMeadowArenaTimerCountdown 
//                         && this.overchargeImmunity <= 0
//                         && this.endlessCharge <= 0) { OverchargeUpdate(); }
//                     if (!this.isMeadowFakePlayer)
//                     {
//                         if (this.overchargeImmunity > 0) { 
//                             this.overchargeImmunity--; 
//                             if (this.overchargeImmunity <= 0 && this.Charge >= this.MaximumCharge)
//                             {
//                                 DischargeStun();
//                             }
//                         }
//                         if (this.endlessCharge > 0) { 
//                             this.endlessCharge--; 
//                             this.charge = this.FullECharge > 0 ? this.FullECharge : this.MaxECharge / 2f;
//                         }
//                     }
//                 }
//                 if (this.particles) {
//                     ParticlesUpdate(); 
//                 }
//             }
//         }

//         //-------------- Public Functions
//         public void DoSparks()
//         {
//             if (this.Player != null)
//             {
//                 DoSparks(this.Player.firstChunk.pos + 25f * (this.DirectionalInput == Vector2.zero ? new Vector2(this.Player.ThrowDirection, 0) : this.DirectionalInput));
//             }
//         }
//         public void DoSparks(Vector2 position)
//         {
//             if (this.Room != null)
//             {
//                 this.Charge -= 0.1f;
//                 this.Room.AddObject(new MouseSpark(position, new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f)), 5f, this.Player.ShortCutColor()));
//             }
//         }

//         public bool Discharge(float reach, float damage, float chargeNeeded)
//         {
//             if (this.Player == null) { return false; }
//             return Discharge(reach, damage, chargeNeeded, Player.firstChunk.pos);
//         }
//         public bool Discharge(float reach, float damage, float chargeNeeded, Vector2 position)
//         {
//             return Discharge(reach, damage, chargeNeeded, position, reach * damage / 150f);
//         }
//         public bool Discharge(float reach, float damage, float chargeNeeded, Vector2 position, float volume)
//         {
//             if (this.Player == null || this.Room == null) { return false; }
            
//             Player pl = this.Player;
//             Room room = this.Room;
//             Color color = pl.ShortCutColor();

//             if (this.Charge >= chargeNeeded && this.dischargeCooldown <= 0)
//             {
//                 this.dischargeCooldown = this.MaxDischargeCooldown;
//                 this.Charge -= chargeNeeded;
//                 bool underwater = false;

//                 if (BTWFunc.BodyChunkSumberged(pl.firstChunk))
//                 {
//                     reach *= 4;
//                     damage *= 0.75f;
//                     underwater = true;
//                 }
//                 int stun = (int)(((damage + 1) * 0.5f + reach * 0.01f + (underwater ? 1 : 0)) * BTWFunc.FrameRate);

//                 ElectricExplosion electricExplosion = new(room, pl, position, 1, reach, 7.5f * damage,
//                     damage, stun, pl, 0, 0.7f, underwater, false, this.isMeadow && this.active)
//                 {
//                     color = color,
//                     forcedKnockbackDirection = new Vector2(pl.ThrowDirection, damage),
//                     hitNonSubmerged = !underwater,
//                     hitPlayer = this.DoDischargeDamagePlayers,
//                     volume = volume
//                 };
//                 room.AddObject( electricExplosion );

//                 return true;
//             }
//             else
//             {
//                 DoSparks(position);
//             }
//             return false;
//         }

//         public void BounceUp(bool overcharged)
//         {
//             if (this.Room != null)
//             {
//                 bool success = Discharge(
//                     overcharged ? 25f : 20f,
//                     overcharged ? 0.70f : 0.25f,
//                     overcharged ? 25f : 15f,
//                     this.Player.firstChunk.pos + Vector2.down * 25f
//                 );

//                 if (success && !this.isMeadowFakePlayer)
//                 {
//                     this.dischargeCooldown /= 2;
//                     float yBounce = overcharged ? 17.5f : 13.5f;
//                     if (overcharged) { this.Player.flipFromSlide = true; }
//                     foreach (BodyChunk bodyChunk in this.Player.bodyChunks)
//                     {
//                         bodyChunk.vel.x *= overcharged ? 1.25f : 1.1f;
//                         bodyChunk.vel.y = Math.Max(yBounce + bodyChunk.vel.y, yBounce);
//                     }
//                 }
//             }
//         }
//         public void BounceBack(bool overcharged)
//         {
//             if (this.Room != null)
//             {
//                 Player player = this.Player;
//                 int direction = (int)Mathf.Sign(player.mainBodyChunk.vel.x);
//                 bool success = Discharge(
//                     overcharged ? 35f : 25f,
//                     overcharged ? 1.25f : 0.5f,
//                     overcharged ? 30f : 20f,
//                     player.firstChunk.pos + new Vector2(direction, -0.1f) * 30f
//                 );

//                 if (success && !this.isMeadowFakePlayer)
//                 {
//                     this.dischargeCooldown /= 2;
//                     float yboost = overcharged ? 15f : 12.5f;

//                     this.Room.PlaySound(SoundID.Slugcat_Sectret_Super_Wall_Jump, player.mainBodyChunk, false, 1f, 1.25f);
//                     player.bodyChunks[1].pos = player.bodyChunks[0].pos;
//                     player.bodyChunks[0].pos += new Vector2(direction * -10f, 10f);
//                     player.rollDirection = -direction;
//                     player.slideDirection = -direction;
//                     // player.animation = Player.AnimationIndex.Flip;
//                     // Plugin.Log("BounceBack ! \nPlayer current direction : " + direction
//                     //     + "\nWanted direction : " + (-direction)
//                     //     + "\nPlayer stats : " 
//                     //     + "\nflip : " + player.flipDirection
//                     //     + "\nroll : " + player.rollDirection
//                     //     + "\nslide : " + player.slideDirection
//                     //     + "\nthrow : " + player.ThrowDirection
//                     //     + "\nanim : " + player.animation
//                     //     + "\nbody : " + player.bodyMode
//                     // );

//                     foreach (BodyChunk bodyChunk in player.bodyChunks)
//                     {
//                         bodyChunk.vel.x = (overcharged ? 17.5f : 10f) * -direction;
//                         bodyChunk.vel.y = Math.Max(yboost + bodyChunk.vel.y, yboost);
//                     }
//                     player.jumpStun = 20;
//                 }
//             }
//         }
//         public void BounceJump(bool overcharged)
//         {
//             if (this.Room != null)
//             {
//                 bool success = Discharge(
//                     overcharged ? 70f : 50f,
//                     overcharged ? 1.25f : 0.75f,
//                     overcharged ? 50f : 35f,
//                     this.Player.firstChunk.pos
//                 );

//                 if (success && !this.isMeadowFakePlayer)
//                 {
//                     this.dischargeCooldown /= 2;
//                     this.Player.Jump();
//                     if (overcharged)
//                     {
//                         if (this.Player.animation != Player.AnimationIndex.Flip)
//                         { this.Player.animation = Player.AnimationIndex.Flip; }
//                         else
//                         { this.Player.flipFromSlide = true; }
//                     }

//                     foreach (BodyChunk bodyChunk in this.Player.bodyChunks)
//                     {
//                         bodyChunk.vel.x *= overcharged ? 1.5f : 1.35f;
//                         bodyChunk.vel.y *= overcharged ? 1.85f : 1.5f;
//                     }
//                 }
//             }
//         }
//         public void SlideBoost(bool overcharged)
//         {
//             if (this.Room != null)
//             {
//                 BodyChunk bodyChunk = this.Player.bodyChunks[0];
//                 Player player = this.Player;
//                 bool success = Discharge(
//                     overcharged ? 50f : 40f,
//                     overcharged ? 0.2f : 0.05f,
//                     overcharged ? 35f : 10f,
//                     bodyChunk.pos + new Vector2(40f * -player.slideDirection, 0),
//                     0.5f
//                 );

//                 if (success && !this.isMeadowFakePlayer)
//                 {
//                     this.dischargeCooldown = 10;
//                     this.slideSpearBounceFrames = overcharged ? 60 : 20;
//                     this.boostSlideBuffer = 20;

//                     this.slideSpeedframes = Math.Min(this.slideSpeedframes + (overcharged ? 100 : 25), this.SlideAccelerationFrames);
//                     this.slideSpeedMult *= overcharged ? this.SlideOverchargeBoostMult : this.SlideBoostMult;
//                 }
//             }
//         }
        
//         public void RechargeFromExternalSource(Vector2 sourcePos, float chargeAdded)
//         {
//             this.Charge += chargeAdded;
//             Player player = this.Player;
//             if (ModManager.MSC && player != null && player.room != null) 
//             {
//                 MoreSlugcatCompat.LightingArc lightingArc = new MoreSlugcatCompat.LightingArc(
//                     sourcePos, player.mainBodyChunk, 
//                     Mathf.Clamp(chargeAdded / 10f, 1, 20), Mathf.Clamp01(chargeAdded / 50f), 10, player.ShortCutColor());
//                 player.room.AddObject(lightingArc);
//             }
//         }
//         public void RechargeFromExternalSource(BodyChunk sourceChunk, float chargeAdded)
//         {
//             this.Charge += chargeAdded;
//             Player player = this.Player;
//             if (ModManager.MSC && player != null && player.room != null) 
//             {
//                 MoreSlugcatCompat.LightingArc lightingArc = new MoreSlugcatCompat.LightingArc(
//                     sourceChunk, player.mainBodyChunk, 
//                     Mathf.Clamp(chargeAdded / 10f, 1, 20), Mathf.Clamp01(chargeAdded / 50f), 10, player.ShortCutColor());
//                 player.room.AddObject(lightingArc);
//             }
//         }
        
//         public void DischargeStun()
//         {
            
//             Player player = this.Player;
//             Room room = this.Room;

//             if (player != null && room != null)
//             {
//                 Vector2 pos = player.mainBodyChunk.pos;
//                 bool isSwimming = player.bodyMode == Player.BodyModeIndex.Swimming;

//                 float FractCharge = this.Charge / this.CapacityCharge;
//                 float OverchargeMargin = this.MaximumCharge - this.CapacityCharge;
//                 float OverchargeCharge = this.Charge - this.CapacityCharge;
//                 float FractOvercharge = OverchargeMargin > 0 && OverchargeCharge > 0 ? OverchargeCharge / OverchargeMargin : FractCharge;

//                 Discharge(200f, 0.5f, 0, pos, 0.75f);
//                 if (OverchargeMargin > 0)
//                 {
//                     this.Charge -= OverchargeMargin;
//                 }
//                 else
//                 {
//                     this.Charge = 0;
//                 }
//                 player.Stun((int)((this.MaxECharge - this.FullECharge) * (FractOvercharge * 3f + 1f) / (isSwimming ? 2 : 1) ));
//                 room.AddObject(new CreatureSpasmer(player, false, player.stun));
//                 player.LoseAllGrasps();
//             }
//         }
        
//         //-------------- Variables

//         // Object Variables
//         public AbstractCreature AbstractPlayer;
//         public StaticChargeBatteryUI staticChargeBatteryUI;

//         // Basic Variables
//         private float charge = 0f;

//         public int slideSpeedframes = 0;
//         public int slideSpearBounceFrames = 0;
//         public float slideSpeedMult = 1f;
//         public int eBounceLeft = 0;
//         public int dischargeCooldown = 20;
//         public int overchargeImmunity = 0;
//         public int endlessCharge = 0;
//         public int crawlCharge = 0;
//         public int whiplashJumpBuffer = 0;
//         public int boostSlideBuffer = 0;
        
//         public float MaxECharge = 200.0f;
//         public float FullECharge = 100.0f;
//         public float RechargeMult = 3f;
//         public float InitSlideSpeed = 5f;
//         public float MaxSlideSpeed = 45f;
//         public int SlideAccelerationFrames = 200;
//         public float SlideBoostMult = 1.35f;
//         public float SlideOverchargeBoostMult = 1.65f;
//         public int MaxDischargeCooldown = 40;
//         public int MaxEBounce = 1;
//         public int MaxWhiplashJumpBuffer = 5;

//         public bool rocketJumpFromBounceJump = false;
//         // public bool slideOverchargedBoost = false;
//         public bool displayBattery = false;
//         public bool particles = false;
//         public bool active = false;
//         public bool DoDischargeDamagePlayers = true;
//         public bool RiskyOvercharge = true;
//         public bool DeathOvercharge = true;
//         public bool init = false;
//         public bool isMeadowFakePlayer = false;
//         public bool isMeadow = false;
//         public bool isMeadowArena = false;
//         public bool isMeadowArenaTimerCountdown = false;
//         public bool consideredAlive = false;
//         public Vector2 oldpos = Vector2.zero;
//         public Vector2 newpos = Vector2.zero;

//         // Get Set Variables
//         public Player Player
//         {
//             get
//             {
//                 if (AbstractPlayer != null && AbstractPlayer.realizedCreature != null && AbstractPlayer.realizedCreature is Player pl)
//                 {
//                     return pl;
//                 }
//                 return null;
//             }
//         }
//         public Room Room
//         {
//             get
//             {
//                 if (Player != null && Player.room != null)
//                 {
//                     return Player.room;
//                 }
//                 return null;
//             }
//         }
//         public float ChargePerSecond
//         {
//             get
//             {
//                 if ((this.dischargeCooldown > 0 && !this.CrawlChargeConditionMet && !this.isMeadowArenaTimerCountdown) || !active || this.endlessCharge > 0) { return 0f; }
//                 Player player = this.Player;
//                 if (player != null)
//                 {
//                     float ActionFriction = 0.5f;
//                     float BodyFriction = 0.5f;
//                     float ContactFriction = 0f;
//                     Vector2 intDir = this.IntDirectionalInput;

//                     if (intDir == Vector2.zero || Mathf.Abs(oldpos.magnitude - newpos.magnitude) < 0.2f)
//                         { ContactFriction = 0f; }
//                     else
//                     {
//                         ContactFriction += Mathf.Pow(
//                             (player.bodyChunks[0].vel.magnitude + player.bodyChunks[1].vel.magnitude) / 2f, 2)
//                             / 50f;
//                         ContactFriction = Mathf.Clamp(ContactFriction, 0, 10);
//                     }
//                     if (this.CrawlChargeRatio > 0)
//                     {
//                         ContactFriction = Mathf.Max(ContactFriction, this.CrawlChargeRatio * 0.15f);
//                     }

//                     if (this.IsCrawlingOnFloor)
//                     {
//                         BodyFriction = 100f;
//                     }
//                     else if (player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
//                     {
//                         BodyFriction = 0.2f;
//                     }
//                     else if (player.bodyMode == Player.BodyModeIndex.ClimbIntoShortCut)
//                     {
//                         BodyFriction = 5f;
//                     }
//                     else if (player.bodyMode == Player.BodyModeIndex.CorridorClimb || player.bodyMode == Player.BodyModeIndex.Crawl)
//                     {
//                         BodyFriction = 10f;
//                     }
//                     else if (player.bodyMode == Player.BodyModeIndex.Stand || player.bodyMode == Player.BodyModeIndex.Default)
//                     {
//                         BodyFriction = 1f;
//                     }
//                     else if (player.bodyMode == Player.BodyModeIndex.Swimming)
//                     {
//                         BodyFriction = 2.5f;
//                     }
//                     else if (player.bodyMode == Player.BodyModeIndex.WallClimb)
//                     {
//                         BodyFriction = 50f;
//                     }
//                     else if (player.bodyMode == Player.BodyModeIndex.ZeroG)
//                     {
//                         BodyFriction = 0.5f;
//                     }

//                     if (!this.Landed)
//                     {
//                         ActionFriction = 0.5f;
//                     }

//                     if (player.inShortcut)
//                     {
//                         ActionFriction = 0.05f;
//                     }
//                     if (this.IsOvercharged)
//                     {
//                         ActionFriction *= 0.5f;
//                     }

//                     if (player.animation == Player.AnimationIndex.Roll)
//                     {
//                         ActionFriction = 20f;
//                     }
//                     else if (player.animation == Player.AnimationIndex.BellySlide)
//                     {
//                         ActionFriction = 4f;
//                     }
//                     else if (player.animation == Player.AnimationIndex.RocketJump)
//                     {
//                         ActionFriction = 0.1f;
//                     }

//                     return this.RechargeMult * ActionFriction * BodyFriction * ContactFriction;
//                 }
//                 return 0f;
//             }
//         }
//         public Vector2 DirectionalInput
//         {
//             get
//             {
//                 if (this.Player != null)
//                 {
//                     Player.InputPackage cinput = this.Player.input[0];
//                     bool isPC = cinput.controllerType == Options.ControlSetup.Preset.KeyboardSinglePlayer;
//                     return isPC ? new Vector2(cinput.x, cinput.y) : cinput.analogueDir;
//                 }
//                 return Vector2.zero;
//             }
//         }
//         public Vector2 IntDirectionalInput
//         {
//             get
//             {
//                 int x = 0; int y = 0;
//                 Vector2 dirInput = this.DirectionalInput;
//                 if (dirInput.x > 0.25f) { x = 1; }
//                 else if (dirInput.x < -0.25f) { x = -1; }
//                 if (dirInput.y > 0.25f) { y = 1; }
//                 else if (dirInput.y < -0.25f) { y = -1; }
//                 return new Vector2(x, y);
//             }
//         }
//         public float Charge
//         {
//             get
//             {
//                 return this.charge;
//             }
//             set
//             {
//                 float maxcharge = Mathf.Max(this.MaxECharge, this.FullECharge);
//                 if (this.charge + value > maxcharge 
//                     && !this.RiskyOvercharge 
//                     && !this.DeathOvercharge)
//                 {
//                     this.endlessCharge += (int)(maxcharge - (this.charge + value));
//                 }
//                 this.charge = Mathf.Clamp(value, 0, maxcharge);
//             }
//         }
//         public bool IsOvercharged
//         {
//             get
//             {
//                 return this.endlessCharge > 0 || (this.MaxECharge > this.FullECharge && this.Charge > this.FullECharge);
//             }
//         }
//         public bool Landed
//         {
//             get
//             {
//                 Player player = this.Player;
//                 if (player == null) { return false; }
//                 return player.canJump > 0 
//                     || player.bodyMode == Player.BodyModeIndex.CorridorClimb 
//                     || player.bodyMode == Player.BodyModeIndex.Swimming;
//             }
//         }
//         public float CurrentSlideMomentum
//         {
//             get
//             {
//                 return Mathf.Lerp(
//                     this.InitSlideSpeed,
//                     this.MaxSlideSpeed,
//                     BTWFunc.EaseOut((float)this.slideSpeedframes / this.SlideAccelerationFrames, 2)
//                 ) * this.slideSpeedMult;
//             }
//         }
//         public bool IsCrawlingOnFloor
//         {
//             get
//             {
//                 Player player = this.Player;
//                 if (player != null)
//                 {
//                     return player.bodyMode == Player.BodyModeIndex.Crawl 
//                         && player.bodyChunks[0].ContactPoint.y < 0 
//                         && player.bodyChunks[1].ContactPoint.y < 0;
//                 }
//                 return false;
//             }
//         }
//         public float CrawlChargeRatio
//         {
//             get
//             {
//                 return BTWFunc.EaseOut( Mathf.Clamp01(this.crawlCharge / 200f), 1.5f);
//             }
//         }
//         public bool CrawlChargeConditionMet
//         {
//             get
//             {
//                 Player player = this.Player;
//                 Room room = this.Room;
//                 Vector2 intdir = this.IntDirectionalInput;

//                 return player != null && room != null 
//                     && !player.inShortcut && this.IsCrawlingOnFloor 
//                     && intdir.y == -1 && intdir.x == 0 
//                     && player.input[0].spec;
//             }
//         }
//         public float MaximumCharge
//         {
//             get
//             {
//                 return Mathf.Max(this.MaxECharge, this.FullECharge);
//             }
//         }
//         public float CapacityCharge
//         {
//             get
//             {
//                 return this.FullECharge > 0 ? this.FullECharge : this.MaxECharge;
//             }
//         }
//     }
//     public class StaticChargeBatteryUI : UpdatableAndDeletable, IDrawable
//     {
//         public StaticChargeBatteryUI(StaticChargeManager staticChargeManager)
//         {
//             this.SCM = staticChargeManager;
//         }
        
//         //-------------- Local Functions
//         // Sprites
//         private void SetBatteryBGSprite(RoomCamera.SpriteLeaser sLeaser)
//         {
//             if (this.SCM != null)
//             {
//                 TriangleMesh BatteryBG = (TriangleMesh)sLeaser.sprites[1];
//                 if (this.SCM.overchargeImmunity > 0)
//                 {
//                     float blue = 0.2f + 0.2f * Mathf.Sin((this.SCM.overchargeImmunity%(Mathf.PI * 100)) * 2 * Mathf.PI / (BTWFunc.FrameRate * 2f));
//                     BatteryBG.color = new Color(0.5f - blue, 1f, 1f);
//                 }
//                 else
//                 {
//                     BatteryBG.color = new Color(1f, 1f, 1f);
//                 }
//                 sLeaser.sprites[1] = BatteryBG;
//             }
//         }
//         private void SetBatteryChargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
//         {
//             float xpos = 13f * Mathf.Clamp01(pourcent) - 7f;
//             TriangleMesh BatteryCharge = (TriangleMesh)sLeaser.sprites[2];
//             BatteryCharge.MoveVertice(2, new Vector2(xpos, -4f));
//             BatteryCharge.MoveVertice(3, new Vector2(xpos, 4f));

//             BatteryCharge.color = new Color(1f, 1f, 0.25f);
//             BatteryCharge.alpha = Mathf.Clamp01(pourcent * 10);
//             sLeaser.sprites[2] = BatteryCharge;
//         }
//         private void SetBatteryOverchargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
//         {
//             pourcent = Mathf.Clamp01(pourcent);
//             TriangleMesh BatteryOvercharge = (TriangleMesh)sLeaser.sprites[3];

            
//             if (pourcent == 0f)
//             {
//                 BatteryOvercharge.alpha = 0f;
//             }
//             else
//             {
//                 float xpos = 18f * pourcent - 9.5f;
//                 BatteryOvercharge.MoveVertice(2, new Vector2(xpos, -5.5f));
//                 BatteryOvercharge.MoveVertice(3, new Vector2(xpos, 5.5f));

//                 BatteryOvercharge.alpha = Mathf.Clamp01(pourcent * 5);
                
//                 if (this.SCM != null && this.SCM.endlessCharge > 0)
//                 {
//                     float blue = 0.25f + 0.25f * Mathf.Sin((this.SCM.overchargeImmunity%(Mathf.PI * 100)) * 2 * Mathf.PI / (BTWFunc.FrameRate * 1f));
//                     BatteryOvercharge.color = new Color(1f - blue, 1f - blue, 1f);
//                 }
//                 else
//                 {
//                     BatteryOvercharge.color = new Color(1f, 0.25f + 0.75f * (1 - pourcent), 0.25f);
//                 }
//             }
//             sLeaser.sprites[3] = BatteryOvercharge;
//         }
//         private void SetBatteryRechargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
//         {
//             pourcent = Mathf.Clamp01(pourcent);
//             float xpos = 18f * pourcent - 9f;
//             TriangleMesh BatteryRecharge = (TriangleMesh)sLeaser.sprites[4];
//             BatteryRecharge.MoveVertice(2, new Vector2(xpos, -9f));
//             BatteryRecharge.MoveVertice(3, new Vector2(xpos, -7f));

//             BatteryRecharge.color = new Color(1f, 0.5f + 0.5f * Mathf.Clamp01(2f - pourcent * 2f), 0.25f + 0.75f * Mathf.Clamp01(1f - pourcent * 2f));
//             sLeaser.sprites[4] = BatteryRecharge;
//         }

//         //-------------- Override Functions

//         // Battery Drawing
//         public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
//         {
//             foreach (FSprite sprite in sLeaser.sprites)
//             {
//                 rCam.ReturnFContainer("HUD").AddChild(sprite);
//             }
//         }
//         public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
//         public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
//         {
//             if (this.SCM == null || this.SCM.Player == null || this.slatedForDeletetion || this.room != rCam.room)
//             {
//                 sLeaser.CleanSpritesAndRemove();
//                 this.SCM.staticChargeBatteryUI = null;
//                 this.Destroy();
//                 return;
//             }
//             if (this.SCM.init && this.SCM.active && !this.SCM.Player.inShortcut)
//             {
//                 Vector2 pos = this.SpriteHeadPos == Vector2.negativeInfinity ?
//                     this.SCM.Player.firstChunk.pos + new Vector2(0f, 40f)
//                     : this.SpriteHeadPos + new Vector2(0f, 20f);
//                 float OverChargeFactor = this.SCM.endlessCharge > 0 ? 1 :
//                     this.SCM.IsOvercharged && this.SCM.MaxECharge - this.SCM.FullECharge != 0 ? 
//                     (this.SCM.Charge - this.SCM.FullECharge) / (this.SCM.MaxECharge - this.SCM.FullECharge) : 0f;
//                 Vector2 shakeFactor = this.SCM.IsOvercharged && this.SCM.endlessCharge <= 0 ? 
//                     new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) 
//                     * 4f * Mathf.Pow(OverChargeFactor, 4f) 
//                         : Vector2.zero;

//                 foreach (FSprite sprite in sLeaser.sprites)
//                 {
//                     sprite.x = pos.x + shakeFactor.x;
//                     sprite.y = pos.y + shakeFactor.y;
//                     sprite.alpha = 1f;
//                 }

//                 SetBatteryBGSprite(sLeaser);
//                 SetBatteryChargeSprite(sLeaser, this.SCM.FullECharge > 0 ? this.SCM.Charge / this.SCM.FullECharge : 0);
//                 SetBatteryRechargeSprite(sLeaser, Mathf.Sqrt(this.SCM.ChargePerSecond / (this.SCM.FullECharge / 2)));
//                 SetBatteryOverchargeSprite(sLeaser, this.SCM.MaxECharge > 0 || this.SCM.endlessCharge > 0 ? OverChargeFactor : 0);
//             }
//             else
//             {
//                 foreach (FSprite sprite in sLeaser.sprites)
//                 {
//                     sprite.alpha = 0f;
//                 }
//             }

//         }
//         public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
//         {
//             sLeaser.sprites = new FSprite[5];

//             TriangleMesh BatteryOutline = new(
//                 "Futile_White",
//                 new TriangleMesh.Triangle[] {
//                     new(0, 1, 2), new(0, 2, 3),
//                     new(4, 5, 6), new(4, 6, 7),
//                     new(8, 9, 10), new(8, 10, 11),
//                     new(3, 12, 13), new(3, 4, 13),
//                     new(2, 14, 15), new(2, 5, 15),
//                 },
//                 true, false
//             );
//             BatteryOutline.MoveVertice(0, new Vector2(-9.5f, 6.5f));
//             BatteryOutline.MoveVertice(1, new Vector2(-9.5f, -6.5f));
//             BatteryOutline.MoveVertice(2, new Vector2(-9f, -6.5f));
//             BatteryOutline.MoveVertice(3, new Vector2(-9f, 6.5f));

//             BatteryOutline.MoveVertice(12, new Vector2(-9f, 6f));
//             BatteryOutline.MoveVertice(14, new Vector2(-9f, -6f));

//             BatteryOutline.MoveVertice(4, new Vector2(8f, 6.5f));
//             BatteryOutline.MoveVertice(5, new Vector2(8f, -6.5f));
//             BatteryOutline.MoveVertice(6, new Vector2(8.5f, -6.5f));
//             BatteryOutline.MoveVertice(7, new Vector2(8.5f, 6.5f));

//             BatteryOutline.MoveVertice(13, new Vector2(8f, 6f));
//             BatteryOutline.MoveVertice(15, new Vector2(8f, -6f));

//             BatteryOutline.MoveVertice(8, new Vector2(8.5f, 3.5f));
//             BatteryOutline.MoveVertice(9, new Vector2(8.5f, -3.5f));
//             BatteryOutline.MoveVertice(10, new Vector2(9.5f, -3.5f));
//             BatteryOutline.MoveVertice(11, new Vector2(9.5f, 3.5f));

//             BatteryOutline.color = new Color(0.1f, 0.1f, 0.1f);
//             sLeaser.sprites[0] = BatteryOutline;


//             TriangleMesh BatteryBG = new(
//                 "Futile_White",
//                 new TriangleMesh.Triangle[] {
//                     new(0, 1, 2), new(0, 2, 3),
//                     new(4, 5, 6), new(4, 6, 7),
//                     new(8, 9, 10), new(8, 10, 11),
//                     new(3, 12, 13), new(3, 4, 13),
//                     new(2, 14, 15), new(2, 5, 15),
//                 },
//                 true, false
//             );
//             BatteryBG.MoveVertice(0, new Vector2(-9f, 6f));
//             BatteryBG.MoveVertice(1, new Vector2(-9f, -6f));
//             BatteryBG.MoveVertice(2, new Vector2(-8f, -6f));
//             BatteryBG.MoveVertice(3, new Vector2(-8f, 6f));

//             BatteryBG.MoveVertice(12, new Vector2(-8f, 5f));
//             BatteryBG.MoveVertice(14, new Vector2(-8f, -5f));

//             BatteryBG.MoveVertice(4, new Vector2(7f, 6f));
//             BatteryBG.MoveVertice(5, new Vector2(7f, -6f));
//             BatteryBG.MoveVertice(6, new Vector2(8f, -6f));
//             BatteryBG.MoveVertice(7, new Vector2(8f, 6f));

//             BatteryBG.MoveVertice(13, new Vector2(7f, 5f));
//             BatteryBG.MoveVertice(15, new Vector2(7f, -5f));

//             BatteryBG.MoveVertice(8, new Vector2(8f, 3f));
//             BatteryBG.MoveVertice(9, new Vector2(8f, -3f));
//             BatteryBG.MoveVertice(10, new Vector2(9f, -3f));
//             BatteryBG.MoveVertice(11, new Vector2(9f, 3f));

//             BatteryBG.color = new Color(1f, 1f, 1f);
//             sLeaser.sprites[1] = BatteryBG;


//             TriangleMesh BatteryCharge = new(
//                 "Futile_White",
//                 new TriangleMesh.Triangle[] {
//                     new(0, 1, 2), new(0, 2, 3)
//                 },
//                 true, false
//             );
//             BatteryCharge.MoveVertice(0, new Vector2(-6f, 4f));
//             BatteryCharge.MoveVertice(1, new Vector2(-6f, -4f));
//             BatteryCharge.MoveVertice(2, new Vector2(5f, -4f));
//             BatteryCharge.MoveVertice(3, new Vector2(5f, 4f));

//             BatteryCharge.color = new Color(1f, 1f, 0.25f);
//             sLeaser.sprites[2] = BatteryCharge;


//             TriangleMesh BatteryOvercharge = new(
//                 "Futile_White",
//                 new TriangleMesh.Triangle[] {
//                     new(0, 1, 2), new(0, 2, 3)
//                 },
//                 true, false
//             );
//             BatteryOvercharge.MoveVertice(0, new Vector2(-8.5f, 5.5f));
//             BatteryOvercharge.MoveVertice(1, new Vector2(-8.5f, -5.5f));
//             BatteryOvercharge.MoveVertice(2, new Vector2(7.5f, -5.5f));
//             BatteryOvercharge.MoveVertice(3, new Vector2(7.5f, 5.5f));

//             BatteryOvercharge.color = new Color(1f, 1f, 0.25f);
//             sLeaser.sprites[3] = BatteryOvercharge;


//             TriangleMesh BatteryRecharge = new(
//                 "Futile_White",
//                 new TriangleMesh.Triangle[] {
//                     new(0, 1, 2), new(0, 2, 3)
//                 },
//                 true, false
//             );
//             BatteryRecharge.MoveVertice(0, new Vector2(-9f, -7f));
//             BatteryRecharge.MoveVertice(1, new Vector2(-9f, -9f));
//             BatteryRecharge.MoveVertice(2, new Vector2(9f, -9f));
//             BatteryRecharge.MoveVertice(3, new Vector2(9f, -7f));

//             BatteryRecharge.color = new Color(1f, 0.75f, 0.25f);
//             sLeaser.sprites[4] = BatteryRecharge;

//             this.AddToContainer(sLeaser, rCam, null);
//         }

//         //-------------- Variables
//         public StaticChargeManager SCM;
//         public Vector2 SpriteHeadPos
//         {
//             get
//             {
//                 if (this.SCM != null && this.SCM.Player != null && BTWSkins.cwtPlayerSpriteInfo.TryGetValue(this.SCM.AbstractPlayer, out var psl))
//                 {
//                     return psl[3].GetPosition();
//                 }
//                 return Vector2.negativeInfinity;
//             }
//         }
//     }
//     public class ElectricExplosion : UpdatableAndDeletable
//     {
//         public ElectricExplosion(
//             Room room, PhysicalObject sourceObject, Vector2 pos, 
//             int lifeTime, float rad, float force, float maxdamage, float maxstun, 
//             Creature killTagHolder, float killTagHolderDmgFactor, float backgroundNoise, 
//             bool underwater = false, bool doSpams = false, bool notifyMeadow = false)
//         {
//             Plugin.Log("Alright, ElectricExplosion created :\n"
//                 +"  room : ["+ room +"]\n"
//                 +"  sourceObject : ["+ sourceObject +"]\n"
//                 +"  pos : ["+ pos +"]\n"
//                 +"  lifeTime : ["+ lifeTime +"]\n"
//                 +"  rad : ["+ rad +"]\n"
//                 +"  force : ["+ force +"]\n"
//                 +"  maxdamage : ["+ maxdamage +"]\n"
//                 +"  maxstun : ["+ maxstun +"]\n"
//                 +"  killTagHolder : ["+ killTagHolder +"]\n"
//                 +"  killTagHolderDmgFactor : ["+ killTagHolderDmgFactor +"]\n"
//                 +"  backgroundNoise : ["+ backgroundNoise +"]\n"
//                 +"  underwater : ["+ underwater +"]\n"
//                 +"  doSpams : ["+ doSpams +"]\n"
//                 +"  notifyMeadow : ["+ notifyMeadow+"]");
//             this.room = room;
//             this.sourceObject = sourceObject;
//             this.pos = pos;
//             this.lifeTime = lifeTime;
//             this.rad = rad;
//             this.force = force;
//             this.underwater = underwater;
//             this.maxDamage = maxdamage;
//             this.maxStun = maxstun;
//             this.killTagHolder = killTagHolder;
//             this.killTagHolderDmgFactor = killTagHolderDmgFactor;
//             this.backgroundNoise = backgroundNoise;
//             this.doSpams = doSpams;
//             this.notifyMeadow = notifyMeadow;

//             if (Plugin.meadowEnabled && notifyMeadow)
//             {
//                 MeadowCompat.SparMeadow_ElectricExplosionRPC(this);
//             }
//             if (killTagHolder != null)
//             {
//                 if (killTagHolderDmgFactor <= 0)
//                 {
//                     this.objectsHit.Add(killTagHolder);
//                 }
//                 if (killTagHolder.grasps != null)
//                 {
//                     foreach (Creature.Grasp grasp in this.killTagHolder.grasps)
//                     {
//                         if (grasp != null && grasp.grabbed != null)
//                         {
//                             this.passThroughObjects.Add(grasp.grabbed);
//                         }
//                     }
//                 }
//             }
//         }

//         //-------------- Functions
        
//         public override void Update(bool eu)
//         {
//             base.Update(eu);
//             if (!this.chainReactionNotified && this.room != null)
//             {
//                 this.chainReactionNotified = true;
//                 for (int i = 0; i < this.room.updateList.Count; i++)
//                 {
//                     if (this.room.updateList[i] is IReactToElectricExplosion)
//                     {
//                         (this.room.updateList[i] as IReactToElectricExplosion).Explosion(this);
//                     }
//                 }
//                 if (this.sourceObject != null)
//                 {
//                     this.room.InGameNoise(new InGameNoise(this.pos, this.backgroundNoise * 900f, this.sourceObject, this.backgroundNoise * 4f));
//                 }
//                 byte sparks = (byte)(UnityEngine.Random.Range(5f, 10f) * (1 + this.maxDamage));
//                 MakeSparkExplosion(this.room, this.rad, this.pos, sparks, this.underwater, this.color);
//                 if (this.maxDamage > 1f) { room.ScreenMovement(this.pos, default, this.maxDamage / 10f); }

//                 room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, this.pos, 0.5f + Math.Min(1f, this.volume), BTWFunc.Random(1.1f, 1.5f));
//                 room.PlaySound(SoundID.Bomb_Explode, this.pos, this.volume / 2f, BTWFunc.Random(1.75f, 2.25f));
//             }
//             this.room.MakeBackgroundNoise(this.backgroundNoise);

//             if (this.frame < this.lifeTime 
//                 && this.rad > 0 
//                 && (this.maxDamage > 0 || this.maxStun > 0)
//                 && this.room != null)
//             {
//                 for (int j = 0; j < this.room.physicalObjects.Length; j++)
//                 {
//                     for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
//                     {
//                         PhysicalObject obj = this.room.physicalObjects[j][k];

//                         if ((!this.objectsHit.Exists(x => x == obj))
//                             && (!this.onlyHitCreatures || obj is Creature)
//                             && (!this.passThroughObjects.Exists(x => x == obj))
//                             && (this.sourceObject == null || BTWFunc.CanTwoObjectsInteract(this.sourceObject, obj))
//                             && BTWFunc.IsObjectInRadius(obj, this.pos, this.rad, out var result)
//                             && !(Plugin.meadowEnabled && MeadowCompat.IsMeadowArena() && this.killTagHolder != null && this.killTagHolder is Player pl && pl != null && obj is Creature cr && MeadowAlly(pl, cr)))
//                         {
//                             bool cthroughWall = !room.VisualContact(this.pos, this.pos + result.distance * result.vectorDistance);
//                             bool submerged = BTWFunc.BodyChunkSumberged(result.closestBodyChunk);
//                             if (cthroughWall && !this.hitThroughtWalls) { continue; }
//                             if ((submerged && !this.hitSubmerged) || (!submerged && !this.hitNonSubmerged)) { continue; }

//                             Vector2 knockbackDir = this.forcedKnockbackDirection == Vector2.zero ? result.vectorDistance : this.forcedKnockbackDirection;
//                             float ratioDist = Mathf.Clamp01(1 - (result.distance / this.rad));
//                             float dmg = this.maxDamage * (
//                                 this.baseDamageFraction + (1 - this.baseDamageFraction) * Mathf.Pow(ratioDist, 2));
//                             float force = this.force;
//                             int stun = (int)(this.maxStun * (
//                                 this.baseStunFraction + (1 - this.baseStunFraction) * Mathf.Pow(ratioDist, 2)));

//                             if (cthroughWall) { dmg *= 0.35f; stun = (int)(stun * 0.25f); }
//                             if (this.sparedObjects.Exists(x => x == obj)) { dmg = 0f; stun *= 2; force *= 0.1f; }
//                             if (submerged) { stun += 1; force *= 0.25f; }

//                             bool targetLocal = !Plugin.meadowEnabled || MeadowCompat.IsMine(obj.abstractPhysicalObject);
//                             if (targetLocal)
//                             {
//                                 foreach (BodyChunk b in result.bodyChunksHit)
//                                 {
//                                     BTWFunc.CustomKnockback(b, knockbackDir, force, notifyMeadow);
//                                 }
//                             }

//                             if (ModManager.MSC)
//                             {
//                                 if (targetLocal && dmg > 1.5f && obj is Player player && player != null)
//                                 {
//                                     MoreSlugcatCompat.StaticManager_CheckIfArtififerShouldExplode(player);
//                                 }
//                             }
//                             if (obj is Creature creature)
//                             {
//                                 Plugin.Log("Ouch ! Creature ["+ creature +"] got shocked by ["+ this.killTagHolder 
//                                     +"] using ["+ this.sourceObject +"] !");
//                                 Plugin.Log("Took <"+ dmg +"/"+ this.maxDamage +"> damage and <"
//                                     + stun +"/"+ this.maxStun +"> stun (reach ratio is <"+ ratioDist +">).");
                                
//                                 ShockCreature(creature, result.closestBodyChunk, this.sourceObject, this.killTagHolder,
//                                     this.killTagHolderDmgFactor, dmg, stun, this.color, this.doSpams, this.notifyMeadow,
//                                     this.hitPlayer, this.sparedObjects);
//                             }
//                             this.objectsHit.Add(obj);
//                         }
//                     }
//                 }
//             }
//             this.frame++;
            
//             if (this.frame >= this.lifeTime)
//             {
//                 this.Destroy();
//             }
//         }

//         //------------- Variables
//         // Objects
//         public PhysicalObject sourceObject;
//         public Creature killTagHolder;
//         public Vector2 pos;
//         public List<PhysicalObject> sparedObjects = new();
//         public List<PhysicalObject> passThroughObjects = new();
//         public List<PhysicalObject> objectsHit = new();
//         public Vector2 forcedKnockbackDirection = Vector2.zero;
//         public Color color = Color.white;

//         // Basic
//         public bool doSpams = false;
//         public bool notifyMeadow = false;
//         public bool chainReactionNotified = false;
//         public bool hitSubmerged = true;
//         public bool hitNonSubmerged = true;
//         public bool hitThroughtWalls = true;
//         public bool onlyHitCreatures = false;
//         public bool hitPlayer = true;
//         public bool underwater = false;

//         public int lifeTime;
//         public int frame = 0;
//         public float rad;
//         public float force;
//         public float maxDamage;
//         public float maxStun;
//         public float killTagHolderDmgFactor;
//         public float backgroundNoise;
//         public float baseDamageFraction = 0.5f;
//         public float baseStunFraction = 0.25f;
//         public float volume = 0.5f;
//         public interface IReactToElectricExplosion // eh may be useful for later
//         {
//             void Explosion(ElectricExplosion electricExplosion);
//         }
//     }
//     // Functions
//     public static void ApplyHooks()
//     {
//         IL.Player.ThrowObject += Player_StaticManager_SlideSpearBounce;
//         On.Player.SpitOutOfShortCut += Player_MoveSparkUI;
//         On.Player.Jump += Player_StaticManager_SlideMomentum;
//         On.Creature.SuckedIntoShortCut += Player_SparkRechargeNull;
//         On.Creature.Violence += Player_Electric_Absorb;
//         On.Creature.Die += Player_StaticManager_ConsideredDead;
//         IL.Centipede.Shock += Player_CentipedeShock_Absorb;
//         IL.ZapCoil.Update += ZapCoil_StaticChargeManager_Absorb;
//         Plugin.Log("SparkObject ApplyHooks Done !");
//     }

//     public static void MakeSparkExplosion(Room room, float size, Vector2 position, byte sparks, bool underwater, Color color)
//     {
//         if (underwater) { room.AddObject(new UnderwaterShock.Flash(position, size, 1f, 30, color)); }

//         room.AddObject(new Explosion.ExplosionLight(position, size, 1f, 5, color));
//         room.AddObject(new ShockWave(position, size, 0.001f, 50, false));
//         for (int i = sparks; i >= 0; i--)
//         {
//             room.AddObject(new MouseSpark(position, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)), 25f, color));
//         }
//     }
//     public static void ShockCreature(Creature target, BodyChunk closestBodyChunk, PhysicalObject sourceObject, Creature killTagHolder, float killTagHolderDmgFactor, float damage, float stun, Color color, bool doSpams = false, bool notifyMeadow = false, bool hitPlayer = true, List<PhysicalObject> sparedObjects = null)
//     {
//         if (target?.room == null) { return; }
//         if (color == null) { color = Color.white; }
//         if (sparedObjects == null) { sparedObjects = new(); }

//         bool isMine = true;
//         if (Plugin.meadowEnabled && !MeadowCompat.IsMine(target.abstractCreature)) { isMine = false; }
        
//         if (killTagHolder != null) { 
//             target.SetKillTag(killTagHolder.abstractCreature); 

//             if (target == killTagHolder)
//             {
//                 damage *= killTagHolderDmgFactor;
//             }

//             if (ModManager.MSC && closestBodyChunk != null)
//             {
//                 BodyChunk mainChunk = killTagHolder.mainBodyChunk ?? killTagHolder.firstChunk;
//                 MoreSlugcatCompat.LightingArc lightingArc = new MoreSlugcatCompat.LightingArc(
//                     mainChunk, closestBodyChunk,
//                     0.5f + damage, damage > 1 ? 1f : 0.5f, (int)(stun / 10f + 5), color
//                 );
//                 target.room.AddObject(lightingArc);
//             }
//         }

//         if ((!hitPlayer && target is Player) || sparedObjects.Exists(x => x == target)) { damage = 0; }
        
//         if (isMine)
//         {
//             target.Violence(sourceObject?.firstChunk, null, closestBodyChunk, null, Creature.DamageType.Electric, damage, stun);
            
//             if (doSpams)
//             {
//                 target.room.AddObject(new CreatureSpasmer(target, false, target.stun));
//             }
//             if (damage > 1f)
//             {
//                 target.LoseAllGrasps();
//             }
//         }

//         if (Plugin.meadowEnabled && notifyMeadow && !MeadowCompat.IsMine(target.abstractCreature))
//         {
//             MeadowCompat.SparMeadow_ShockCreatureRPC(target, closestBodyChunk, sourceObject, 
//                 killTagHolder, killTagHolderDmgFactor, damage, stun, color, doSpams);
//         }
//     }

//     private static bool OnlineTimerOn()
//     {
//         if (Plugin.meadowEnabled)
//         {
//             return MeadowCompat.ShouldHoldFireFromOnlineArenaTimer();
//         }
//         return false;
//     }
//     private static bool MeadowAlly(Player pl, Creature creature)
//     {
//         if (Plugin.meadowEnabled && pl != null && creature != null)
//         {
//             return MeadowCompat.IsCreatureFriendlies(pl, creature);
//         }
//         return false;
//     }

//     // Hook
//     private static void Player_StaticManager_ConsideredDead(On.Creature.orig_Die orig, Creature self)
//     {
//         orig(self);
//         if (SparkFunc.cwtSpark.TryGetValue(self.abstractCreature, out var staticChargeManager))
//         {
//             staticChargeManager.consideredAlive = false;
//         }
//     }

//     private static void Player_SparkRechargeNull(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
//     {
//         orig(self, entrancePos,carriedByOther);
//         if (SparkFunc.cwtSpark.TryGetValue(self.abstractCreature, out var staticChargeManager))
//         {
//             staticChargeManager.newpos = Vector2.negativeInfinity;
//             staticChargeManager.oldpos = Vector2.negativeInfinity;
//         }
//     }
//     private static void Player_StaticManager_SlideSpearBounce(ILContext il)
//     {
//         Plugin.Log("StaticChargeManager IL 1 starts");
//         try
//         {
//             Plugin.Log("Trying to hook IL");
//             ILCursor cursor = new(il);
//             if (cursor.TryGotoNext(MoveType.After,
//                 x => x.MatchLdloc(0),
//                 x => x.MatchLdfld<IntVector2>(nameof(IntVector2.x)),
//                 x => x.MatchLdarg(0),
//                 x => x.MatchLdfld<Player>(nameof(Player.rollDirection)),
//                 x => x.MatchBneUn(out _),
//                 x => x.MatchLdarg(0),
//                 x => x.MatchCall(out _),
//                 x => x.MatchLdfld<SlugcatStats>(nameof(SlugcatStats.throwingSkill))
//             ))
//             {
//                 static int IsAbleToSpearBounce(int orig, Player player)
//                 {
//                     if (SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
//                     {
//                         return (SCM.slideSpearBounceFrames > 0 || SCM.slideSpeedframes > SCM.SlideAccelerationFrames / 2 || SCM.slideSpeedMult > 1.5f) ? 1 : 0;
//                     }
//                     return orig;
//                 }
//                 cursor.Emit(OpCodes.Ldarg_0);
//                 cursor.EmitDelegate(IsAbleToSpearBounce);
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
//         Plugin.Log("StaticChargeManager IL 1 ends");
//     }
//     private static void Player_MoveSparkUI(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
//     {
//         orig(self, pos, newRoom, spitOutAllSticks);
//         if (SparkFunc.cwtSpark.TryGetValue(self.abstractCreature, out var staticChargeManager))
//         {
//             staticChargeManager.newpos = Vector2.zero;
//             staticChargeManager.oldpos = Vector2.zero;
//             // if (staticChargeManager.staticChargeBatteryUI != null)
// 			// {
// 			// 	// now what ??
// 			// }
//         }
//     }
//     private static void Player_StaticManager_SlideMomentum(On.Player.orig_Jump orig, Player self)
//     {
//         Player.AnimationIndex oldanim = self.animation;
//         orig(self);
//         if (SparkFunc.cwtSpark.TryGetValue(self.abstractCreature, out var staticChargeManager) 
//             && oldanim == Player.AnimationIndex.BellySlide 
//             && self.animation == Player.AnimationIndex.RocketJump)
//         {
//             foreach (BodyChunk bodyChunk in self.bodyChunks)
//             {
//                 bodyChunk.vel.x = Mathf.Sign(bodyChunk.vel.x) * Mathf.Max(
//                     Mathf.Abs(bodyChunk.vel.x), 
//                     staticChargeManager.CurrentSlideMomentum * 0.85f);
//             }
//         }
//     }
    
//     private static void Player_CentipedeShock_Absorb(ILContext il) // this is the first IL hook I made myself :D
//     {
//         Plugin.Log("StaticChargeManager IL 2 starts");
//         try
//         {
//             Plugin.Log("Trying to hook IL");
//             ILCursor cursor = new(il);
//             if (cursor.TryGotoNext(MoveType.Before, x => x.MatchCall<Centipede>("get_Small")))
//             {
//                 static bool CanPlayerShockAbsorb(Creature creature) => creature is Player player && player != null && SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out _);

//                 static void CheckPlayerAbsorb(Centipede self, Creature creature)
//                 {
//                     if (CanPlayerShockAbsorb(creature) && SparkFunc.cwtSpark.TryGetValue(creature.abstractCreature, out var SCM))
//                     {
//                         Player pl = creature as Player;
//                         pl.SetKillTag(self.abstractCreature);

//                         float diff = 0.45f;
//                         float AddedCharge = 0f;
//                         self.shockCharge = 0f;
//                         if (self.Small)
//                         {
//                             AddedCharge = (SCM.FullECharge - SCM.Charge) * diff;
//                         }
//                         else
//                         {
//                             AddedCharge = ((SCM.FullECharge / 2) * (self.TotalMass / pl.TotalMass) - SCM.Charge) * diff;
//                         }
                        
//                         AddedCharge = Math.Max(0, AddedCharge);
//                         SCM.RechargeFromExternalSource(self.mainBodyChunk ?? self.firstChunk, AddedCharge);

//                         self.Stun(6);
//                         self.shockGiveUpCounter = Math.Max(self.shockGiveUpCounter, 30);
//                         self.AI.annoyingCollisions = Math.Min(self.AI.annoyingCollisions / 2, 150);

//                         Plugin.Log("SHOCKING ! " + AddedCharge);
//                     }
//                     else
//                     {
//                         Plugin.Log("Can't shock :<");
//                     }
//                 }

//                 // Logger.LogDebug("Moving cursor");
//                 cursor.GotoPrev(MoveType.Before, x => x.MatchLdarg(0)); 
//                 var mark1 = cursor.Previous; 

//                 cursor.Emit(OpCodes.Ldarg_0); // add shock resist
//                 cursor.Emit(OpCodes.Ldarg_1);
//                 cursor.EmitDelegate(CheckPlayerAbsorb);
//                 var mark2 = cursor.Previous; 
                
//                 // Logger.LogDebug("Get label 1");
//                 ILLabel label1 = cursor.DefineLabel(); // set label to ignore if not met
//                 cursor.MarkLabel(label1);

//                 // Logger.LogDebug("Get label 2");
//                 ILLabel label2 = cursor.DefineLabel(); // set label to ignore if met
//                 cursor.GotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<PhysicalObject>("get_Submersion"));
//                 cursor.GotoPrev(MoveType.Before, x => x.MatchLdarg(1)); 
//                 cursor.MarkLabel(label2);

//                 // Logger.LogDebug("Moving to mark 1");
//                 cursor.Goto(0, MoveType.After); //return to start
//                 cursor.GotoNext(MoveType.After, x => x == mark1); //add condition to ignore if not met
//                 cursor.Emit(OpCodes.Ldarg_1); 
//                 cursor.EmitDelegate(CanPlayerShockAbsorb);
//                 cursor.Emit(OpCodes.Brfalse, label1);

//                 // Logger.LogDebug("Moving to mark 2");
//                 cursor.Goto(0, MoveType.After); //return to start
//                 cursor.GotoNext(MoveType.After, x => x == mark2); //add ignore if met
//                 cursor.Emit(OpCodes.Br, label2);


//                 // Logger.LogDebug("Cursor moved !");
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
//         Plugin.Log("StaticChargeManager IL 2 ends");
//     }
//     private static void Player_Electric_Absorb(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
//     {
//         if (self != null && self is Player player && player != null && type == Creature.DamageType.Electric && SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
//         {
//             SCM.RechargeFromExternalSource(source, 40f * damage);
//             orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, 0f, stunBonus / 4);
//         }
//         else
//         {
//             orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
//         }
//     }
    
//     private static Creature CheckIfSparkIsZapped(Creature creature, int indexBodyPart)
//     {
//         // Plugin.Log("Checking creature to zap with Zap Coil : ["+ creature +"]");
//         if (creature is Player player && SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
//         {
//             BodyChunk bodyChunk = player.bodyChunks[indexBodyPart] ?? player.mainBodyChunk ?? player.firstChunk;
//             Vector2 a = bodyChunk.ContactPoint.ToVector2();
//             Vector2 v = bodyChunk.pos + a * (bodyChunk.rad + 30f);
//             SCM.RechargeFromExternalSource(v, 1500f);
//             Plugin.Log("Spark got zapped !");
//             return null;
//         }
//         return creature;
//     }
//     private static void ZapCoil_StaticChargeManager_Absorb(ILContext il)
//     {
//         Plugin.Log("StaticChargeManager IL 3 starts");
//         try
//         {
//             Plugin.Log("Trying to hook IL");
//             ILCursor cursor = new(il);

//             Func<Instruction, bool>[] iltarget = {
//                 x => x.MatchLdarg(0),
//                 x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
//                 x => x.MatchLdfld<Room>(nameof(Room.physicalObjects)),
//                 x => x.MatchLdloc(1),
//                 x => x.MatchLdelemRef(),
//                 x => x.MatchLdloc(2),
//                 x => x.MatchCallvirt(out _),
//                 x => x.MatchIsinst(typeof(Creature))};

//             Func<Instruction, bool>[] iltarget2 = {
//                 x => x.MatchLdarg(0),
//                 x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
//                 x => x.MatchLdfld<Room>(nameof(Room.physicalObjects)),
//                 x => x.MatchLdloc(1),
//                 x => x.MatchLdelemRef(),
//                 x => x.MatchLdloc(2),
//                 x => x.MatchCallvirt(out _),
//                 x => x.MatchIsinst(typeof(Creature)),
//                 x => x.MatchCallOrCallvirt<Creature>(nameof(Creature.Die))};

//             if (cursor.TryGotoNext(MoveType.Before, iltarget2))
//             {
//                 if (cursor.TryGotoPrev(MoveType.After, iltarget))
//                 {
//                     cursor.Emit(OpCodes.Ldloc_3);
//                     cursor.EmitDelegate(CheckIfSparkIsZapped);
//                 }
//                 else
//                 {
//                     Plugin.logger.LogError("Couldn't find IL hook :<");
//                     Plugin.Log(il);
//                 }
//             }
//             else
//             {
//                 Plugin.logger.LogError("Couldn't find IL hook main :<");
//                 Plugin.Log(il);
//             }
            
//             Plugin.Log("IL hook ended");
//         }
//         catch (Exception ex)
//         {
//             Plugin.logger.LogError(ex);
//         }
//         Plugin.Log("StaticChargeManager IL 3 ends");
//     }

// }