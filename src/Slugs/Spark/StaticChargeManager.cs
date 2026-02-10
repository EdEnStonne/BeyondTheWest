using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using BeyondTheWest.MSCCompat;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace BeyondTheWest;
public class StaticChargeManager
{
    public static ConditionalWeakTable<AbstractCreature, StaticChargeManager> chargeManagers = new();
    public static bool TryGetManager(AbstractCreature creature, out StaticChargeManager SCM)
    {
        return chargeManagers.TryGetValue(creature, out SCM);
    }
    public static StaticChargeManager GetManager(AbstractCreature creature)
    {
        TryGetManager(creature, out StaticChargeManager SCM);
        return SCM;
    }
    public static void AddManager(AbstractCreature creature, out StaticChargeManager SCM)
    {
        SCM = new(creature);
        chargeManagers.Add(creature, SCM);
    }
    public static void AddManager(AbstractCreature creature)
    {
        AddManager(creature, out _);
    }
    public static void RemoveManager(AbstractCreature creature)
    {
        if (TryGetManager(creature, out var SCM))
        {
            chargeManagers.Remove(creature);
            if (SCM.staticChargeBatteryUI != null && !SCM.staticChargeBatteryUI.slatedForDeletetion)
            {
                SCM.staticChargeBatteryUI.Destroy();
            }
        }
    }

