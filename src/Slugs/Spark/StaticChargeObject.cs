using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SlugBase.Features;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using BepInEx;
using BepInEx.Logging;
using System;
using MonoMod.Cil;
using RWCustom;
using Mono.Cecil.Cil;
using System.Runtime.InteropServices.WindowsRuntime;
using BeyondTheWest;

public class SparkObject
{
    // Variables

    // Objects
    public class StaticChargeManager
    {
        public StaticChargeManager(AbstractCreature abstractpl)
        {
            this.AbstractPlayer = abstractpl;
            if (ModManager.MSC)
            {
                MoreSlugcatCompat.StaticManager_AddSCMToLigningArcCWT(this);
            }
            InitPlayerStaticCharge();
        }

        //-------------- Local Functions

        // Init
        private void InitPlayerStaticCharge()
        {
            if (this.AbstractPlayer != null && this.Player != null && this.Room != null)
            {
                this.init = true;
                if (SparkFunc.IsSpark(this.Player))
                {
                    this.particles = true;
                    this.active = true;
                    this.isMeadow = false;
                    this.isMeadowFakePlayer = false;
                    this.Charge = 0f;
                    this.dischargeCooldown = 1;
                    this.DoDischargeDamagePlayers = BTWRemix.DoSparkShockSlugs.Value || this.Room.game.IsArenaSession;

                    if (BTWRemix.DoDisplaySparkBattery.Value && this.active)
                    {
                        this.displayBattery = true;
                    }
                    if (Plugin.meadowEnabled)
                    {
                        MeadowCompat.SparkMeadow_Init(this);
                    }
                }
                else
                {
                    this.active = false;
                }
                Plugin.Log("Spark manager Init ! " + this.init + "/" + this.particles + "/" + this.active + "/" + this.isMeadow + "/" + this.isMeadowFakePlayer + "/" + this.displayBattery + "/" + this.dischargeCooldown);
            }
        }

        // Updates
        private void ParticlesUpdate()
        {
            float FractCharge = this.Charge / this.FullECharge;
            Player player = this.Player;
            Room room = this.Room;
            Color color = player.ShortCutColor();
            Vector2 pos = player.mainBodyChunk.pos;

            if (UnityEngine.Random.Range(0f, 1f) * 5f < FractCharge)
            {
                room.AddObject(new MouseSpark(pos, new Vector2(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f)), 15f, color));
            }
            if (IsOvercharged)
            {
                float FractOvercharge = (this.Charge - this.FullECharge) / (this.MaxECharge - this.FullECharge);
                if (UnityEngine.Random.Range(0f, 1f) < Mathf.Pow(FractOvercharge, 2)/2)
                {
                    room.AddObject(new Spark(pos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)) * FractOvercharge, color, null, 5, 20));
                    
                    room.PlaySound(SoundID.Centipede_Shock, player.mainBodyChunk, false, Mathf.Pow(FractOvercharge, 1f/2f) / 2f, UnityEngine.Random.Range(1.25f, 1.75f));
                    if (this.active)
                    {
                        this.Charge -= FractOvercharge * 0.25f;
                        foreach (BodyChunk bodyChunk in this.Player.bodyChunks)
                        {
                            bodyChunk.vel.x += UnityEngine.Random.Range(-1f, 1f) * FractOvercharge * 0.5f;
                            bodyChunk.vel.y += UnityEngine.Random.Range(-1f, 1f) * FractOvercharge * 0.2f;
                        }
                    }
                }
            }
        }
        private void BounceUpdate()
        {
            Player player = this.Player;
            Player.InputPackage inputs = this.Player.input[0];
            Player.InputPackage lastInputs = this.Player.input[1];
            Vector2 intInput = this.IntDirectionalInput;

            if (this.Landed)
            {
                this.eBounceLeft = this.MaxEBounce;
            }
            else if (this.eBounceLeft > 0 && this.dischargeCooldown <= 0 && player.stun <= 0)
            {
                if (
                    (player.animation == Player.AnimationIndex.Flip || this.rocketJumpFromBounceJump) &&
                    inputs.spec && !lastInputs.spec && intInput.y == -1
                )
                {
                    this.eBounceLeft--;
                    this.BounceUp(IsOvercharged);
                }
                else if (
                    player.animation == Player.AnimationIndex.RocketJump &&
                    inputs.spec && !lastInputs.spec && intInput.x * player.mainBodyChunk.vel.x < 0
                )
                {
                    this.eBounceLeft--;
                    Plugin.Log("Spark Jump Tech");
                    this.BounceBack(IsOvercharged);
                }
            }
        }
        private void DischargeUpdate()
        {
            Player player = this.Player;
            Vector2 pos = player.mainBodyChunk.pos;
            Player.InputPackage inputs = this.Player.input[0];
            Vector2 dirInput = this.DirectionalInput;
            Vector2 intInput = this.IntDirectionalInput;

            bool overcharged = this.IsOvercharged;
            if (this.rocketJumpFromBounceJump && this.Player.canJump > 0)
            {
                this.rocketJumpFromBounceJump = false;
            }
            if (inputs.spec && this.dischargeCooldown <= 0)
            {
                if (player.animation == Player.AnimationIndex.ClimbOnBeam)
                {
                    bool success = Discharge(
                        overcharged ? 125f : 75f,
                        overcharged ? 0.35f : 0.25f,
                        overcharged ? 80f : 50.0f,
                        pos
                    );
                    if (overcharged && success && !this.isMeadowFakePlayer)
                    {
                        foreach (BodyChunk b in player.bodyChunks)
                        {
                            b.vel.x += UnityEngine.Random.Range(-10f, 10f);
                            b.vel.y += UnityEngine.Random.Range(-10f, 10f);
                        }
                    }
                }
                else if (player.animation == Player.AnimationIndex.Roll || player.animation == Player.AnimationIndex.BellySlide)
                {
                    if (player.animation == Player.AnimationIndex.BellySlide && intInput.x == -player.rollDirection && intInput.y == -1)
                    {
                        SlideBoost(overcharged);
                    }
                    else
                    {
                        BounceJump(overcharged);
                        this.rocketJumpFromBounceJump = true;
                    }
                }
                else
                {
                    Vector2 lookPos = intInput != Vector2.zero ? dirInput : new Vector2(player.ThrowDirection, 0);
                    Vector2 dischargePos = pos + (player.bodyMode == Player.BodyModeIndex.WallClimb ? lookPos * -15f : lookPos * 25f);
                    bool success = Discharge(
                        overcharged ? 45f : 30f,
                        overcharged ? 1.75f : 1.15f,
                        overcharged ? 50f : 30f,
                        dischargePos
                    );

                    if (success && !this.isMeadowFakePlayer)
                    {
                        if (player.bodyMode != Player.BodyModeIndex.WallClimb)
                        {
                            foreach (BodyChunk b in player.bodyChunks)
                            {
                                b.vel.x -= UnityEngine.Random.Range(5f, 7f) * lookPos.x;
                                b.vel.y -= UnityEngine.Random.Range(6f, 8f) * lookPos.y;
                            }
                        }
                        if (overcharged)
                        {
                            player.Stun(30);
                        }
                    }
                }
            }
        }
        private void OverchargeUpdate()
        {
            float FractOvercharge = (this.Charge - this.FullECharge) / (this.MaxECharge - this.FullECharge);

            Player player = this.Player;
            Room room = this.Room;
            Vector2 pos = player.mainBodyChunk.pos;
            Color color = player.ShortCutColor();
            bool forceDischarge = false;

            if (this.Charge >= this.MaxECharge)
            {
                if (this.DeathOvercharge)
                {
                    for (int i = (int)UnityEngine.Random.Range(15f, 25f); i >= 0; i--)
                    {
                        room.AddObject(new MouseSpark(pos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)), 50f, color));
                    }

                    room.ScreenMovement(pos, default, 1.1f);
                    room.PlaySound(SoundID.Bomb_Explode, pos);
                    room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, pos);
                    room.InGameNoise(new Noise.InGameNoise(pos, 900f, player, 1f));
                    Discharge(this.FullECharge * 1.5f, 1.0f, this.MaxECharge, pos, 1.5f);
                    player.Die();
                    return;
                }
                else
                {
                    this.Charge = this.MaxECharge;
                    forceDischarge = true;
                }
            }
            if (this.RiskyOvercharge)
            {
                bool isSwimming = player.bodyMode == Player.BodyModeIndex.Swimming;
                if (forceDischarge || ((
                        player.bodyMode == Player.BodyModeIndex.Stand ||
                        player.bodyMode == Player.BodyModeIndex.Crawl ||
                        isSwimming
                    ) && UnityEngine.Random.Range(0f, 1f) < Mathf.Pow(FractOvercharge, 4) * (isSwimming ? 0.02f : 0.075f)))
                {
                    Discharge(200f, 0.5f, this.MaxECharge - this.FullECharge);
                    player.Stun((int)((this.MaxECharge - this.FullECharge) * (FractOvercharge * 3f + 1f) / (isSwimming ? 2 : 1) ));
                    room.AddObject(new CreatureSpasmer(player, false, player.stun));
                    player.LoseAllGrasps();
                }
            }
        }
        private void RechargeUpdate()
        {
            if (this.Player != null && !this.Player.dead && (this.dischargeCooldown <= 0 || this.isMeadowArenaTimerCountdown))
            {
                if (this.isMeadowArenaTimerCountdown && this.IsOvercharged)
                {
                    return;
                }
                this.Charge += this.ChargePerSecond / 40f;
            }
        }
        private void SlideUpdate()
        {
            if (this.Player != null && this.Player.rollCounter > 8 && this.Player.animation == Player.AnimationIndex.BellySlide)
            {
                bool overcharged = this.IsOvercharged;
                BodyChunk bodyChunk = this.Player.bodyChunks[0];
                Player player = this.Player;

                player.rollCounter = 12;
                bodyChunk.vel.x = Mathf.Lerp(
                    this.InitSlideSpeed,
                    this.MaxSlideSpeed,
                    BTWFunc.EaseOut((float)this.slideSpeedframes / this.SlideAccelerationFrames, 2)
                ) * this.slideSpeedMult * player.rollDirection;

                if (this.slideSpeedframes < this.SlideAccelerationFrames) { this.slideSpeedframes++; }
                if (this.slideSpearBounceFrames > 0) { this.slideSpearBounceFrames--; }
                if (this.slideSpeedMult > 1f) 
                    { this.slideSpeedMult = Mathf.Clamp(Mathf.Lerp(this.slideSpeedMult, 1f, 0.05f), 1f, 5f); }

                if (overcharged && this.active)
                {
                    float amountOvercharge = this.Charge - this.FullECharge;
                    float fractOvercharge = amountOvercharge / (this.MaxECharge - this.FullECharge);
                    const float criticAmount = 0.25f;

                    this.slideSpeedMult = Math.Max(this.slideSpeedMult, 1f + fractOvercharge / 2f);
                    if (fractOvercharge >= criticAmount)
                    {
                        this.Charge -= amountOvercharge * Mathf.Pow((fractOvercharge - criticAmount) / (1 - criticAmount), 4);
                    }
                    
                }
            }
            else
            {
                // this.slideOverchargedBoost = false;
                this.slideSpeedframes = 0;
                this.slideSpeedMult = 1f;
                this.slideSpearBounceFrames = 0;
            }
        }
        private void MovementUpdate()
        {
            Player player = this.Player;
            oldpos = newpos;

            if (player != null)
            {
                Vector2 pos = Vector2.zero;
                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    pos += bodyChunk.pos;
                }
                pos /= player.bodyChunks.Length;

                if (oldpos == Vector2.negativeInfinity)
                {
                    oldpos = pos;
                }
                newpos = pos;
            }
            else
            {
                oldpos = Vector2.negativeInfinity;
                newpos = Vector2.negativeInfinity;
            }
        }
        //-------------- Override Functions
        public void Update()
        {
            if (!this.init)
            {
                InitPlayerStaticCharge();
                return;
            }

            if (this.displayBattery && this.Room != null)
            {
                if (this.staticChargeBatteryUI == null)
                {
                    this.staticChargeBatteryUI = new StaticChargeBatteryUI(this);
                    this.Room.AddObject(this.staticChargeBatteryUI);
                }
                if (this.staticChargeBatteryUI != null && this.Room != this.staticChargeBatteryUI.room)
                {
                    this.staticChargeBatteryUI.RemoveFromRoom();
                    this.Room.AddObject(this.staticChargeBatteryUI);
                }
            }
            else if (!(this.displayBattery && this.Room != null) && this.staticChargeBatteryUI != null)
            {
                this.Room.RemoveObject(this.staticChargeBatteryUI);
                this.staticChargeBatteryUI.Destroy();
                this.staticChargeBatteryUI = null;
            }

            if (this.Room != null)
            {
                if (this.active && !this.Player.dead)
                {
                    if (Plugin.meadowEnabled && this.isMeadowArenaTimerCountdown && OnlineTimerOn())
                    {
                        this.dischargeCooldown = 3;
                    }
                    else
                    {
                        if (this.dischargeCooldown > 0) { this.dischargeCooldown--; }
                        if (this.isMeadowArenaTimerCountdown) { this.isMeadowArenaTimerCountdown = false; }
                    }
                    if (this.MaxEBounce > 0 && !this.isMeadowArenaTimerCountdown) { BounceUpdate(); }
                    if (!this.isMeadowArenaTimerCountdown) { DischargeUpdate(); }
                    if (this.RechargeMult > 0 && !this.isMeadowFakePlayer) { MovementUpdate(); RechargeUpdate(); }
                    if (!this.isMeadowFakePlayer) { SlideUpdate(); }
                    if (this.IsOvercharged && !this.isMeadowFakePlayer && !this.isMeadowArenaTimerCountdown) { OverchargeUpdate(); }
                }
                if (this.particles) {
                    ParticlesUpdate(); 
                }
            }
            if (this.particles && ModManager.MSC) {
                MoreSlugcatCompat.StaticManager_UpdateLigningArcs(this);
            }
        }

        //-------------- Public Functions
        public void DoSparks()
        {
            if (this.Player != null)
            {
                DoSparks(this.Player.firstChunk.pos + 25f * (this.DirectionalInput == Vector2.zero ? new Vector2(this.Player.ThrowDirection, 0) : this.DirectionalInput));
            }
        }
        public void DoSparks(Vector2 position)
        {
            if (this.Room != null)
            {
                this.Charge -= 0.1f;
                this.Room.AddObject(new MouseSpark(position, new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f)), 5f, this.Player.ShortCutColor()));
            }
        }

        public bool Discharge(float reach, float damage, float chargeNeeded)
        {
            if (this.Player == null) { return false; }
            return Discharge(reach, damage, chargeNeeded, Player.firstChunk.pos);
        }
        public bool Discharge(float reach, float damage, float chargeNeeded, Vector2 position)
        {
            return Discharge(reach, damage, chargeNeeded, position, reach * damage / 150f);
        }
        public bool Discharge(float reach, float damage, float chargeNeeded, Vector2 position, float volume)
        {
            if (this.Player == null || this.Room == null) { return false; }
            
            Player pl = this.Player;
            Room room = this.Room;
            Color color = pl.ShortCutColor();

            if (this.Charge >= chargeNeeded && this.dischargeCooldown <= 0)
            {
                this.dischargeCooldown = this.MaxDischargeCooldown;
                this.Charge -= chargeNeeded;
                bool underwater = false;

                if (BTWFunc.BodyChunkSumberged(pl.firstChunk))
                {
                    reach *= 4;
                    damage *= 0.75f;
                    underwater = true;
                }

                byte sparks = (byte)(UnityEngine.Random.Range(5f, 10f) * (1 + damage));
                MakeSparkExplosion(room, reach, position, sparks, underwater, color);
                if (damage > 1f) { room.ScreenMovement(position, default, damage / 5f); }

                room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, position, 0.5f + Math.Min(1f, volume), UnityEngine.Random.Range(1.1f, 1.5f));
                room.PlaySound(SoundID.Bomb_Explode, position, volume / 2f, UnityEngine.Random.Range(1.75f, 2.25f));
                if (this.isMeadow && this.active)
                {
                    MeadowCompat.SparkMeadow_DischargeRPC(
                        this, (short)Mathf.Clamp(reach, 0, short.MaxValue), position,
                        sparks, (byte)Mathf.Clamp(volume * 100, 0, byte.MaxValue));
                }
                if (this.active) { room.InGameNoise(new Noise.InGameNoise(position, chargeNeeded, pl, 1f)); }
                if (this.isMeadowFakePlayer) { damage = 0f; }
                
                if (damage > 0f)
                {
                    foreach (AbstractCreature Abcreature in room.abstractRoom.creatures)
                    {
                        if (Abcreature.realizedCreature != null && !Abcreature.realizedCreature.inShortcut && !Abcreature.realizedCreature.dead)
                        {
                            Creature creature = Abcreature.realizedCreature;
                            foreach (var g in pl.grasps) { if (g != null && g.grabbed.Equals(creature)) { continue; } }
                            if (Plugin.meadowEnabled && this.isMeadowArena && MeadowAlly(pl, creature)) { continue; }
                            
                            if (!pl.Equals(creature))
                            {
                                float dist = -1;
                                BodyChunk cbody = null;
                                Vector2 vecDist = Vector2.one;
                                bool csubmerged = false;
                                foreach (BodyChunk c in creature.bodyChunks)
                                {
                                    csubmerged = BTWFunc.BodyChunkSumberged(c);
                                    if (!csubmerged && underwater) { continue; }
                                    Vector2 cdist = c.pos - position;
                                    float realdist = Math.Max(0, cdist.magnitude - c.rad);
                                    if (realdist < reach && (realdist < dist || cbody == null))
                                    {
                                        cbody = c;
                                        dist = realdist;
                                        vecDist = cdist.normalized;
                                    }
                                }
                                if (cbody != null)
                                {
                                    float ratioDist = Math.Max(1 - (dist / reach), 0);
                                    float dmg = damage / 2f + (damage / 2f) * Mathf.Pow(ratioDist, csubmerged ? 3 : 2);
                                    int stun = (int)(((dmg + 1) * 0.5f + reach * ratioDist * 0.01f + (csubmerged ? 1 : 0)) * BTWFunc.FrameRate);
                                    bool cthroughWall = room.VisualContact(position, vecDist * dist);

                                    if (cthroughWall) { dmg *= 0.35f; stun = (int)(stun * 0.25f); }
                                    if (creature is Player && !this.DoDischargeDamagePlayers) { dmg = 0f; stun *= 2; }
                                    
                                    creature.SetKillTag(pl.abstractCreature);
                                    creature.Violence(null, vecDist * ratioDist, cbody, null, Creature.DamageType.Electric, dmg, stun);

                                    ShockCreatureEffect(cbody, dmg, !this.isMeadowFakePlayer);
                                    // room.PlaySound(SoundID.Death_Lightning_Spark_Object, cbody.pos, 0.75f * volume, UnityEngine.Random.Range(1.75f, 2.25f));
                                    // room.AddObject(new ShockWave(cbody.pos, reach * 0.5f, 0.001f, 100, false));
                                }
                            }
                        }
                    }
                }
                
                return true;
            }
            else
            {
                DoSparks(position);
            }
            return false;
        }
        public void ShockCreatureEffect(BodyChunk targetChunk, float damage, bool isReal = true)
        {
            if (targetChunk.owner is not Creature creature) { return; }

            Room room = this.Room;
            if (room != null)
            {
                Player player = this.Player;
                bool targetLocal = !this.isMeadow || MeadowCompat.IsCreatureMine(creature.abstractCreature);
                if (targetLocal)
                {
                    targetChunk.vel.x += UnityEngine.Random.Range(5f, 10f) * player.ThrowDirection * damage;
                    targetChunk.vel.y += UnityEngine.Random.Range(5f, 10f) * damage;
                }

                if (ModManager.MSC)
                {
                    if (targetLocal && damage > 1.5f && creature is Player)
                    {
                        MoreSlugcatCompat.StaticManager_CheckIfArtififerShouldExplode(this, creature);
                    }
                    MoreSlugcatCompat.StaticManager_AddLigningArc(this, targetChunk, damage + 1f);
                }
                if (isReal && this.isMeadow && this.active)
                {
                    MeadowCompat.SparkMeadow_DischargeHitRPC(this, creature, damage);
                }
            }
            
        }

        public void BounceUp(bool overcharged)
        {
            if (this.Room != null)
            {
                bool success = Discharge(
                    overcharged ? 25f : 20f,
                    overcharged ? 0.70f : 0.25f,
                    overcharged ? 25f : 15f,
                    this.Player.firstChunk.pos + Vector2.down * 25f
                );

                if (success && !this.isMeadowFakePlayer)
                {
                    this.dischargeCooldown /= 2;
                    float yBounce = overcharged ? 17.5f : 13.5f;
                    if (overcharged) { this.Player.flipFromSlide = true; }
                    foreach (BodyChunk bodyChunk in this.Player.bodyChunks)
                    {
                        bodyChunk.vel.x *= overcharged ? 1.25f : 1.1f;
                        bodyChunk.vel.y = Math.Max(yBounce + bodyChunk.vel.y, yBounce);
                    }
                }
            }
        }
        public void BounceBack(bool overcharged)
        {
            if (this.Room != null)
            {
                Player player = this.Player;
                int direction = (int)Mathf.Sign(player.mainBodyChunk.vel.x);
                bool success = Discharge(
                    overcharged ? 35f : 25f,
                    overcharged ? 1.25f : 0.5f,
                    overcharged ? 30f : 20f,
                    player.firstChunk.pos + new Vector2(direction, -0.1f) * 30f
                );

                if (success && !this.isMeadowFakePlayer)
                {
                    this.dischargeCooldown /= 2;
                    float yboost = overcharged ? 15f : 12.5f;

                    this.Room.PlaySound(SoundID.Slugcat_Sectret_Super_Wall_Jump, player.mainBodyChunk, false, 1f, 1.25f);
                    player.bodyChunks[1].pos = player.bodyChunks[0].pos;
                    player.bodyChunks[0].pos += new Vector2(direction * -10f, 10f);
                    player.rollDirection = -direction;
                    player.slideDirection = -direction;
                    // player.animation = Player.AnimationIndex.Flip;
                    // Plugin.Log("BounceBack ! \nPlayer current direction : " + direction
                    //     + "\nWanted direction : " + (-direction)
                    //     + "\nPlayer stats : " 
                    //     + "\nflip : " + player.flipDirection
                    //     + "\nroll : " + player.rollDirection
                    //     + "\nslide : " + player.slideDirection
                    //     + "\nthrow : " + player.ThrowDirection
                    //     + "\nanim : " + player.animation
                    //     + "\nbody : " + player.bodyMode
                    // );

                    foreach (BodyChunk bodyChunk in player.bodyChunks)
                    {
                        bodyChunk.vel.x = (overcharged ? 17.5f : 10f) * -direction;
                        bodyChunk.vel.y = Math.Max(yboost + bodyChunk.vel.y, yboost);
                    }
                    player.jumpStun = 20;
                }
            }
        }
        public void BounceJump(bool overcharged)
        {
            if (this.Room != null)
            {
                bool success = Discharge(
                    overcharged ? 70f : 50f,
                    overcharged ? 1.25f : 0.75f,
                    overcharged ? 50f : 35f,
                    this.Player.firstChunk.pos
                );

                if (success && !this.isMeadowFakePlayer)
                {
                    this.dischargeCooldown /= 2;
                    this.Player.Jump();
                    if (overcharged)
                    {
                        if (this.Player.animation != Player.AnimationIndex.Flip)
                        { this.Player.animation = Player.AnimationIndex.Flip; }
                        else
                        { this.Player.flipFromSlide = true; }
                    }

                    foreach (BodyChunk bodyChunk in this.Player.bodyChunks)
                    {
                        bodyChunk.vel.x *= overcharged ? 1.5f : 1.35f;
                        bodyChunk.vel.y *= overcharged ? 1.85f : 1.5f;
                    }
                }
            }
        }
        public void SlideBoost(bool overcharged)
        {
            if (this.Room != null)
            {
                BodyChunk bodyChunk = this.Player.bodyChunks[0];
                Player player = this.Player;
                bool success = Discharge(
                    overcharged ? 50f : 40f,
                    overcharged ? 0.2f : 0.05f,
                    overcharged ? 35f : 10f,
                    bodyChunk.pos + new Vector2(40f * -player.slideDirection, 0),
                    0.5f
                );

                if (success && !this.isMeadowFakePlayer)
                {
                    this.dischargeCooldown = 10;
                    this.slideSpearBounceFrames = overcharged ? 60 : 20;

                    this.slideSpeedframes = Math.Min(this.slideSpeedframes + (overcharged ? 100 : 25), this.SlideAccelerationFrames);
                    this.slideSpeedMult *= overcharged ? this.SlideOverchargeBoostMult : this.SlideBoostMult;
                }
            }
        }
        
        //-------------- Variables

        // Object Variables
        public AbstractCreature AbstractPlayer;
        public StaticChargeBatteryUI staticChargeBatteryUI;

        // Basic Variables
        public float charge = 0f;

        public int slideSpeedframes = 0;
        public int slideSpearBounceFrames = 0;
        public float slideSpeedMult = 1f;
        public int eBounceLeft = 0;
        public int dischargeCooldown = 20;
        
        public float MaxECharge = 200.0f;
        public float FullECharge = 100.0f;
        public float RechargeMult = 5.0f;
        public float InitSlideSpeed = 5f;        
        public float MaxSlideSpeed = 45f;
        public int SlideAccelerationFrames = 200;
        public float SlideBoostMult = 1.35f;
        public float SlideOverchargeBoostMult = 1.65f;
        public int MaxDischargeCooldown = 40;
        public int MaxEBounce = 1;

        public bool rocketJumpFromBounceJump = false;
        // public bool slideOverchargedBoost = false;
        public bool displayBattery = false;
        public bool particles = false;
        public bool active = false;
        public bool DoDischargeDamagePlayers = true;
        public bool RiskyOvercharge = true;
        public bool DeathOvercharge = true;
        public bool init = false;
        public bool isMeadowFakePlayer = false;
        public bool isMeadow = false;
        public bool isMeadowArena = false;
        public bool isMeadowArenaTimerCountdown = false;
        public Vector2 oldpos = Vector2.negativeInfinity;
        public Vector2 newpos = Vector2.negativeInfinity;

        // Get Set Variables
        public Player Player
        {
            get
            {
                if (AbstractPlayer != null && AbstractPlayer.realizedCreature != null && AbstractPlayer.realizedCreature is Player pl)
                {
                    return pl;
                }
                return null;
            }
        }
        public Room Room
        {
            get
            {
                if (Player != null && Player.room != null)
                {
                    return Player.room;
                }
                return null;
            }
        }
        public float ChargePerSecond
        {
            get
            {
                if ((this.dischargeCooldown > 0 && !this.isMeadowArenaTimerCountdown) || !active) { return 0f; }
                if (this.Player != null)
                {
                    float ActionFriction = 1f;
                    float BodyFriction = 0.5f;
                    float ContactFriction = 0f;
                    Vector2 intDir = this.IntDirectionalInput;

                    if (oldpos == Vector2.negativeInfinity || newpos == Vector2.negativeInfinity)
                    {
                        ContactFriction = 0f;
                    }
                    else
                    {
                        ContactFriction += Mathf.Pow(newpos.magnitude - oldpos.magnitude, 2) / 50f;
                    }

                    if (intDir == Vector2.zero)
                    {
                        ContactFriction = 0f;
                    }

                    if (this.Player.bodyMode == Player.BodyModeIndex.Crawl)
                    {
                        BodyFriction = 25.0f;
                    }
                    else if (this.Player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
                    {
                        BodyFriction = 0.1f;
                    }
                    else if (this.Player.bodyMode == Player.BodyModeIndex.ClimbIntoShortCut)
                    {
                        BodyFriction = 5f;
                    }
                    else if (this.Player.bodyMode == Player.BodyModeIndex.CorridorClimb)
                    {
                        BodyFriction = 12.5f;
                    }
                    else if (this.Player.bodyMode == Player.BodyModeIndex.Stand || this.Player.bodyMode == Player.BodyModeIndex.Default)
                    {
                        BodyFriction = 1f;
                    }
                    else if (this.Player.bodyMode == Player.BodyModeIndex.Swimming)
                    {
                        BodyFriction = 0.75f;
                    }
                    else if (this.Player.bodyMode == Player.BodyModeIndex.WallClimb)
                    {
                        BodyFriction = 2.5f;
                    }
                    else if (this.Player.bodyMode == Player.BodyModeIndex.ZeroG)
                    {
                        BodyFriction = 0.5f;
                    }

                    if (this.Player.canJump <= 0)
                    {
                        ActionFriction = 0.5f;
                    }

                    if (this.Player.inShortcut)
                    {
                        ActionFriction = 0.05f;
                    }

                    if (this.Player.animation == Player.AnimationIndex.Roll)
                    {
                        ActionFriction = 6f;
                    }
                    else if (this.Player.animation == Player.AnimationIndex.BellySlide)
                    {
                        ActionFriction = 1.5f;
                    }

                    return this.RechargeMult * ActionFriction * BodyFriction * ContactFriction;
                }
                return 0f;
            }
        }
        public Vector2 DirectionalInput
        {
            get
            {
                if (this.Player != null)
                {
                    Player.InputPackage cinput = this.Player.input[0];
                    bool isPC = cinput.controllerType == Options.ControlSetup.Preset.KeyboardSinglePlayer;
                    return isPC ? new Vector2(cinput.x, cinput.y) : cinput.analogueDir;
                }
                return Vector2.zero;
            }
        }
        public Vector2 IntDirectionalInput
        {
            get
            {
                int x = 0; int y = 0;
                Vector2 dirInput = this.DirectionalInput;
                if (dirInput.x > 0.25f) { x = 1; }
                else if (dirInput.x < -0.25f) { x = -1; }
                if (dirInput.y > 0.25f) { y = 1; }
                else if (dirInput.y < -0.25f) { y = -1; }
                return new Vector2(x, y);
            }
        }
        public float Charge
        {
            get
            {
                return this.charge;
            }
            set
            {
                this.charge = Mathf.Clamp(value, 0, Mathf.Max(this.MaxECharge, this.FullECharge));
            }
        }
        public bool IsOvercharged
        {
            get
            {
                return this.MaxECharge > this.FullECharge && this.Charge > this.FullECharge;
            }
        }
        public bool Landed
        {
            get
            {
                Player player = this.Player;
                if (player == null) { return false; }
                return player.canJump > 0 
                    || player.bodyMode == Player.BodyModeIndex.CorridorClimb 
                    || player.bodyMode == Player.BodyModeIndex.Swimming;
            }
        }
    }
    public class StaticChargeBatteryUI : UpdatableAndDeletable, IDrawable
    {
        public StaticChargeBatteryUI(StaticChargeManager staticChargeManager)
        {
            this.SCM = staticChargeManager;
        }
        
        //-------------- Local Functions
        // Sprites
        private void SetBatteryChargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
        {
            float xpos = 13f * Mathf.Clamp01(pourcent) - 7f;
            TriangleMesh BatteryCharge = (TriangleMesh)sLeaser.sprites[2];
            BatteryCharge.MoveVertice(2, new Vector2(xpos, -4f));
            BatteryCharge.MoveVertice(3, new Vector2(xpos, 4f));

            BatteryCharge.color = new Color(1f, 1f, 0.25f);
            BatteryCharge.alpha = Mathf.Clamp01(pourcent * 10);
            sLeaser.sprites[2] = BatteryCharge;
        }
        private void SetBatteryOverchargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
        {
            pourcent = Mathf.Clamp01(pourcent);
            TriangleMesh BatteryOvercharge = (TriangleMesh)sLeaser.sprites[3];
            if (pourcent == 0f)
            {
                BatteryOvercharge.alpha = 0f;
            }
            else
            {
                float xpos = 18f * pourcent - 9.5f;
                BatteryOvercharge.MoveVertice(2, new Vector2(xpos, -5.5f));
                BatteryOvercharge.MoveVertice(3, new Vector2(xpos, 5.5f));

                BatteryOvercharge.alpha = Mathf.Clamp01(pourcent * 5);
                BatteryOvercharge.color = new Color(1f, 0.25f + 0.75f * (1 - pourcent), 0.25f);
            }
            sLeaser.sprites[3] = BatteryOvercharge;
        }
        private void SetBatteryRechargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
        {
            pourcent = Mathf.Clamp01(pourcent);
            float xpos = 18f * pourcent - 9f;
            TriangleMesh BatteryRecharge = (TriangleMesh)sLeaser.sprites[4];
            BatteryRecharge.MoveVertice(2, new Vector2(xpos, -9f));
            BatteryRecharge.MoveVertice(3, new Vector2(xpos, -7f));

            BatteryRecharge.color = new Color(1f, 0.5f + 0.5f * Mathf.Clamp01(2f - pourcent * 2f), 0.25f + 0.75f * Mathf.Clamp01(1f - pourcent * 2f));
            sLeaser.sprites[4] = BatteryRecharge;
        }

        //-------------- Override Functions

        // Battery Drawing
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            foreach (FSprite sprite in sLeaser.sprites)
            {
                rCam.ReturnFContainer("HUD").AddChild(sprite);
            }
        }
        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (this.SCM.Player == null || this.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
                this.Destroy();
                return;
            }
            if (this.SCM.init && this.SCM.active && !this.SCM.Player.inShortcut)
            {
                Vector2 pos = this.SpriteHeadPos == Vector2.negativeInfinity ?
                    this.SCM.Player.firstChunk.pos + new Vector2(0f, 40f)
                    : this.SpriteHeadPos + new Vector2(0f, 20f);
                float OverChargeFactor = this.SCM.IsOvercharged ? (this.SCM.Charge - this.SCM.FullECharge) / (this.SCM.MaxECharge - this.SCM.FullECharge) : 0f;
                Vector2 shakeFactor = this.SCM.IsOvercharged ? new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * Mathf.Pow(OverChargeFactor, 3f) : Vector2.zero;

                foreach (FSprite sprite in sLeaser.sprites)
                {
                    sprite.x = pos.x + shakeFactor.x;
                    sprite.y = pos.y + shakeFactor.y;
                    sprite.alpha = 1f;
                }

                SetBatteryChargeSprite(sLeaser, this.SCM.FullECharge > 0 ? this.SCM.Charge / this.SCM.FullECharge : 0);
                SetBatteryRechargeSprite(sLeaser, Mathf.Sqrt(this.SCM.ChargePerSecond / (this.SCM.FullECharge / 2)));
                if (this.SCM.MaxECharge > this.SCM.FullECharge)
                {
                    SetBatteryOverchargeSprite(sLeaser, this.SCM.MaxECharge > 0 ? OverChargeFactor : 0);
                }
            }
            else
            {
                foreach (FSprite sprite in sLeaser.sprites)
                {
                    sprite.alpha = 0f;
                }
            }

        }
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[5];

            TriangleMesh BatteryOutline = new(
                "Futile_White",
                new TriangleMesh.Triangle[] {
                    new(0, 1, 2), new(0, 2, 3),
                    new(4, 5, 6), new(4, 6, 7),
                    new(8, 9, 10), new(8, 10, 11),
                    new(3, 12, 13), new(3, 4, 13),
                    new(2, 14, 15), new(2, 5, 15),
                },
                true, false
            );
            BatteryOutline.MoveVertice(0, new Vector2(-9.5f, 6.5f));
            BatteryOutline.MoveVertice(1, new Vector2(-9.5f, -6.5f));
            BatteryOutline.MoveVertice(2, new Vector2(-9f, -6.5f));
            BatteryOutline.MoveVertice(3, new Vector2(-9f, 6.5f));

            BatteryOutline.MoveVertice(12, new Vector2(-9f, 6f));
            BatteryOutline.MoveVertice(14, new Vector2(-9f, -6f));

            BatteryOutline.MoveVertice(4, new Vector2(8f, 6.5f));
            BatteryOutline.MoveVertice(5, new Vector2(8f, -6.5f));
            BatteryOutline.MoveVertice(6, new Vector2(8.5f, -6.5f));
            BatteryOutline.MoveVertice(7, new Vector2(8.5f, 6.5f));

            BatteryOutline.MoveVertice(13, new Vector2(8f, 6f));
            BatteryOutline.MoveVertice(15, new Vector2(8f, -6f));

            BatteryOutline.MoveVertice(8, new Vector2(8.5f, 3.5f));
            BatteryOutline.MoveVertice(9, new Vector2(8.5f, -3.5f));
            BatteryOutline.MoveVertice(10, new Vector2(9.5f, -3.5f));
            BatteryOutline.MoveVertice(11, new Vector2(9.5f, 3.5f));

            BatteryOutline.color = new Color(0.1f, 0.1f, 0.1f);
            sLeaser.sprites[0] = BatteryOutline;


            TriangleMesh BatteryBG = new(
                "Futile_White",
                new TriangleMesh.Triangle[] {
                    new(0, 1, 2), new(0, 2, 3),
                    new(4, 5, 6), new(4, 6, 7),
                    new(8, 9, 10), new(8, 10, 11),
                    new(3, 12, 13), new(3, 4, 13),
                    new(2, 14, 15), new(2, 5, 15),
                },
                true, false
            );
            BatteryBG.MoveVertice(0, new Vector2(-9f, 6f));
            BatteryBG.MoveVertice(1, new Vector2(-9f, -6f));
            BatteryBG.MoveVertice(2, new Vector2(-8f, -6f));
            BatteryBG.MoveVertice(3, new Vector2(-8f, 6f));

            BatteryBG.MoveVertice(12, new Vector2(-8f, 5f));
            BatteryBG.MoveVertice(14, new Vector2(-8f, -5f));

            BatteryBG.MoveVertice(4, new Vector2(7f, 6f));
            BatteryBG.MoveVertice(5, new Vector2(7f, -6f));
            BatteryBG.MoveVertice(6, new Vector2(8f, -6f));
            BatteryBG.MoveVertice(7, new Vector2(8f, 6f));

            BatteryBG.MoveVertice(13, new Vector2(7f, 5f));
            BatteryBG.MoveVertice(15, new Vector2(7f, -5f));

            BatteryBG.MoveVertice(8, new Vector2(8f, 3f));
            BatteryBG.MoveVertice(9, new Vector2(8f, -3f));
            BatteryBG.MoveVertice(10, new Vector2(9f, -3f));
            BatteryBG.MoveVertice(11, new Vector2(9f, 3f));

            BatteryBG.color = new Color(1f, 1f, 1f);
            sLeaser.sprites[1] = BatteryBG;


            TriangleMesh BatteryCharge = new(
                "Futile_White",
                new TriangleMesh.Triangle[] {
                    new(0, 1, 2), new(0, 2, 3)
                },
                true, false
            );
            BatteryCharge.MoveVertice(0, new Vector2(-6f, 4f));
            BatteryCharge.MoveVertice(1, new Vector2(-6f, -4f));
            BatteryCharge.MoveVertice(2, new Vector2(5f, -4f));
            BatteryCharge.MoveVertice(3, new Vector2(5f, 4f));

            BatteryCharge.color = new Color(1f, 1f, 0.25f);
            sLeaser.sprites[2] = BatteryCharge;


            TriangleMesh BatteryOvercharge = new(
                "Futile_White",
                new TriangleMesh.Triangle[] {
                    new(0, 1, 2), new(0, 2, 3)
                },
                true, false
            );
            BatteryOvercharge.MoveVertice(0, new Vector2(-8.5f, 5.5f));
            BatteryOvercharge.MoveVertice(1, new Vector2(-8.5f, -5.5f));
            BatteryOvercharge.MoveVertice(2, new Vector2(7.5f, -5.5f));
            BatteryOvercharge.MoveVertice(3, new Vector2(7.5f, 5.5f));

            BatteryOvercharge.color = new Color(1f, 1f, 0.25f);
            sLeaser.sprites[3] = BatteryOvercharge;


            TriangleMesh BatteryRecharge = new(
                "Futile_White",
                new TriangleMesh.Triangle[] {
                    new(0, 1, 2), new(0, 2, 3)
                },
                true, false
            );
            BatteryRecharge.MoveVertice(0, new Vector2(-9f, -7f));
            BatteryRecharge.MoveVertice(1, new Vector2(-9f, -9f));
            BatteryRecharge.MoveVertice(2, new Vector2(9f, -9f));
            BatteryRecharge.MoveVertice(3, new Vector2(9f, -7f));

            BatteryRecharge.color = new Color(1f, 0.75f, 0.25f);
            sLeaser.sprites[4] = BatteryRecharge;

            this.AddToContainer(sLeaser, rCam, null);
        }

        //-------------- Variables
        public StaticChargeManager SCM;
        public Vector2 SpriteHeadPos
        {
            get
            {
                if (this.SCM != null && this.SCM.Player != null && BTWSkins.cwtPlayerSpriteInfo.TryGetValue(this.SCM.AbstractPlayer, out var psl))
                {
                    return psl[3].GetPosition();
                }
                return Vector2.negativeInfinity;
            }
        }
    }
    
    // Functions
    public static void ApplyHooks()
    {
        IL.Player.ThrowObject += Player_StaticManager_SlideSpearBounce;
        // On.Player.SpitOutOfShortCut += Player_MoveSparkUI;
        Plugin.Log("SparkObject ApplyHooks Done !");
    }

    public static void MakeSparkExplosion(Room room, float size, Vector2 position, byte sparks, bool underwater, Color color)
    {
        if (underwater) { room.AddObject(new UnderwaterShock.Flash(position, size, 1f, 30, color)); }

        room.AddObject(new Explosion.ExplosionLight(position, size, 1f, 5, color));
        room.AddObject(new ShockWave(position, size, 0.001f, 50, false));
        for (int i = sparks; i >= 0; i--)
        {
            room.AddObject(new MouseSpark(position, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)), 25f, color));
        }
    }
    private static bool OnlineTimerOn()
    {
        if (Plugin.meadowEnabled)
        {
            return MeadowCompat.ShouldHoldFireFromOnlineArenaTimer();
        }
        return false;
    }
    private static bool MeadowAlly(Player pl, Creature creature)
    {
        if (Plugin.meadowEnabled)
        {
            return MeadowCompat.IsCreatureFriendlies(pl, creature);
        }
        return false;
    }

    // Hook
    private static void Player_StaticManager_SlideSpearBounce(ILContext il)
    {
        Plugin.Log("StaticManager IL starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(0),
                x => x.MatchLdfld<IntVector2>(nameof(IntVector2.x)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.rollDirection)),
                x => x.MatchBneUn(out _),
                x => x.MatchLdarg(0),
                x => x.MatchCall(out _),
                x => x.MatchLdfld<SlugcatStats>(nameof(SlugcatStats.throwingSkill)),
                x => x.MatchLdcI4(0)
            ))
            {
                static int IsAbleToSpearBounce(int orig, Player player)
                {
                    if (SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
                    {
                        return (SCM.slideSpearBounceFrames > 0 || SCM.slideSpeedframes > SCM.SlideAccelerationFrames / 2 || SCM.slideSpeedMult > 1.5f) ? 1 : 0;
                    }
                    return orig;
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(IsAbleToSpearBounce);
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
        Plugin.Log("StaticManager IL ends");
    }
    private static void Player_MoveSparkUI(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
    {
        orig(self, pos, newRoom, spitOutAllSticks);
        if (SparkFunc.cwtSpark.TryGetValue(self.abstractCreature, out var staticChargeManager))
        {
            if (staticChargeManager.staticChargeBatteryUI != null)
			{
				// now what ??
			}
        }
    }
}