    public StaticChargeManager(AbstractCreature abstractpl)
    {
        this.AbstractPlayer = abstractpl;
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
                this.RiskyOvercharge = BTWRemix.SparkRiskyOvercharge.Value;
                this.DeathOvercharge = BTWRemix.SparkDeadlyOvercharge.Value;

                if (BTWRemix.DoDisplaySparkBattery.Value && this.active)
                {
                    this.displayBattery = true;
                }
                if (BTWPlugin.meadowEnabled)
                {
                    MeadowCalls.SparkMeadow_Init(this);
                }
            }
            else
            {
                this.active = false;
            }
            BTWPlugin.Log("Spark manager Init ! " + this.init + "/" + this.particles + "/" + this.active + "/" + this.isMeadow + "/" + this.isMeadowFakePlayer + "/" + this.displayBattery + "/" + this.dischargeCooldown);
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
                inputs.spec && !lastInputs.spec
            )
            {
                this.eBounceLeft--;
                this.BounceUp();
            }
            else if (
                (
                    player.animation == Player.AnimationIndex.RocketJump ||
                    (BTWPlayerData.TryGetManager(this.AbstractPlayer, out var BTWData) && BTWData.isSuperLaunchJump)
                ) &&
                inputs.spec && !lastInputs.spec && intInput.x * player.mainBodyChunk.vel.x < 0
            )
            {
                this.eBounceLeft--;
                // BTWPlugin.Log("Spark Jump Tech");
                this.BounceBack();
            }
        }
    }
    private void QuickStartUpdate()
    {
        Player player = this.Player;
        if (player != null)
        {
            Player.InputPackage inputs = player.input[0];
            Player.InputPackage lastInputs = player.input[1];
            Vector2 intInput = this.IntDirectionalInput;

            if ((player.standing
                    || player.bodyMode == Player.BodyModeIndex.Crawl
                    || player.bodyMode == Player.BodyModeIndex.Stand
                    || player.animation == Player.AnimationIndex.DownOnFours
                    || player.animation == Player.AnimationIndex.Roll
                    || (!this.rollquickStartbuffer.ended
                        && player.bodyMode == Player.BodyModeIndex.Default
                        && player.animation == Player.AnimationIndex.None))
                && player.animation != Player.AnimationIndex.ZeroGSwim
                && player.animation != Player.AnimationIndex.BellySlide
                && player.animation != Player.AnimationIndex.ZeroGPoleGrab
                && (player.bodyChunks[1].ContactPoint.y < 0 
                    || player.animation == Player.AnimationIndex.Roll) 
                && intInput.x != 0
                && player.input[0].y == -1
                && inputs.spec 
                && !lastInputs.spec)
            {
                QuickStart();
            }

            if (player.animation == Player.AnimationIndex.Roll)
            {
                this.rollquickStartbuffer.ResetUp();
            }
            else
            {
                this.rollquickStartbuffer.Tick();
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
        if (player.dangerGrasp != null)
        {
            if (player.dangerGraspTime < 30
                && BTWPlayerData.TryGetManager(player.abstractCreature, out var bTWPlayerData)
                && !bTWPlayerData.dangerGraspLastSpecButton
                && RWInput.PlayerInput(BTWFunc.GetPlayerNumber(player)).spec)
            {
                ZapToGetFreeTrueCombo();
            }
        }
        else if (inputs.spec && this.dischargeCooldown <= 0)
        {
            if (player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
            {
                bool success = Discharge(
                    overcharged ? 135f : 95f,
                    overcharged ? 0.25f : 0.2f,
                    overcharged ? 70f : 50.0f,
                    pos,
                    overcharged ? 0.8f : 0.6f
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
                if (player.animation == Player.AnimationIndex.BellySlide 
                    && intInput.x == -player.rollDirection 
                    && intInput.y == -1)
                {
                    RollBack();
                }
                else
                {
                    BounceJump();
                    this.rocketJumpFromBounceJump = true;
                }
            }
            else
            {
                Vector2 lookPos = intInput != Vector2.zero ? dirInput.normalized : new Vector2(player.ThrowDirection, 0);
                float range = overcharged ? 40f : 30f;
                Vector2 dischargePos = pos + (player.bodyMode == Player.BodyModeIndex.WallClimb ? lookPos * -0.5f * range : lookPos * range * 0.5f);
                bool success = Discharge(
                    range,
                    overcharged ? 1.15f : 0.8f,
                    overcharged ? 50f : 35f,
                    dischargePos,
                    overcharged ? 0.9f : 0.75f
                );

                if (success && !this.isMeadowFakePlayer)
                {
                    player.Blink(this.MaxDischargeCooldown/2);
                    if (player.bodyMode != Player.BodyModeIndex.WallClimb)
                    {
                        foreach (BodyChunk b in player.bodyChunks)
                        {
                            b.vel.x -= UnityEngine.Random.Range(5f, 7f) * lookPos.x;
                            b.vel.y -= UnityEngine.Random.Range(6f, 8f) * lookPos.y;
                        }
                    }
                    if (overcharged && this.endlessCharge <= 0 && this.overchargeImmunity <= 0)
                    {
                        player.stun = Mathf.Max(player.stun, 30);;
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
        bool forceDischarge = false; // I'll be removing this soon, will see if it still have a use

        if (this.RiskyOvercharge)
        {
            bool isSwimming = player.bodyMode == Player.BodyModeIndex.Swimming;
            if (forceDischarge 
                || (this.CrawlChargeRatio <= 0 
                    && (
                        player.bodyMode == Player.BodyModeIndex.Stand ||
                        player.bodyMode == Player.BodyModeIndex.Crawl ||
                        isSwimming
                    ) 
                    && UnityEngine.Random.Range(0f, 1f) < Mathf.Pow(FractOvercharge, 4) * (isSwimming ? 0.01f : 0.025f)))
            {
                DischargeStun();
            }
        }
    }
    private void RechargeUpdate()
    {
        Player player = this.Player;
        if (player != null && !player.dead)
        {
            if (this.stopConsecutiveDischarge && !player.input[0].spec)
            {
                this.stopConsecutiveDischarge = false;
            }
            this.Charge += this.ChargePerSecond / 40f;
        }
    }
    private void SlideUpdate()
    {
        if (this.Player != null && this.Player.rollCounter > 8 && this.Player.animation == Player.AnimationIndex.BellySlide)
        {
            bool overcharged = this.IsOvercharged;
            Player player = this.Player;
            BodyChunk bodyChunk = player.bodyChunks[0];

            player.rollCounter = 12;
            bodyChunk.vel.x = this.CurrentSlideMomentum * player.rollDirection;

            if (this.slideSpeedframes < this.SlideAccelerationFrames) { this.slideSpeedframes++; }
            if (this.slideSpearBounceFrames > 0) { this.slideSpearBounceFrames--; }
            if (this.slideSpeedMult > 1f) 
            { 
                this.slideSpeedMult = Mathf.Clamp(Mathf.Lerp(this.slideSpeedMult, 1f, 0.01f), 1f, 5f); 
            }

            if (this.slideStun > 0)
            {
                player.exitBellySlideCounter = 0;
            }

            if (player.whiplashJump)
            {
                if (player.input[0].x == -player.rollDirection)
                {
                    this.whiplashJumpBuffer = this.MaxWhiplashJumpBuffer;
                }
                else
                {
                    if (this.whiplashJumpBuffer > 0) { this.whiplashJumpBuffer--; }
                    else { player.whiplashJump = false; }
                }
            }
            else if (this.whiplashJumpBuffer > 0) { this.whiplashJumpBuffer = 0; }
            if (this.slideStun > 0) { this.slideStun--; }

            if (player.bodyChunks[0].ContactPoint.x == player.rollDirection)
            {
                player.bodyChunks[0].vel.x *= -0.5f;
                player.bodyChunks[1].vel.x *= -0.25f;
                player.animation = Player.AnimationIndex.None;
                player.stun = (int)Mathf.Max(0, this.CurrentSlideMomentum - 20);
            }
        }
        else
        {
            // this.slideOverchargedBoost = false;
            this.slideSpeedframes = 0;
            this.slideSpeedMult = 1f;
            this.slideSpearBounceFrames = 0;
            this.whiplashJumpBuffer = 0;
            this.slideStun = 0;
        }
    }
    private void MovementUpdate()
    {
        Player player = this.Player;
        oldpos = newpos;

        if (player != null && player.room != null && player.bodyChunks != null && !player.inShortcut)
        {
            Vector2 pos = Vector2.zero;
            foreach (BodyChunk bodyChunk in player.bodyChunks)
            {
                pos += bodyChunk.pos;
            }
            pos /= player.bodyChunks.Length;

            if (oldpos == Vector2.zero)
            {
                oldpos = pos;
            }
            newpos = pos;
        }
        else
        {
            oldpos = Vector2.zero;
            newpos = Vector2.zero;
        }
    }
    private void CrawlChargeUpdate()
    {
        Player player = this.Player;
        Room room = this.Room;

        if (this.CrawlChargeConditionMet)
        {
            Vector2 pos = player.mainBodyChunk.pos;
            Color color = player.ShortCutColor();
            float chargeFraction = this.CrawlChargeRatio;
            float chargelikeelectricRatio = this.Charge / (this.FullECharge > 0 ? this.FullECharge : this.MaxECharge);

            this.dischargeCooldown = Mathf.Max(10, this.dischargeCooldown);
            if (!this.IsOvercharged)
            {
                this.crawlCharge++;
                player.Blink(5);
            }
            else
            {
                this.crawlCharge--;
            }
            
            if (ModManager.MSC && BTWFunc.Random() < 0.05f + 0.1f * chargelikeelectricRatio)
            {
                LightingArc lightingArc = new (
                    player.bodyChunks[0], player.bodyChunks[1],
                    1f + 3f * chargelikeelectricRatio, 0.25f + 0.25f * chargelikeelectricRatio, 5, color
                )
                {
                    fromOffset = BTWFunc.RandomCircleVector(player.bodyChunks[0].rad * BTWFunc.Random() * 1.5f),
                    targetOffset = BTWFunc.RandomCircleVector(player.bodyChunks[1].rad * BTWFunc.Random() * 1.5f)
                };
                room.AddObject(lightingArc);
            }
            
            if (BTWFunc.Random() < 0.25f)
            {
                room.AddObject(new Spark(pos, BTWFunc.RandomCircleVector(10f), color, null, 5, 20));
                room.PlaySound(SoundID.Centipede_Shock, player.mainBodyChunk, false, 0.15f * chargeFraction, UnityEngine.Random.Range(1.25f, 1.75f));
                if (this.active) { room.InGameNoise(new Noise.InGameNoise(pos, 1000f * chargeFraction, player, 1f)); }
            }
        }
        else if (this.crawlCharge > 0)
        {
            if (this.crawlCharge > 200) { this.crawlCharge = 200; }
            this.crawlCharge--;
        }
    }
    private void DebugUpdate()
    {
        Player player = this.Player;
        Room room = this.Room;
        if (player == null || room == null || !room.game.devToolsActive)
        {
            return;
        }
        
        try
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    this.overchargeImmunity = this.overchargeImmunity > 0 ? 0 : int.MaxValue;
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    this.endlessCharge = this.endlessCharge > 0 ? 0 : int.MaxValue;
                }
                else
                {
                    this.Charge = this.FullECharge > 0 ? this.FullECharge : this.MaxECharge;
                }
            }
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
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
        else if (!this.displayBattery && this.Room != null && this.staticChargeBatteryUI != null)
        {
            this.Room.RemoveObject(this.staticChargeBatteryUI);
            this.staticChargeBatteryUI.Destroy();
            this.staticChargeBatteryUI = null;
        }

        if (this.Room != null && this.Player != null)
        {
            CrawlChargeUpdate();
            if (this.active && !this.Player.dead)
            {
                if (!this.consideredAlive)
                {
                    this.consideredAlive = true;
                    this.overchargeImmunity = Mathf.Max(this.overchargeImmunity, 10);
                }
                if (this.Room.game.devToolsActive) { DebugUpdate(); }
                if (BTWPlugin.meadowEnabled && this.isMeadowArenaTimerCountdown && BTWFunc.OnlineArenaTimerOn())
                {
                    this.overchargeImmunity = Mathf.Max(this.overchargeImmunity, 5);
                }
                else
                {
                    if (this.isMeadowArenaTimerCountdown) { this.isMeadowArenaTimerCountdown = false; }
                }

                if (this.dischargeCooldown > 0) { this.dischargeCooldown--; }
                if (this.MaxEBounce > 0) { BounceUpdate(); }
                QuickStartUpdate();
                DischargeUpdate();
                if (this.RechargeMult > 0 && !this.isMeadowFakePlayer) { MovementUpdate(); RechargeUpdate(); }
                if (!this.isMeadowFakePlayer) { SlideUpdate(); }
                if (this.IsOvercharged 
                    && !this.isMeadowFakePlayer 
                    && this.overchargeImmunity <= 0
                    && this.endlessCharge <= 0) { OverchargeUpdate(); }
                if (!this.isMeadowFakePlayer)
                {
                    if (this.overchargeImmunity > 0) { 
                        this.overchargeImmunity--; 
                        if (this.overchargeImmunity <= 0 && this.Charge >= this.MaximumCharge)
                        {
                            DischargeStun();
                        }
                    }
                    if (this.endlessCharge > 0) { 
                        this.endlessCharge--; 
                        this.charge = this.FullECharge > 0 ? this.FullECharge : this.MaxECharge / 2f;
                    }
                }
            }
            if (this.particles) {
                ParticlesUpdate(); 
            }
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
        return Discharge(reach, damage, chargeNeeded, position, reach * damage / 50f);
    }
    public bool Discharge(float reach, float damage, float chargeNeeded, Vector2 position, float volume)
    {
        if (this.Player == null || this.Room == null) { return false; }
        
        Player pl = this.Player;
        Room room = this.Room;
        Color color = pl.ShortCutColor();

        if (!this.stopConsecutiveDischarge && this.Charge >= chargeNeeded && this.dischargeCooldown <= 0)
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

            if (this.isMeadowArenaTimerCountdown)
            {
                byte sparks = (byte)(UnityEngine.Random.Range(5f, 10f) * (1 + damage));
                ElectricExplosion.MakeSparkExplosion(room, reach, position, sparks, underwater, color);
                room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, position, 0.5f + Math.Min(1f, volume), UnityEngine.Random.Range(1.1f, 1.5f));
                room.PlaySound(SoundID.Bomb_Explode, position, volume / 2f, UnityEngine.Random.Range(1.75f, 2.25f));
                
                if (this.isMeadow)
                {
                    MeadowCalls.ElectricExplosion_SparkExplosionRPC(room, reach, position, sparks, volume, underwater, color);
                }
            }
            else
            {
                int stun = (int)((Mathf.Pow(damage, 2) + reach * 0.01f + (underwater ? 2f : 0.5f)) * BTWFunc.FrameRate);
                Vector2 knockbackdir = position - pl.mainBodyChunk.pos;

                ElectricExplosion electricExplosion = new(room, pl, position, 1, reach, 22f * damage,
                    damage, stun, pl, 0, 0.7f, underwater, false, this.isMeadow && this.active)
                {
                    color = color,
                    forcedKnockbackDirection = knockbackdir.magnitude > 10f ? knockbackdir.normalized : Vector2.zero,
                    hitNonSubmerged = !underwater,
                    hitPlayer = this.DoDischargeDamagePlayers,
                    volume = volume
                };
                room.AddObject( electricExplosion );
            }

            return true;
        }
        else
        {
            DoSparks(position);
        }
        return false;
    }

    public void BounceUp()
    {
        if (this.Room != null)
        {
            bool overcharged = this.IsOvercharged;

            Player player = this.Player;
            float reach = overcharged ? 40f : 30f;
            // Vector2 intInput = this.IntDirectionalInput;
            int bounceDir = 1; //(int)(intInput.y == 0 ? 1 : intInput.y);

            bool success = Discharge(
                reach,
                overcharged ? 0.35f : 0.15f,
                overcharged ? 25f : 15f,
                player.mainBodyChunk.pos + new Vector2(0, -bounceDir) * reach * 0.8f,
                overcharged ? 0.075f : 0.04f
            );

            if (success && !this.isMeadowFakePlayer)
            {
                this.dischargeCooldown = 10;
                this.stopConsecutiveDischarge = true;

                player.room.PlaySound(SoundID.Slugcat_Flip_Jump, player.mainBodyChunk, false, 1.25f, BTWFunc.Random(1.2f, 1.35f));
                player.animation = Player.AnimationIndex.Flip;
                if (overcharged && ! player.flipFromSlide) { player.flipFromSlide = true; }
                float yboost = overcharged ? 13f : 9f;
                
                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    bodyChunk.vel.x *= overcharged ? 1.2f : 1.1f;
                    bodyChunk.vel.y = Math.Max(yboost + bounceDir * bodyChunk.vel.y, yboost) * bounceDir;
                }
            }
        }
    }
    public void BounceBack()
    {
        if (this.Room != null)
        {
            bool overcharged = this.IsOvercharged;
            
            Player player = this.Player;
            int direction = (int)Mathf.Sign(player.mainBodyChunk.vel.x);
            float reach = overcharged ? 60f : 35f;

            bool success = Discharge(
                reach,
                overcharged ? 0.65f : 0.3f,
                overcharged ? 40f : 20f,
                player.firstChunk.pos + new Vector2(direction, -0.1f) * reach * 0.85f,
                overcharged ? 0.1f : 0.055f
            );

            if (success && !this.isMeadowFakePlayer)
            {
                this.dischargeCooldown = 10;
                this.stopConsecutiveDischarge = true;

                player.room.PlaySound(SoundID.Slugcat_Sectret_Super_Wall_Jump, player.mainBodyChunk, false, 1.5f, BTWFunc.Random(1.3f, 1.4f));
                player.bodyChunks[1].pos = player.bodyChunks[0].pos;
                player.bodyChunks[0].pos += new Vector2(direction * -10f, 10f);


                float yboost;
                float xboost;
                if (player.input[0].y == 1)
                {
                    player.rollDirection = -direction;
                    player.animation = Player.AnimationIndex.Flip;
                    player.flipFromSlide = true;
                    yboost = overcharged ? 16.5f : 13f;
                    xboost = -5f;
                }
                else
                {
                    player.jumpStun = -direction * 10;
                    player.animation = Player.AnimationIndex.RocketJump;
                    yboost = overcharged ? 12.5f : 11f;
                    xboost = overcharged ? 16f : 12f;
                }

                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    bodyChunk.vel.x = xboost * -direction;
                    bodyChunk.vel.y = Math.Max(yboost + bodyChunk.vel.y, yboost);
                }
            }
        }
    }
    public void BounceJump()
    {
        if (this.Room != null)
        {
            bool overcharged = this.IsOvercharged;
            
            Player player = this.Player;
            float reach = overcharged ? 70f : 50f;
            bool success = Discharge(
                reach,
                overcharged ? 0.85f : 0.55f,
                overcharged ? 55f : 35f,
                player.firstChunk.pos + new Vector2(-this.IntDirectionalInput.x, 0) * reach * 0.75f,
                overcharged ? 0.85f : 0.65f
            );

            if (success && !this.isMeadowFakePlayer)
            {
                this.dischargeCooldown = 20;
                this.stopConsecutiveDischarge = true;
                player.Jump();
                if (overcharged)
                {
                    if (player.animation != Player.AnimationIndex.Flip)
                    { 
                        player.animation = Player.AnimationIndex.Flip; 
                    }
                    else
                    { 
                        player.flipFromSlide = true; 
                    }
                }

                foreach (BodyChunk bodyChunk in player.bodyChunks)
                {
                    bodyChunk.vel.x *= 0.35f;
                    bodyChunk.vel.y *= overcharged ? 2.05f : 1.55f;
                }
            }
        }
    }
    public void RollBack()
    {
        if (this.Room != null)
        {
            bool overcharged = this.IsOvercharged;
            
            Player player = this.Player;
            BodyChunk bodyChunk = player.bodyChunks[0];
            float reach = overcharged ? 60f : 40f;
            int rollDir = -player.rollDirection;

            bool success = Discharge(
                reach, 
                overcharged ? 0.4f : 0.2f,
                overcharged ? 45f : 35f,
                bodyChunk.pos + new Vector2(-rollDir, 0) * reach * 0.65f,
                overcharged ? 0.25f : 0.2f
            );

            if (success && !this.isMeadowFakePlayer)
            {
                this.dischargeCooldown = 5;
                this.stopConsecutiveDischarge = true;
                player.animation = Player.AnimationIndex.Roll;
                player.bodyMode = Player.BodyModeIndex.Default;
                player.rollDirection = rollDir;
                player.rollCounter = 0;
                player.bodyChunks[0].vel.x = Mathf.Lerp(player.bodyChunks[0].vel.x, 9f * rollDir, 0.7f);
                player.bodyChunks[1].vel.x = Mathf.Lerp(player.bodyChunks[1].vel.x, 9f * rollDir, 0.7f);
                player.standing = false;
                player.input[0].downDiagonal = rollDir;
                player.room.PlaySound(SoundID.Slugcat_Roll_Init, player.mainBodyChunk, false, 1f, 1f);
            }
        }
    }
    public void QuickStart()
    {
        Room room = this.Room;
        if (room != null)
        {
            Player player = this.Player;
            bool overcharged = this.IsOvercharged;
            float reach = overcharged ? 40f : 30f;
            Vector2 intInput = this.IntDirectionalInput;
            int slideDir = (int)(intInput.x == 0 ? player.ThrowDirection : intInput.x);

            bool success = Discharge(
                reach,
                overcharged ? 0.15f : 0.05f,
                overcharged ? 35f : 15f,
                player.mainBodyChunk.pos + new Vector2(-slideDir, 0) * reach * 0.8f,
                overcharged ? 0.35f : 0.25f
            );

            if (success && !this.isMeadowFakePlayer)
            {
                this.dischargeCooldown = 5;
                this.stopConsecutiveDischarge = true;
                this.slideSpearBounceFrames = 100;
                player.animation = Player.AnimationIndex.BellySlide;
                player.bodyMode = Player.BodyModeIndex.Default;
                player.flipDirection = slideDir;
                player.rollDirection = slideDir;
                player.rollCounter = 12;
                player.standing = false;
                player.room.PlaySound(SoundID.Slugcat_Belly_Slide_Init, player.mainBodyChunk, false, 1f, 1f);
                
                this.slideSpeedframes = 10;
                this.slideSpeedMult = overcharged ? 1.55f : 1.20f;

                BodyChunk mainChuck = player.bodyChunks[0];
                BodyChunk lowerChuck = player.bodyChunks[1];
                if (lowerChuck.ContactPoint.y >= 0)
                {
                    IntVector2 chuckTilePos = room.GetTilePosition(player.bodyChunks[1].pos);
                    if (lowerChuck.SolidFloor(chuckTilePos.x, chuckTilePos.y - 1))
                    {
                        lowerChuck.pos.y = chuckTilePos.y * 20f + lowerChuck.TerrainRad;
                        this.slideStun = 10;
                    }
                    else if (lowerChuck.SolidFloor(chuckTilePos.x, chuckTilePos.y - 2))
                    {
                        lowerChuck.pos.y = (chuckTilePos.y - 1) * 20f + lowerChuck.TerrainRad;
                        this.slideStun = 10;
                    }
                    
                    mainChuck.pos = lowerChuck.pos + new Vector2(slideDir * 10f, 0);
                    lowerChuck.vel.y = -0.5f;
                    lowerChuck.vel.x = CurrentSlideMomentum * slideDir;
                    mainChuck.vel = lowerChuck.vel;
                }
            }
        }
    }
    public void ZapToGetFreeTrueCombo()
    {
        if (this.Room != null)
        {
            Player player = this.Player;
            Creature.Grasp danger = player.dangerGrasp;
            // bool overcharged = this.IsOvercharged;

            if (danger != null)
            {
                BodyChunk dangerChunk = null;
                float dst = float.MaxValue;
                for (int l = 0; l < danger.grabber.bodyChunks.Length; l++)
                {
                    if (Custom.DistLess(player.mainBodyChunk.pos, danger.grabber.bodyChunks[l].pos, dst))
                    {
                        dangerChunk = danger.grabber.bodyChunks[l];
                        dst = Vector2.Distance(player.mainBodyChunk.pos, danger.grabber.bodyChunks[l].pos);
                    }
                }
                if (dangerChunk != null)
                {
                    bool success = Discharge(
                        70f,
                        0.1f,
                        100f,
                        dangerChunk.pos,
                        1f
                    );

                    if (success && !this.isMeadowFakePlayer)
                    {
                        this.dischargeCooldown = BTWFunc.FrameRate * 2;
                        this.Charge = 0;
                        player.room.PlaySound(SoundID.Rock_Hit_Creature, player.mainBodyChunk, false, 1f, UnityEngine.Random.Range(1.85f, 1.9f));
                        Vector2 flingVector = (player.mainBodyChunk.pos - dangerChunk.pos).normalized;
                        foreach (BodyChunk bodyChunk in player.bodyChunks)
                        {
                            bodyChunk.vel += flingVector * 15f;
                        }
                    }
                }
            }
        }
    }

    public void RechargeFromExternalSource(Vector2 sourcePos, float chargeAdded)
    {
        float oldCharge = this.Charge;
        this.Charge += chargeAdded;
        if (oldCharge + chargeAdded >= this.MaximumCharge && this.Player != null && !this.Player.dead)
        {
            if (this.DeathOvercharge)
            {
                OverchargeDeath();
                this.Charge = oldCharge + chargeAdded - this.MaximumCharge;
            }
            else if (this.RiskyOvercharge)
            {
                DischargeStun();
            }
        }

        Player player = this.Player;
        if (ModManager.MSC && player != null && player.room != null) 
        {
            LightingArc lightingArc = new LightingArc(
                sourcePos, player.mainBodyChunk, 
                Mathf.Clamp(chargeAdded / this.MaximumCharge, 0.1f, 1f), Mathf.Clamp01(chargeAdded / 50f), 10, player.ShortCutColor());
            player.room.AddObject(lightingArc);
        }
    }
    public void RechargeFromExternalSource(BodyChunk sourceChunk, float chargeAdded)
    {
        float oldCharge = this.Charge;
        this.Charge += chargeAdded;
        if (oldCharge + chargeAdded >= this.MaximumCharge && this.Player != null && !this.Player.dead)
        {
            if (this.DeathOvercharge)
            {
                OverchargeDeath();
                this.Charge = oldCharge + chargeAdded - this.MaximumCharge;
                BTWPlugin.Log($"Spark [{this.Player}] took <{chargeAdded}> charge at <{oldCharge}> charge and <{this.MaximumCharge}> max charge. Dead is inevitable. Spark has now <{this.Charge}> charge.");
            }
            else if (this.RiskyOvercharge)
            {
                DischargeStun();
            }
        }

        Player player = this.Player;
        if (ModManager.MSC && player != null && player.room != null) 
        {
            LightingArc lightingArc = new LightingArc(
                sourceChunk, player.mainBodyChunk, 
                Mathf.Clamp(chargeAdded / this.MaximumCharge, 0.1f, 1f), Mathf.Clamp01(chargeAdded / 50f), 10, player.ShortCutColor());
            player.room.AddObject(lightingArc);
        }
    }
    
    public void DischargeStun()
    {
        
        Player player = this.Player;
        Room room = this.Room;

        if (player != null && room != null)
        {
            Vector2 pos = player.mainBodyChunk.pos;
            bool isSwimming = player.bodyMode == Player.BodyModeIndex.Swimming;

            float FractCharge = this.Charge / this.CapacityCharge;
            float OverchargeMargin = this.MaximumCharge - this.CapacityCharge;
            float OverchargeCharge = this.Charge - this.CapacityCharge;
            float FractOvercharge = OverchargeMargin > 0 && OverchargeCharge > 0 ? OverchargeCharge / OverchargeMargin : FractCharge;

            Discharge(200f, 0.5f, 0, pos, 0.75f);
            if (OverchargeMargin > 0)
            {
                this.Charge -= OverchargeMargin;
            }
            else
            {
                this.Charge = 0;
            }
            player.Stun((int)((this.MaxECharge - this.FullECharge) * (FractOvercharge * 3f + 1f) / (isSwimming ? 2 : 1) ));
            room.AddObject(new CreatureSpasmer(player, false, player.stun));
            player.LoseAllGrasps();
        }
    }
    public void OverchargeDeath()
    {
        Player player = this.Player;
        Room room = this.Room;
        if (player != null && room != null)
        {
            Vector2 pos = player.mainBodyChunk.pos;
            Color color = player.ShortCutColor();
            
            for (int i = (int)UnityEngine.Random.Range(15f, 25f); i >= 0; i--)
            {
                room.AddObject(new MouseSpark(pos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)), 50f, color));
            }

            room.ScreenMovement(pos, default, 1.1f);
            room.PlaySound(SoundID.Bomb_Explode, pos);
            room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, pos);
            room.InGameNoise(new Noise.InGameNoise(pos, 900f, player, 1f));
            Discharge(this.FullECharge * 1.5f, 1.0f, 0, pos, 1.5f);
            this.Charge = 0;
            BTWPlugin.Log("Seems like Spark "+ player.ToString() +" couldn't handle the charge...");
            player.Die();
        }
    }
    
    //-------------- Variables

    // Object Variables
    public AbstractCreature AbstractPlayer;
    public StaticChargeBatteryUI staticChargeBatteryUI;

    // Basic Variables
    private float charge = 0f;

    public int slideSpeedframes = 0;
    public int slideSpearBounceFrames = 0;
    public float slideSpeedMult = 1f;
    public int eBounceLeft = 0;
    public int dischargeCooldown = 20;
    public int overchargeImmunity = 0;
    public int endlessCharge = 0;
    public int crawlCharge = 0;
    public int whiplashJumpBuffer = 0;
    public int slideStun = 0;
    
    public float MaxECharge = 200.0f;
    public float FullECharge = 100.0f;
    public float RechargeMult = 3f;
    public float InitSlideSpeed = 5f;
    public float MaxSlideSpeed = 45f;
    public int SlideAccelerationFrames = 200;
    public float SlideBoostMult = 1.35f;
    public float SlideOverchargeBoostMult = 1.65f;
    public int MaxDischargeCooldown = 60;
    public int MaxEBounce = 3;
    public int MaxWhiplashJumpBuffer = 5;

    public bool rocketJumpFromBounceJump = false;
    public bool stopConsecutiveDischarge = true;
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
    public bool consideredAlive = false;
    public Vector2 oldpos = Vector2.zero;
    public Vector2 newpos = Vector2.zero;
    public Counter rollquickStartbuffer = new(5);

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
            if (!active || this.endlessCharge > 0) { return 0f; } // if ((this.dischargeCooldown > 0 && !this.CrawlChargeConditionMet && !this.isMeadowArenaTimerCountdown) || !active || this.endlessCharge > 0) { return 0f; }
            Player player = this.Player;
            if (player != null)
            {
                float ActionFriction = 1f;
                float BodyFriction = 1f;
                float ContactFriction = 0f;
                Vector2 intDir = this.IntDirectionalInput;

                if (intDir == Vector2.zero || Mathf.Abs(oldpos.magnitude - newpos.magnitude) < 0.2f) { ContactFriction = 0f; }
                else
                {
                    ContactFriction += Mathf.Pow(
                        (player.bodyChunks[0].vel.magnitude + player.bodyChunks[1].vel.magnitude) / 2f, 2)
                        / 50f;
                    ContactFriction = Mathf.Clamp(ContactFriction, 0, 10);
                }
                if (this.CrawlChargeRatio > 0 && this.CrawlChargeConditionMet)
                {
                    ContactFriction = Mathf.Max(ContactFriction, this.CrawlChargeRatio);
                }


                if (this.CrawlChargeRatio > 0 && this.CrawlChargeConditionMet)
                {
                    BodyFriction = 5f;
                }
                else if (this.IsCrawlingOnFloor)
                {
                    BodyFriction = 50f;
                }
                else if (player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
                {
                    BodyFriction = 0.2f;
                }
                else if (player.bodyMode == Player.BodyModeIndex.ClimbIntoShortCut)
                {
                    BodyFriction = 5f;
                }
                else if (player.bodyMode == Player.BodyModeIndex.CorridorClimb || player.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    BodyFriction = 10f;
                }
                else if (player.bodyMode == Player.BodyModeIndex.Stand || player.bodyMode == Player.BodyModeIndex.Default)
                {
                    BodyFriction = 1f;
                }
                else if (player.bodyMode == Player.BodyModeIndex.Swimming)
                {
                    BodyFriction = 2.5f;
                }
                else if (player.bodyMode == Player.BodyModeIndex.WallClimb)
                {
                    BodyFriction = 30f;
                }
                else if (player.bodyMode == Player.BodyModeIndex.ZeroG)
                {
                    BodyFriction = 0.5f;
                }


                if (!this.Landed)
                {
                    ActionFriction = 0.5f;
                }
                if (player.inShortcut)
                {
                    ActionFriction = 0.05f;
                }

                if (this.CrawlChargeRatio > 0 && this.CrawlChargeConditionMet)
                {
                    ActionFriction = 5f;
                }
                else if (player.animation == Player.AnimationIndex.Roll)
                {
                    ActionFriction = 20f;
                }
                else if (player.animation == Player.AnimationIndex.BellySlide)
                {
                    ActionFriction = 7.5f;
                }
                else if (player.animation == Player.AnimationIndex.RocketJump)
                {
                    ActionFriction = 0.25f;
                }
                else if (BTWPlayerData.TryGetManager(this.AbstractPlayer, out var BTWData) && BTWData.isSuperLaunchJump)
                {
                    ActionFriction = 0.15f;
                }

                if (this.IsOvercharged)
                {
                    float fractOvercharge = (this.Charge - this.FullECharge) / (this.MaxECharge - this.FullECharge);
                    ActionFriction *= 1 - BTWFunc.EaseInOut(fractOvercharge, 3);
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
            float maxcharge = Mathf.Max(this.MaxECharge, this.FullECharge);
            if (this.charge + value > maxcharge 
                && !this.RiskyOvercharge 
                && !this.DeathOvercharge)
            {
                this.endlessCharge += (int)(maxcharge - (this.charge + value));
            }
            this.charge = Mathf.Clamp(value, 0, maxcharge);
        }
    }
    public bool IsOvercharged
    {
        get
        {
            return this.endlessCharge > 0 || (this.MaxECharge > this.FullECharge && this.Charge > this.FullECharge);
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
                || player.bodyMode == Player.BodyModeIndex.Swimming
                || player.bodyMode == Player.BodyModeIndex.Crawl;
        }
    }
    public float CurrentSlideMomentum
    {
        get
        {
            return Mathf.Lerp(
                this.InitSlideSpeed,
                this.MaxSlideSpeed,
                BTWFunc.EaseOut((float)this.slideSpeedframes / this.SlideAccelerationFrames, 2)
            ) * this.slideSpeedMult;
        }
    }
    public bool IsCrawlingOnFloor
    {
        get
        {
            Player player = this.Player;
            if (player != null)
            {
                return player.bodyMode == Player.BodyModeIndex.Crawl
                    && player.animation == Player.AnimationIndex.None;
            }
            return false;
        }
    }
    public float CrawlChargeRatio
    {
        get
        {
            return BTWFunc.EaseOut( Mathf.Clamp01(this.crawlCharge / 200f), 1.5f);
        }
    }
    public bool CrawlChargeConditionMet
    {
        get
        {
            Player player = this.Player;
            Room room = this.Room;
            Vector2 intdir = this.IntDirectionalInput;

            return player != null && room != null 
                && !player.inShortcut && this.IsCrawlingOnFloor 
                && intdir.y == -1 && intdir.x == 0 
                && player.input[0].spec;
        }
    }
    public float MaximumCharge
    {
        get
        {
            return Mathf.Max(this.MaxECharge, this.FullECharge);
        }
    }
    public float CapacityCharge
    {
        get
        {
            return this.FullECharge > 0 ? this.FullECharge : this.MaxECharge;
        }
    }
}

public static class StaticChargeHooks
{
    public static void ApplyHooks()
    {
        On.Player.Update += Player_Electric_Charge_Update;
        IL.Player.ThrowObject += Player_StaticManager_SlideSpearBounce;
        On.Player.Jump += Player_StaticManager_SlideMomentum;
        On.Creature.SuckedIntoShortCut += Player_SparkRechargeNull;
        On.Creature.Violence += Player_Electric_Absorb;
        On.Creature.Die += Player_StaticManager_ConsideredDead;
        IL.Centipede.Shock += Player_CentipedeShock_Absorb;
        IL.ZapCoil.Update += ZapCoil_StaticChargeManager_Absorb;

        BTWPlugin.Log("StaticChargeHooks ApplyHooks Done !");
    }
    
    private static void Player_Electric_Charge_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (StaticChargeManager.TryGetManager(self.abstractCreature, out var SCM))
        {
            SCM.Update();
        }
    }
    
    private static void Player_StaticManager_ConsideredDead(On.Creature.orig_Die orig, Creature self)
    {
        orig(self);
        if (StaticChargeManager.TryGetManager(self.abstractCreature, out var staticChargeManager))
        {
            staticChargeManager.consideredAlive = false;
        }
    }

    private static void Player_SparkRechargeNull(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
    {
        orig(self, entrancePos,carriedByOther);
        if (StaticChargeManager.TryGetManager(self.abstractCreature, out var staticChargeManager))
        {
            staticChargeManager.newpos = Vector2.negativeInfinity;
            staticChargeManager.oldpos = Vector2.negativeInfinity;
        }
    }
    private static void Player_StaticManager_SlideSpearBounce(ILContext il)
    {
        BTWPlugin.Log("StaticChargeManager IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(0),
                x => x.MatchLdfld<IntVector2>(nameof(IntVector2.x)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.rollDirection)),
                x => x.MatchBneUn(out _),
                x => x.MatchLdarg(0),
                x => x.MatchCall(out _),
                x => x.MatchLdfld<SlugcatStats>(nameof(SlugcatStats.throwingSkill))
            ))
            {
                static int IsAbleToSpearBounce(int orig, Player player)
                {
                    if (StaticChargeManager.TryGetManager(player.abstractCreature, out var SCM))
                    {
                        return (SCM.slideSpearBounceFrames > 0 || SCM.slideSpeedframes > BTWFunc.FrameRate * 1.5f || SCM.slideSpeedMult > 1.5f) ? 1 : 0;
                    }
                    return orig;
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(IsAbleToSpearBounce);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("StaticChargeManager IL 1 ends");
    }
    private static void Player_StaticManager_SlideMomentum(On.Player.orig_Jump orig, Player self)
    {
        Player.AnimationIndex oldanim = self.animation;
        orig(self);
        if (StaticChargeManager.TryGetManager(self.abstractCreature, out var staticChargeManager) 
            && oldanim == Player.AnimationIndex.BellySlide 
            && self.animation == Player.AnimationIndex.RocketJump)
        {
            foreach (BodyChunk bodyChunk in self.bodyChunks)
            {
                bodyChunk.vel.x = Mathf.Sign(bodyChunk.vel.x) * Mathf.Max(
                    Mathf.Abs(bodyChunk.vel.x), 
                    staticChargeManager.CurrentSlideMomentum * 0.85f);
            }
        }
    }
    
    private static void Player_CentipedeShock_Absorb(ILContext il) // this is the first IL hook I made myself :D
    {
        BTWPlugin.Log("StaticChargeManager IL 2 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before, x => x.MatchCall<Centipede>("get_Small")))
            {
                static bool CanPlayerShockAbsorb(Creature creature) => creature is Player player && player != null && StaticChargeManager.TryGetManager(player.abstractCreature, out _);

                static void CheckPlayerAbsorb(Centipede self, Creature creature)
                {
                    if (CanPlayerShockAbsorb(creature) && StaticChargeManager.TryGetManager(creature.abstractCreature, out var SCM))
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
                        SCM.RechargeFromExternalSource(self.mainBodyChunk ?? self.firstChunk, AddedCharge);

                        self.Stun(6);
                        self.shockGiveUpCounter = Math.Max(self.shockGiveUpCounter, 30);
                        self.AI.annoyingCollisions = Math.Min(self.AI.annoyingCollisions / 2, 150);

                        BTWPlugin.Log("SHOCKING ! " + AddedCharge);
                    }
                    else
                    {
                        BTWPlugin.Log("Can't shock :<");
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
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("StaticChargeManager IL 2 ends");
    }
    private static void Player_Electric_Absorb(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player player 
            && type == Creature.DamageType.Electric 
            && StaticChargeManager.TryGetManager(player.abstractCreature, out var SCM))
        {
            BTWPlugin.Log($"Spark [{self}] absorbed <{damage}> damage into <{50f * damage}> charge !");
            SCM.RechargeFromExternalSource(source, 80f * damage);
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, 0f, stunBonus / 4);
        }
        else
        {
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }
    }
    
    private static Creature CheckIfSparkIsZapped(Creature creature, int indexBodyPart)
    {
        // Plugin.Log("Checking creature to zap with Zap Coil : ["+ creature +"]");
        if (creature is Player player && StaticChargeManager.TryGetManager(player.abstractCreature, out var SCM))
        {
            BodyChunk bodyChunk = player.bodyChunks[indexBodyPart] ?? player.mainBodyChunk ?? player.firstChunk;
            Vector2 a = bodyChunk.ContactPoint.ToVector2();
            Vector2 v = bodyChunk.pos + a * (bodyChunk.rad + 30f);
            SCM.RechargeFromExternalSource(v, 1500f);
            BTWPlugin.Log("Spark got zapped !");
            return null;
        }
        return creature;
    }
    private static void ZapCoil_StaticChargeManager_Absorb(ILContext il)
    {
        BTWPlugin.Log("StaticChargeManager IL 3 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);

            Func<Instruction, bool>[] iltarget = {
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchLdfld<Room>(nameof(Room.physicalObjects)),
                x => x.MatchLdloc(1),
                x => x.MatchLdelemRef(),
                x => x.MatchLdloc(2),
                x => x.MatchCallvirt(out _),
                x => x.MatchIsinst(typeof(Creature))};

            Func<Instruction, bool>[] iltarget2 = {
                x => x.MatchCallOrCallvirt<Creature>(nameof(Creature.Die))};

            if (cursor.TryGotoNext(MoveType.Before, iltarget2) 
                && cursor.TryGotoPrev(MoveType.Before, iltarget))
            {
                if (cursor.TryGotoPrev(MoveType.After, iltarget))
                {
                    cursor.Emit(OpCodes.Ldloc_3);
                    cursor.EmitDelegate(CheckIfSparkIsZapped);
                }
                else
                {
                    BTWPlugin.logger.LogError("Couldn't find IL hook :<");
                }
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook main :<");
            }
            
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("StaticChargeManager IL 3 ends");
    }

}