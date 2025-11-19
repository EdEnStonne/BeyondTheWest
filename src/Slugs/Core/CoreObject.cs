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
using BeyondTheWest;
using Mono.Cecil.Cil;
using System.Threading;
using UnityEngine.PlayerLoop;

public class CoreObject
{

    public static AbstractPhysicalObject.AbstractObjectType EnergyCoreType;
    public static void ApplyHooks()
    {
        EnergyCoreType = new("EnergyCore", true);
        On.PlayerGraphics.PlayerObjectLooker.HowInterestingIsThisObject += Interest_Exepection_Hook;
        On.Player.SpitOutOfShortCut += Player_MoveAbstractCore;
        Plugin.Log("CoreObject ApplyHooks Done !");
    }

    // My class
    public class AbstractEnergyCore : AbstractPhysicalObject
    {
        public AbstractEnergyCore(AbstractCreature abstractPlayer, EnergyCore energyCore = null)
            : base(abstractPlayer.world, EnergyCoreType, energyCore, abstractPlayer.pos, abstractPlayer.world.game.GetNewID())
        {
            this.abstractPlayer = abstractPlayer;
            this.destroyOnAbstraction = false;
            this.energy = this.CoreMaxEnergy;

            this.active = true;
            this.isMeadow = false;
            this.isMeadowFakePlayer = false;

            if (energyCore != null)
            {
                this.realizedObject = energyCore;
                energyCore.abstractPhysicalObject = this;
                this.RealizedOnce = true;
            }

            if (Plugin.meadowEnabled)
            {
                MeadowCompat.CoreMeadow_Init(this);
            }
        }

        // ----------- Override funcitons
        public override void Realize()
        {
            // Plugin.Log((this.realizedObject != null) + "/" + (this.Player) + "/" + (this.Player.room == this.world?.GetAbstractRoom(this.pos)?.realizedRoom) + "/" + (this.world) + "/" + (this.world?.GetAbstractRoom(this.pos)) + "/" + (this.world?.GetAbstractRoom(this.pos)?.realizedRoom));
            if (this.realizedObject != null || this.Player == null 
                || this.world?.GetAbstractRoom(this.pos)?.realizedRoom == null
                || this.world?.GetAbstractRoom(this.pos)?.realizedRoom != this.Player.room) 
            {
                return;
            }

            Plugin.Log("Realizing Core...");
            this.realizedObject = new EnergyCore(this, this.Player);
            this.realizedObject.room = this.world.GetAbstractRoom(this.pos).realizedRoom;
            this.realizedObject.firstChunk.HardSetPosition(this.Player.mainBodyChunk.pos);

            if (!this.RealizedOnce && this.realizedObject != null && this.realizedObject.room != null) {
                this.RealizedOnce = true; 
                Plugin.Log("Realized for the first time " + this.realizedObject.ToString() + " of " + this.Player.ToString() + " !");
            }

        }
        public override void Update(int time)
        {
            base.Update(time);
            // Plugin.Log("An update in " + time.ToString()); 

            if (abstractPlayer.Room != this.Room) // all of those function doing literally nothing.
            {
                this.Move(abstractPlayer.pos);
                Plugin.Log(this.ToString() + " of " + this.abstractPlayer.ToString() + " changed rooms !!");
            }

            if (!this.RealizedOnce)
            {
                Plugin.Log("Attempting to realize for the first time " + this.ToString() + " of " + this.abstractPlayer.ToString());
                if (this.Player == null || this.Room == null) { return; }
                this.RealizeInRoom();
                return;
            }

            if (this.Player == null && this.realizedObject != null)
            {
                this.Abstractize(abstractPlayer.pos);
            }
            else if (this.Player != null && this.realizedObject == null)
            {
                Plugin.Log("Attempting to realize " + this.ToString() + " of " + this.abstractPlayer.ToString());
                this.RealizeInRoom();
            }

            if (this.realizedObject != null && this.Player != null && this.Player.room != null &&
                this.Player.room != this.realizedObject.room) // why do you not log ???
            {
                Plugin.Log(this.ToString() + " of " + this.abstractPlayer.ToString() + " is not in the good room !!");
                this.Abstractize(abstractPlayer.pos);
                this.RealizeInRoom();
            }

            if (this.isMeadow)
            {
                MeadowCompat.CoreMeadow_Update(this);
            }
        }

        // ----------- Variables

        // Objects
        public AbstractCreature abstractPlayer;

        // Basic
        public float scale = 1f;
        public byte state = 1;
        /// 0 : Deactivated
        /// 1 : Idle, 2 : Boosting, 3 : No Boost left
        /// 4 : Anti-Gravity OFF, 5 : Anti-gravity ON, 6 : SlowDown ON
        /// 7 : Oxygen ON
        /// 10 : Meltdown

        public bool IsBetaBoost = !BTWRemix.Core0GSpecialButton.Value;
        public bool RealizedOnce = false;
    
        public bool active = false;
        public bool isMeadow = false;
        public bool isMeadowFakePlayer = false;
        public bool isShockwaveEnabled = true;
        public bool isMeadowArenaTimerCountdown = false;
        
        public float energy = 100.0f;
        public int boostingCount = 0;
        public int repairCount = 0;
        public int antiGravityCount = 0;
        public int oxygenCount = 0;
        public int slowModeCount = 0;
        public int waterCorrectionCount = 0;
        public int coreBoostLeft = 2;

        public float CoreMaxEnergy = 1200.0f;
        public float CoreEnergyRecharge = 35.0f;
        public float CoreMeltdown = 600.0f;
        public float CoreShockwavePower = 300.0f;
        public float CoreOxygenEnergyUsage = 250.0f;
        public float CoreAntiGravity = 0.85f;
        public int CoreMaxBoost = 2;
        public int CoreAntiGravityStartUp = 5;

        // Get - Set
        public Player Player
        {
            get
            {
                if (abstractPlayer.realizedCreature != null)
                {
                    if (abstractPlayer.realizedCreature is Player player)
                    {
                        return player;
                    }
                }
                return null;
            }
        }
        public EnergyCore RealizedCore
        {
            get
            {
                return (EnergyCore)this.realizedObject;
            }
        }
    }
    public class EnergyCore : PhysicalObject, IDrawable
    {
        public EnergyCore(AbstractEnergyCore abstractEnergyCore, Player player) : base(abstractEnergyCore)
        {
            // Plugin.Log("Core ctor init...");
            
            this.player = player;
            this.color = Color.black;
            this.scale = this.AEC.scale;

            this.collisionLayer = 1;
            base.bodyChunks = new BodyChunk[1];
            base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(player.mainBodyChunk.pos.x, player.mainBodyChunk.pos.y), this.scale, 0.1f)
            {
                collideWithSlopes = false,
                collideWithObjects = false,
                collideWithTerrain = false,
                owner = this,
                rad = 7.5f,
            };
            this.firstChunk.rotationChunk = player.mainBodyChunk;

            base.CollideWithObjects = false;
            base.CollideWithSlopes = false;
            base.CollideWithTerrain = false;
            this.canBeHitByWeapons = true;
            base.gravity = 1f;

            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            Plugin.Log("Core ctor " + this.ToString() + "/"+ this.abstractPhysicalObject.ID + " of " + this.player.ToString() + " done !");
            //this.evenUpdate = !player.evenUpdate;
            //logger.LogDebug(player.mainBodyChunk.owner);
            //logger.LogDebug(this.firstChunk.owner);
            //this.firstChunk.MoveWithOtherObject(true, player.mainBodyChunk, new Vector2(0.5f, -10f));
        }

        //----------------- Local functions
        byte GetCurrentState()
        {
            if (this.player != null && !this.player.dead)
            {
                if (this.AEC.energy <= 0f) { return 10; }
                if (ShouldZeroG)
                {
                    if (this.AEC.antiGravityCount > 0) { return 5; }
                    if (this.AEC.oxygenCount > 0 && this.oxygenCooldown) { return 7; }
                    if (this.AEC.coreBoostLeft <= 0) { return 3; }
                    if (this.AEC.waterCorrectionCount > 0) { return 6; }
                    return 4;
                }
                if (this.AEC.coreBoostLeft <= 0 && !this.Landed) { return 3; }
                if (this.AEC.boostingCount > 0) { return 2; }
                if (this.AEC.slowModeCount > 0 || this.AEC.waterCorrectionCount > 0) { return 6; }
                return 1;
            }
            return 0;
        }
        void SetCurrentState()
        {
            this.AEC.state = GetCurrentState();
        }
        void SetCoreMesh(RoomCamera.SpriteLeaser sLeaser)
        {
            TriangleMesh CoreMesh = (TriangleMesh)sLeaser.sprites[1];

            if (this.player != null)
            {
                byte state = this.AEC.state;

                float energy = this.AEC.energy;
                float eRatio = energy / this.AEC.CoreMaxEnergy;
                float eOUTRatio = Mathf.Pow(eRatio, 1f / 3f);
                float eINRatio = Mathf.Pow(eRatio, 3f);


                if (state == 1 || state == 3)
                {
                    CoreMesh.MoveVertice(0, new Vector2(-4f, 0f));
                    CoreMesh.MoveVertice(1, new Vector2(0f, 4f));
                    CoreMesh.MoveVertice(2, new Vector2(0f, -4f));

                    CoreMesh.MoveVertice(3, new Vector2(0f, 4f));
                    CoreMesh.MoveVertice(4, new Vector2(0f, -4f));
                    CoreMesh.MoveVertice(5, new Vector2(4f, 0f));
                }
                if (state == 4 || state == 5 || state == 6)
                {
                    CoreMesh.MoveVertice(0, new Vector2(-2f, -2f));
                    CoreMesh.MoveVertice(1, new Vector2(-2f, 2f));
                    CoreMesh.MoveVertice(2, new Vector2(2f, 2f));

                    CoreMesh.MoveVertice(3, new Vector2(-2f, -2f));
                    CoreMesh.MoveVertice(4, new Vector2(2f, 2f));
                    CoreMesh.MoveVertice(5, new Vector2(2f, -2f));
                }


                switch (state)
                {
                    case 1: // Idle
                        {
                            this.color = new(0.5f * eRatio, 0.25f + 0.75f * eRatio, 0.5f * eRatio);
                            break;
                        }
                    case 2: // Boosting
                        {
                            float boostCount = this.AEC.boostingCount;
                            float ratio = boostCount / 100f;
                            float MAXratio = boostCount / 400f;
                            float MAXratioSQRT = Mathf.Sqrt(MAXratio);
                            float iMAXratioSQRT = 1 - MAXratioSQRT;

                            this.color = new Color(0.75f + ratio * 0.25f, 0.75f + ratio * 0.25f, ratio * 0.25f);

                            MAXratioSQRT = 0.25f + 0.75f * MAXratioSQRT;
                            iMAXratioSQRT = 0.25f + 0.75f * iMAXratioSQRT;
                            CoreMesh.MoveVertice(0, new Vector2(-4f, 0f));
                            CoreMesh.MoveVertice(1, new Vector2(-4f * MAXratioSQRT, 4f * iMAXratioSQRT));
                            CoreMesh.MoveVertice(2, new Vector2(-4f * MAXratioSQRT, -4f * iMAXratioSQRT));
                            CoreMesh.MoveVertice(3, new Vector2(4f * MAXratioSQRT, 4f * iMAXratioSQRT));
                            CoreMesh.MoveVertice(4, new Vector2(4f * MAXratioSQRT, -4f * iMAXratioSQRT));
                            CoreMesh.MoveVertice(5, new Vector2(4f, 0f));
                            break;
                        }
                    case 3: // Out of Boosts
                        {
                            this.color = new Color(1f, 0.35f, 0.10f);
                            break;
                        }
                    case 4: // Anti-G OFF
                        {
                            this.color = new Color(0.25f * eRatio, 0.25f + 0.25f * eRatio, 0.25f + 0.75f * eRatio);
                            break;
                        }
                    case 5: // Anti-G ON
                        {
                            this.color = new Color(0.25f * eINRatio, 0.5f + 0.5f * eINRatio, 0.5f + 0.5f * eINRatio);
                            break;
                        }
                    case 6: // Slow-Down
                        {
                            this.color = new Color(0.75f + 0.25f * eRatio, 0.75f + 0.25f * eRatio, 0.75f + 0.25f * eRatio);
                            break;
                        }
                    case 7: // Oxygen ON
                        {
                            this.color = new(0.25f + 0.75f * eOUTRatio, 0.5f * eOUTRatio, 0.25f + 0.75f * eOUTRatio);

                            CoreMesh.MoveVertice(0, new Vector2(-4f, 0f));
                            CoreMesh.MoveVertice(1, new Vector2(-1f, 3f));
                            CoreMesh.MoveVertice(2, new Vector2(-1f, -3f));

                            CoreMesh.MoveVertice(3, new Vector2(1f, 3f));
                            CoreMesh.MoveVertice(4, new Vector2(1f, -3f));
                            CoreMesh.MoveVertice(5, new Vector2(4f, 0f));
                            break;
                        }
                    case 10: // Meltdown
                        {
                            float melt_energy = this.AEC.CoreMeltdown;
                            float meltRatio = -energy / melt_energy;
                            int frequency = (int)((1 - meltRatio) * melt_energy / 10f);
                            float red = frequency < 4 ? 1f : Math.Abs((-(int)energy) % frequency - frequency / 2f) / (frequency / 2f);

                            this.color = new Color(0.15f + red * 0.85f, 0.1f, 0.1f);

                            this.room.AddObject(new Spark(this.firstChunk.pos, new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f)) * (1 + meltRatio), Color.red, null, 3, 10));
                            this.room.PlaySound(SoundID.Centipede_Shock, this.firstChunk.pos, 0.1f, UnityEngine.Random.Range(0.75f, 0.9f));

                            float meltRatioSQRT = (float)(Math.Sqrt(-energy) / Math.Sqrt(melt_energy));
                            float imeltRatioSQRT = 0.25f + 0.75f * (1 - meltRatioSQRT);

                            CoreMesh.MoveVertice(0, new Vector2(-2f - 2f * imeltRatioSQRT, -4f * imeltRatioSQRT));
                            CoreMesh.MoveVertice(1, new Vector2(-2f - 2f * imeltRatioSQRT, 4f * imeltRatioSQRT));
                            CoreMesh.MoveVertice(2, new Vector2(2f + 2f * imeltRatioSQRT, 4f * imeltRatioSQRT));

                            CoreMesh.MoveVertice(3, new Vector2(-2f - 2f * imeltRatioSQRT, -4f * imeltRatioSQRT));
                            CoreMesh.MoveVertice(4, new Vector2(2f + 2f * imeltRatioSQRT, 4f * imeltRatioSQRT));
                            CoreMesh.MoveVertice(5, new Vector2(2f + 2f * imeltRatioSQRT, -4f * imeltRatioSQRT));
                            break;
                        }
                    default: // Dead or OFF
                        {
                            this.color = new Color(0.15f, 0.15f, 0.15f);

                            CoreMesh.MoveVertice(0, new Vector2(-2f, 0f));
                            CoreMesh.MoveVertice(1, new Vector2(0f, 2f));
                            CoreMesh.MoveVertice(2, new Vector2(0f, -2f));

                            CoreMesh.MoveVertice(3, new Vector2(0f, 2f));
                            CoreMesh.MoveVertice(4, new Vector2(0f, -2f));
                            CoreMesh.MoveVertice(5, new Vector2(2f, 0f));
                            break;
                        }
                }
            }
            sLeaser.sprites[1] = CoreMesh;
        }

        //------------------ Public functions
        public void MeltdownStart()
        {
            this.player?.Stun((int)(this.AEC.CoreMeltdown / 5));
            this.room?.PlaySound(SoundID.Death_Lightning_Spark_Object, this.firstChunk.pos, 0.75f, 0.9f);
            this.AEC.antiGravityCount = 0;
            this.AEC.boostingCount = -200;
        }
        public void Disable()
        {
            if (this.disableCooldown <= 0)
            {
                this.disableCooldown = BTWFunc.FrameRate * 2;
                if (this.AEC.energy > 0)
                {
                    this.AEC.energy = -this.AEC.CoreMeltdown / 4;
                }
                else
                {
                    this.AEC.energy -= this.AEC.CoreMeltdown / 4;
                }
                if (this.AEC.isMeadowFakePlayer)
                {
                    MeadowCompat.CoreMeadow_DisableRPC(this.AEC);
                }
                else 
                {
                    MeltdownStart();
                }
            }
        }
        public void Boost(float pow, bool isReal = true)
        {
            if (this.player != null && this.room != null)
            {
                bool boostJump = false;
                var pos = this.firstChunk.pos;
                Vector2 inputDir = this.DirectionalInput;
                Vector2 intInput = this.IntDirectionalInput;

                if (isReal && this.Landed)
                {
                    boostJump = true;

                    if (this.player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
                    {
                        if (this.player.animation == Player.AnimationIndex.ClimbOnBeam && intInput.y == 1 && this.player.slideUpPole < 1)
                        {
                            this.player.slideUpPole = (int)Mathf.Min(pow / 4 + 10, 60);
                            this.player.Blink(this.player.slideUpPole / 3);
                            this.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, this.player.mainBodyChunk, false, 0.8f, 1f);
                        }
                        else
                        {
                            this.player.Jump();
                            this.player.animation = Player.AnimationIndex.None;
                        }
                    }
                    else
                    {
                        this.player.Jump();
                    }
                    if (!this.BoostAllowed) { return; }
                }

                this.room.AddObject(new ShockWave(pos - Vector2.down, 25 + pow / 4, 0.05f, 10, false));

                if (pow < 40)
                {
                    this.room.PlaySound(SoundID.Cyan_Lizard_Small_Jump, pos, 0.6f, UnityEngine.Random.Range(1.75f, 2.25f) + pow / 100);
                }
                else if (pow < 100)
                {
                    this.room.PlaySound(SoundID.Cyan_Lizard_Medium_Jump, pos, 0.6f, UnityEngine.Random.Range(1.75f, 2.25f) + pow / 100);
                }
                else
                {
                    this.room.PlaySound(SoundID.Cyan_Lizard_Powerful_Jump, pos, 0.6f, UnityEngine.Random.Range(1.75f, 2.25f) + pow / 100);
                }

                if (isReal)
                {
                    if (!boostJump)
                    {
                        if (this.player.animation == Player.AnimationIndex.Flip)
                        {
                            this.player.flipFromSlide = true;
                        }
                        else
                        {
                            this.player.animation = Player.AnimationIndex.Flip;
                        }
                        this.flipFromBoost = true;
                        this.AEC.coreBoostLeft--;
                    }

                    Vector2 baseBoost = Vector2.zero;
                    Vector2 scaleBoost = Vector2.zero;
                    float diagonalPenalty = 0.5f;
                    if (boostJump)
                    {
                        baseBoost.y = intInput.y == -1 ? this.player.firstChunk.vel.y : 10.0f;
                        scaleBoost.y = (1f + inputDir.y) * 0.085f;

                        baseBoost.x = 7.5f * inputDir.x;
                        scaleBoost.x = inputDir.x * 0.09f;

                        diagonalPenalty = 0.75f;
                    }
                    else
                    {
                        if (intInput.x == 0 && intInput.y == 0) { intInput.y = 1; }

                        baseBoost.y = intInput.y == -1 ? -15f : 8f * (inputDir.y + 0.5f);
                        scaleBoost.y = inputDir.y * 0.20f;

                        baseBoost.x = 10f * inputDir.x;
                        scaleBoost.x = inputDir.x * 0.15f;
                        
                        diagonalPenalty = 0.5f;
                    }

                    scaleBoost *= pow;
                    Vector2 penaltyMultiplier = new Vector2 
                    {
                        x = Mathf.Abs(intInput.y) > 0 ? diagonalPenalty : 1,
                        y = Mathf.Abs(intInput.x) > 0 ? diagonalPenalty : 1
                    };
                    Vector2 leapBoost = new Vector2
                    {
                        x = (baseBoost.x + scaleBoost.x) * penaltyMultiplier.x,
                        y = (baseBoost.y + scaleBoost.y) * penaltyMultiplier.y
                    };



                    foreach (var b in this.player.bodyChunks)
                    {
                        b.vel.x = intInput.x != 0 && b.vel.x * intInput.x > Math.Abs(leapBoost.x) ? 
                            b.vel.x + scaleBoost.x * penaltyMultiplier.x : leapBoost.x;
                        b.vel.y = intInput.y != 0 && b.vel.y * intInput.y > Math.Abs(leapBoost.y) ? 
                            b.vel.y + scaleBoost.y * penaltyMultiplier.y : leapBoost.y;
                    }
                    this.AEC.boostingCount = -15;
                }

                if (this.AEC.energy > 0)
                {
                    this.AEC.energy -= Mathf.Clamp(pow * 3f, 50f, 1000f);
                    if (this.AEC.energy <= 0) { MeltdownStart(); }
                }
                else
                {
                    this.AEC.energy -= 100f;
                }
                
                if (isReal && this.AEC.active && this.AEC.isMeadow)
                {
                    byte reducedPow = (byte)Mathf.Clamp(pow, 0, byte.MaxValue);
                    MeadowCompat.CoreMeadow_BoostRPC(this.AEC, reducedPow);
                }
            }
        }
        public void ShockWave(bool isReal = true)
        {
            if (this.player != null && !this.player.dead && this.AEC.energy > 0f)
            {
                float er = this.AEC.CoreShockwavePower;
                Vector2 vector = Vector2.Lerp(base.firstChunk.pos, base.firstChunk.lastPos, 0.35f);

                if (isReal)
                {
                    this.room.InGameNoise(new Noise.InGameNoise(vector, er * 3f, this, 1f));
                    MeltdownStart();
                    this.player?.Stun((int)(this.AEC.CoreMeltdown / 2));
                }
                if (!this.AEC.isMeadowArenaTimerCountdown)
                {
                    this.room.AddObject(new Explosion(this.room, this, vector, 5, er * 0.25f, 4.00f , 2.25f, 240f, 0.25f, this.player, 0f, 120f, 1f));
                    this.room.AddObject(new Explosion(this.room, this, vector, 5, er        , 1.75f , 0.50f, 120f, 0.25f, this.player, 0f, 30f, 1f));
                }
                this.room.AddObject(new Explosion.ExplosionLight(vector, er * 0.2f  , 0.5f, 10, Color.white));
                this.room.AddObject(new Explosion.ExplosionLight(vector, er         , 1.5f, 60, Color.white));

                if (!this.AEC.isMeadowArenaTimerCountdown)
                {
                    this.room.AddObject(new ShockWave(vector, er * 0.25f, 1.185f, 100, true));
                    this.room.AddObject(new ShockWave(vector, er        , 0.675f, 60, false));
                    this.room.ScreenMovement(new Vector2?(vector), default, 0.4f);
                }
                this.room.PlaySound(SoundID.Bomb_Explode, base.firstChunk);

                this.AEC.energy = 0f;

                if (isReal && this.AEC.active && this.AEC.isMeadow && !this.AEC.isMeadowArenaTimerCountdown)
                {
                    MeadowCompat.CoreMeadow_ShockwaveRPC(this.AEC);
                }
            }
        }
        public void Explode(bool isReal = true)
        {
            if (this.player != null && (!this.player.dead || !isReal))
            {
                var er = 500f;
                Vector2 vector = Vector2.Lerp(base.firstChunk.pos, base.firstChunk.lastPos, 0.35f);

                if (isReal)
                {
                    this.room.InGameNoise(new Noise.InGameNoise(vector, er * 2f, this, 1f));
                    this.player.Die();
                    this.AEC.energy = 1f;
                }

                if (!this.AEC.isMeadowArenaTimerCountdown)
                {
                    this.room.AddObject(new Explosion(this.room, this, vector, 5, er * 0.1f, 3f, 0.75f, 300f, 0.25f, null, 0f, 60f, 1f));
                    this.room.AddObject(new Explosion(this.room, this, vector, 5, er       , 1f, 0.10f, 60f, 0.25f, null, 0.2f, 30f, 1f));
                }
                this.room.AddObject(new Explosion.ExplosionLight(vector, er * 0.1f, 0.5f, 5, Color.white));
                this.room.AddObject(new Explosion.ExplosionLight(vector, er, 1f, 10, Color.white));
                
                if (!this.AEC.isMeadowArenaTimerCountdown)
                {
                    this.room.AddObject(new ShockWave(vector, er * 0.25f, 1.185f, 20, true));
                    this.room.AddObject(new ShockWave(vector, er        , 0.675f, 10, false));
                    this.room.ScreenMovement(new Vector2?(vector), default, 0.2f);
                }
                this.room.PlaySound(SoundID.Bomb_Explode, base.firstChunk);

                if (this.AEC.isMeadow && isReal && this.AEC.active)
                {
                    MeadowCompat.CoreMeadow_ExplodeRPC(this.AEC);
                }
            }
        }
        public void Pop(bool isReal = true)
        {
            if (this.player != null && this.room != null)
            {
                Vector2 pos = this.firstChunk.pos;
                this.room.PlaySound(SoundID.Snail_Pop, pos, 1.0f, 1.5f);
                this.room.AddObject(new ReverseShockwave(pos - Vector2.down, 35, 1f, 20, false));

                if (this.AEC.isMeadow && isReal && this.AEC.active)
                {
                    MeadowCompat.CoreMeadow_PopRPC(this.AEC);
                }
            }
        }
        
        public void AntiGravityUpdate()
        {
            if (this.player != null && this.room != null)
            {
                var cinput = this.player.input[0];
                Vector2 inputDir = this.DirectionalInput;
                Vector2 pos = this.firstChunk.pos;
                bool triggered = this.AEC.IsBetaBoost ? cinput.jmp : cinput.spec;

                float antiGravity = this.AEC.CoreAntiGravity;
                int startUp = this.AEC.CoreAntiGravityStartUp;

                bool Underwater = this.Underwater;
                bool ZeroG = this.ZeroG;

                if (this.AEC.waterCorrectionCount > 0 
                    && (!Underwater || (Underwater && !triggered))
                    && (!ZeroG || (ZeroG && !triggered)))
                {
                    this.AEC.waterCorrectionCount--;
                    this.AEC.antiGravityCount = 0;
                    this.AEC.boostingCount = 0;
                    foreach (var b in this.player.bodyChunks)
                    {
                        b.vel *= antiGravity;
                    }
                }
                else if (ShouldZeroG && triggered && (this.AEC.energy > 0f))
                {
                    this.AEC.boostingCount = 0;
                    if (this.AEC.antiGravityCount == startUp)
                    {
                        Pop();
                        foreach (var b in this.player.bodyChunks)
                        {
                            b.vel.y = Math.Max(b.vel.y, antiGravity * 4f);
                            b.vel.y *= 1.5f;
                            b.vel.x *= 1.5f;
                        }
                        this.AEC.waterCorrectionCount = 0;
                        this.AEC.energy -= 50f;
                        this.AEC.coreBoostLeft--;
                    }
                    else if (this.AEC.antiGravityCount > startUp)
                    {
                        foreach (var b in this.player.bodyChunks)
                        {
                            if (Underwater || ZeroG)
                            {
                                if (this.AEC.waterCorrectionCount == 0)
                                {
                                    b.vel = Vector2.zero;
                                }
                                else
                                {
                                    b.vel = inputDir * (Underwater ? 55f : 7.5f);
                                }
                                if (this.AEC.waterCorrectionCount < (Underwater ? 40 : 80))
                                {
                                    this.AEC.waterCorrectionCount++;
                                }
                            }
                            else
                            {
                                b.vel.y += this.player.gravity * antiGravity;
                            }
                        }
                        this.AEC.energy -= this.AEC.antiGravityCount / (Underwater ? 40f : 100f);
                    }

                    if (this.AEC.energy < 0f)
                    {
                        MeltdownStart();
                    }
                }
                else
                {
                    if (this.AEC.antiGravityCount > 0)
                    {
                        this.AEC.antiGravityCount = 0;
                    }
                    if (this.AEC.waterCorrectionCount > 0)
                    {
                        this.AEC.waterCorrectionCount = 0;
                    }
                }
            }
        }
        public void OxygenUpdate()
        {
            if (this.player != null)
            {
                if (this.player.airInLungs <= 0.85f && this.AEC.energy > 50f && this.oxygenCooldown)
                {
                    this.AEC.energy -= this.AEC.CoreOxygenEnergyUsage * Mathf.Max(0f, 0.85f - this.player.airInLungs);
                    this.AEC.oxygenCount++;
                    this.player.airInLungs = 0.85f;
                    if (this.AEC.energy <= 50f)
                    {
                        this.oxygenCooldown = false;
                        this.room?.PlaySound(SoundID.Death_Lightning_Spark_Object, this.firstChunk.pos, 0.75f, 0.75f);
                    }
                }
                else if (this.AEC.oxygenCount > 0 && this.player.airInLungs > 0.95f)
                {
                    this.oxygenCooldown = true;
                    this.AEC.oxygenCount = 0;
                }
            }
        }
        public void RegenUpdate()
        {
            if (this.player != null)
            {
                if (
                    this.AEC.oxygenCount <= 0 &&
                    this.AEC.repairCount <= 0 &&
                    this.AEC.boostingCount <= 0 &&
                    this.AEC.slowModeCount <= 0 &&
                    this.AEC.antiGravityCount <= 0 &&
                    this.AEC.energy > 0f &&
                    this.AEC.energy < this.AEC.CoreMaxEnergy
                )
                {
                    this.AEC.energy += this.AEC.CoreEnergyRecharge / 40f; 
                    if (this.AEC.energy >= this.AEC.CoreMaxEnergy)
                    {
                        this.AEC.energy = this.AEC.CoreMaxEnergy;
                    }
                }

                if (this.AEC.energy <= 0f && this.AEC.repairCount <= 0)
                {
                    this.AEC.energy--;
                    this.player.slowMovementStun = 40;
                }

                if (this.AEC.boostingCount < 0)
                {
                    this.AEC.boostingCount++;
                }
                else if (player.stun > 0)
                {
                    this.AEC.boostingCount = 0;
                }

                if (this.AEC.antiGravityCount < 0)
                {
                    this.AEC.antiGravityCount++;
                }
                else if (player.stun > 0)
                {
                    this.AEC.antiGravityCount = 0;
                }
                
                if (this.disableCooldown > 0)
                {
                    this.disableCooldown--;
                }

                if (this.Landed || this.Underwater || this.ZeroG)
                {
                    if (!this.Underwater && !this.ZeroG) { this.AEC.antiGravityCount = 0; }
                    this.AEC.coreBoostLeft = this.AEC.CoreMaxBoost;
                    this.flipFromBoost = false;
                }
            }
        }
        public void RepairUpdate()
        {
            if (this.player != null && this.AEC.energy <= 0f && this.room != null)
            {
                var cinput = this.player.input[0];
                var intDir = this.IntDirectionalInput;
                Vector2 pos = this.firstChunk.pos;

                if (cinput.pckp && intDir.x == 0 && intDir.y == 0 && !cinput.jmp && !cinput.thrw && !cinput.spec)
                {
                    this.player.Blink(3);
                    this.AEC.repairCount++;
                    if (this.AEC.energy < -1f) { this.AEC.energy++; }
                    this.player.swallowAndRegurgitateCounter = 0;
                    this.room.PlaySound(SoundID.Spore_Bee_Spark, pos, 0.15f, UnityEngine.Random.Range(0.4f, 0.6f));
                }
                else
                {
                    this.AEC.repairCount = 0;
                }

                if (this.AEC.repairCount >= 100)
                {
                    this.AEC.repairCount = 0;
                    this.AEC.energy = this.AEC.CoreMaxEnergy / 4f;
                    this.room.PlaySound(SoundID.Death_Lightning_Spark_Object, pos, 0.5f, 0.8f);
                }
            }
            
        }
        
        //----------------- IDrawable
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[1]);
            rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[2]);
            rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[3]);
        }
        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
        public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (this.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
            if (this.player != null && !this.player.inShortcut)
            {
                Vector2Int IntDir = this.IntDirectionalInput;
                BodyChunk playerBody = this.player.mainBodyChunk;
                Vector2 corePos = (this.player.bodyChunks[0].pos + this.player.bodyChunks[1].pos) / 2f;
                this.firstChunk.pos = corePos + new Vector2(7.5f, 0) * IntDir.x;
                float eRatio = Mathf.Clamp01(this.AEC.energy / this.AEC.CoreMaxEnergy);
                // var ppos = pbody.pos;

                SetCoreMesh(sLeaser);

                int CinputX = (
                        this.player.bodyMode == Player.BodyModeIndex.Crawl ||
                        this.player.bodyMode == Player.BodyModeIndex.Default ||
                        this.player.bodyMode == Player.BodyModeIndex.Stand) ?
                    IntDir.x : 0;
                var dir = this.player.ThrowDirection;
                var crouchBonus = 
                    (this.player.bodyMode == Player.BodyModeIndex.Crawl ||
                    this.player.bodyMode == Player.BodyModeIndex.CorridorClimb) ? dir : 0f;
                var rot = playerBody.Rotation.GetAngle() + 90 + 20 * CinputX;

                // this.firstChunk.pos = pbody.pos
                //     + BTWFunc.OffsetRelativeToBody(
                //         pbody,
                //         new Vector2(1f * dir + 5f * cinputX + 4f * crouchBonus, 1f - Math.Abs(cinputX))
                //     );
                // vector.x = Mathf.Lerp(base.firstChunk.lastPos.x, base.firstChunk.pos.x, timeStacker) - camPos.x;
                // vector.y = Mathf.Lerp(base.firstChunk.lastPos.y, base.firstChunk.pos.y, timeStacker) - camPos.y;
                Vector2 coreSpritePos = (player.graphicsModule != null && player.graphicsModule is PlayerGraphics playerGraphics ?
                    PlayerMiddleSpritePos : corePos - camPos) + new Vector2(1, 0);
                coreSpritePos += new Vector2(1f * dir + 4f * CinputX, - Math.Abs(CinputX));

                foreach (FSprite sprite in sLeaser.sprites)
                {
                    sprite.x = coreSpritePos.x;
                    sprite.y = coreSpritePos.y;
                }
                
                sLeaser.sprites[0].x = coreSpritePos.x - CinputX * 0.25f;
                sLeaser.sprites[0].scaleX = this.scale - 0.1f * Math.Abs(CinputX);
                sLeaser.sprites[0].rotation = rot; //this.firstChunk.Rotation.GetAngle() + 90; 
                sLeaser.sprites[0].alpha = 1f;
                sLeaser.sprites[0].scale = this.scale;

                sLeaser.sprites[1].scaleX = this.scale - 0.1f * Math.Abs(CinputX);
                sLeaser.sprites[1].color = this.color;
                sLeaser.sprites[1].rotation = rot; //this.firstChunk.Rotation.GetAngle() + 90;
                sLeaser.sprites[1].alpha = 1f;
                sLeaser.sprites[1].scale = this.scale;

                sLeaser.sprites[2].scale = this.scale * (2f + 6f * eRatio);
                sLeaser.sprites[2].color = this.color;

                if (this.AEC.state == 5)
                {
                    sLeaser.sprites[3].alpha = Mathf.Lerp(sLeaser.sprites[3].alpha, 0.30f + 0.30f * eRatio, 0.20f);
                    sLeaser.sprites[3].scale = Mathf.Lerp(sLeaser.sprites[3].scale, 6f * this.AEC.CoreAntiGravity, 0.20f);
                }
                else if (this.AEC.state == 7 || (Underwater && this.oxygenCooldown && this.AEC.energy > 0f))
                {
                    sLeaser.sprites[3].alpha = Mathf.Lerp(sLeaser.sprites[3].alpha, 0.10f + 0.20f * eRatio, 0.05f);
                    sLeaser.sprites[3].scale = Mathf.Lerp(sLeaser.sprites[3].scale, 4f + 12f * eRatio, 0.05f);
                    sLeaser.sprites[2].scale = this.scale * 16f * eRatio;
                }
                else
                {
                    sLeaser.sprites[3].alpha = Mathf.Lerp(sLeaser.sprites[3].alpha, 0f, 0.10f);
                    sLeaser.sprites[3].scale = Mathf.Lerp(sLeaser.sprites[3].scale, 1f, 0.10f);
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
            sLeaser.sprites = new FSprite[4];

            TriangleMesh BGMesh = new(
                "Futile_White",
                new TriangleMesh.Triangle[] { new(0, 1, 2), new(1, 2, 3) },
                true, false
            );
            BGMesh.MoveVertice(0, new Vector2(-5.5f, 0f));
            BGMesh.MoveVertice(1, new Vector2(0f, 8f));
            BGMesh.MoveVertice(2, new Vector2(0f, -8f));
            BGMesh.MoveVertice(3, new Vector2(5.5f, 0f));
            BGMesh.color = new Color(0.1f, 0.1f, 0.1f);
            sLeaser.sprites[0] = BGMesh;

            TriangleMesh CoreMesh = new(
                "Futile_White",
                new TriangleMesh.Triangle[] { new(0, 1, 2), new(1, 2, 3), new(2, 3, 4),  new(3, 4, 5) },
                true, false
            );
            CoreMesh.MoveVertice(0, new Vector2(-4f, 0f));
            CoreMesh.MoveVertice(1, new Vector2(0f, 4f));
            CoreMesh.MoveVertice(2, new Vector2(0f, -4f));
            CoreMesh.MoveVertice(3, new Vector2(0f, 4f));
            CoreMesh.MoveVertice(4, new Vector2(0f, -4f));
            CoreMesh.MoveVertice(5, new Vector2(4f, 0f));
            CoreMesh.color = this.color;
            sLeaser.sprites[1] = CoreMesh;

            sLeaser.sprites[2] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["LightSource"],
                alpha = 0.5f
            };

            sLeaser.sprites[3] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["GravityDisruptor"],
                scale = 5f,
                alpha = 0f
            };

            this.AddToContainer(sLeaser, rCam, null);
        }

        //---------------- Overrides
        public override void Update(bool eu)
        {
            base.Update(eu);

            if (this.player != null && this.room != null && this.player.room == this.room)
            {
                var cinput = this.player.input[0];
                if (Plugin.meadowEnabled && this.AEC.isMeadowArenaTimerCountdown && !OnlineTimerOn())
                {
                    this.AEC.isMeadowArenaTimerCountdown = false;
                }
                if (this.AEC.active) {
                    SetCurrentState();
                    RepairUpdate();
                    RegenUpdate();
                    
                    if (this.AEC.IsBetaBoost ? cinput.jmp : cinput.spec)
                    {
                        if (this.ShouldZeroG && this.AEC.boostingCount <= 0)
                        {
                            if (this.AEC.antiGravityCount > 0 || this.AEC.coreBoostLeft > 0)
                            {
                                this.AEC.antiGravityCount++;
                            }
                        }
                        else if ((this.Landed || this.AEC.coreBoostLeft > 0) && this.AEC.waterCorrectionCount <= 0)
                        {
                            if (this.BoostAllowed)
                            {
                                this.AEC.boostingCount += this.AEC.IsBetaBoost && this.player.input[0].spec ? 3 : 1;
                                this.player.slowMovementStun = 10;
                                if (this.AEC.boostingCount > 400)
                                {
                                    if (this.AEC.isShockwaveEnabled) { ShockWave(true); }
                                    else { MeltdownStart(); }
                                }
                            }
                            else
                            {
                                this.AEC.boostingCount = -1;
                            }
                        }
                    }
                    else if (!this.ShouldZeroG && this.AEC.boostingCount > 0)
                    {
                        if (this.AEC.boostingCount < 5 && this.AEC.IsBetaBoost && this.player.canJump > 0)
                        {
                            this.AEC.boostingCount = -1;
                            this.player.Jump();
                        }
                        else
                        {
                            Boost(Mathf.Min(200, this.AEC.boostingCount), true);
                        }
                    }
                    AntiGravityUpdate();
                    OxygenUpdate();

                    if (this.AEC.energy < -this.AEC.CoreMeltdown)
                    {
                        Explode(true);
                    }
                }
            }
            else
            {
                Plugin.Log("Core " + this.ToString() + "/"+ this.abstractPhysicalObject.ID + " of " + this.player.ToString() + " is not in the same room as player ! Deleting...");
                this.AbstractEnergyCore.Abstractize(this.player != null ? this.player.abstractCreature.pos : this.AbstractEnergyCore.pos);
                this.AbstractEnergyCore.realizedObject = null;
                this.Destroy();
            }
        }
        public override void Grabbed(Creature.Grasp grasp)
        {
            grasp.Release();
        }
        public override void HitByWeapon(Weapon weapon)
        {
            // logger.LogDebug(weapon);
            // logger.LogDebug(weapon.mode);
            // logger.LogDebug(weapon.thrownBy);
            if (
                this.player != null &&
                !this.player.dead &&
                weapon.thrownBy != this.player
            )
            {
                Vector2 vector = Vector2.Lerp(weapon.firstChunk.lastPos, base.firstChunk.lastPos, 0.5f);
                Vector2 vector2 = this.player.mainBodyChunk.Rotation + Custom.DegToVec(UnityEngine.Random.value * 180f - 90f);
                weapon.WeaponDeflect(vector, vector2.normalized, this.firstChunk.vel.magnitude * 2 + weapon.firstChunk.vel.magnitude / 2);
                Disable();
            }
            else
            {
                Plugin.Log("The core has been hit by "+ weapon +" from "+ weapon.thrownBy +" but nothing happened !");
            }
        }

        //---------------- Variables

        // Objects
        public Color color = new(0.25f, 1f, 0.25f);
        public Player player;

        // Basic
        public float scale = 1f;
        public bool flipFromBoost = false;
        public bool oxygenCooldown = true;
        public int disableCooldown = 0;

        // Get - Set
        public Vector2 DirectionalInput
        {
            get
            {
                if (this.player != null)
                {
                    Player.InputPackage cinput = this.player.input[0];
                    bool isPC = cinput.controllerType == Options.ControlSetup.Preset.KeyboardSinglePlayer;
                    return isPC ? new Vector2(cinput.x, cinput.y).normalized : cinput.analogueDir.normalized;
                }
                return Vector2.zero;
            }
        }
        public Vector2Int IntDirectionalInput
        {
            get
            {
                int x = 0; int y = 0;
                Vector2 dirInput = this.DirectionalInput;
                if (dirInput.x > 0.2f) { x = 1; }
                else if (dirInput.x < -0.2f) { x = -1; }
                if (dirInput.y > 0.2f) { y = 1; }
                else if (dirInput.y < -0.2f) { y = -1; }
                return new Vector2Int(x, y);
            }
        }
        public AbstractEnergyCore AbstractEnergyCore
        {
            get
            {
                return (AbstractEnergyCore)this.abstractPhysicalObject;
            }
        }
        public AbstractEnergyCore AEC
        {
            get
            {
                return this.AbstractEnergyCore;
            }
        }
        public Vector2 PlayerMiddleSpritePos
        {
            get
            {
                if (this.player == null) { return Vector2.negativeInfinity; }
                if (BTWSkins.cwtPlayerSpriteInfo.TryGetValue(this.player.abstractCreature, out var fSprites) && fSprites.Count > 1)
                {
                    return fSprites[1].GetPosition() * 0.9f + fSprites[0].GetPosition() * 0.1f;
                }
                return Vector2.negativeInfinity;
            }
        }
        public bool ShouldZeroG
        {
            get
            {
                if (this.player == null) { return false; }
                return (this.player.animation == Player.AnimationIndex.Flip && !this.flipFromBoost) || Underwater || ZeroG;
            }
        }
        public bool Underwater
        {
            get
            {
                if (this.player == null) { return false; }
                return this.player.animation == Player.AnimationIndex.DeepSwim && this.player.submerged;
            }
        }
        public bool ZeroG
        {
            get
            {
                if (this.player == null) { return false; }
                return this.player.bodyMode == Player.BodyModeIndex.ZeroG;
            }
        }
        public bool Landed
        {
            get
            {
                if (this.player == null) { return false; }
                return this.player.canJump > 0 
                    || this.player.bodyMode == Player.BodyModeIndex.CorridorClimb 
                    || this.player.bodyMode == Player.BodyModeIndex.Swimming;
            }
        }
        public bool BoostAllowed
        {
            get
            {
                if (this.player != null)
                {
                    return this.player.animation != Player.AnimationIndex.GetUpOnBeam && 
                        this.player.animation != Player.AnimationIndex.GetUpToBeamTip && 
                        this.player.animation != Player.AnimationIndex.HangFromBeam;
                }
                return false;
            }
        }
    }
    // Functions
    private static bool OnlineTimerOn()
    {
        if (Plugin.meadowEnabled)
        {
            return MeadowCompat.ShouldHoldFireFromOnlineArenaTimer();
        }
        return false;
    }
    public static List<AbstractEnergyCore> GetAllAbstractCoreOfRoom(AbstractRoom abstractRoom)
    {
        List<AbstractEnergyCore> corelist = new();
        foreach (AbstractWorldEntity abstractObject in abstractRoom.entities)
        {
            if (abstractObject is AbstractEnergyCore abstractEnergyCore)
            {
                corelist.Add(abstractEnergyCore);
            }
        }
        return corelist;
    }
    public static AbstractEnergyCore GetAbstractEnergyCoreOfPlayer(AbstractCreature abstractPlayer)
    {
        if (abstractPlayer.Room != null)
        {
            return GetAllAbstractCoreOfRoom(abstractPlayer.Room).Find(x => x.abstractPlayer == abstractPlayer);
        }
        return null;
    }
    // Ma hooks
    private static float Interest_Exepection_Hook(On.PlayerGraphics.PlayerObjectLooker.orig_HowInterestingIsThisObject orig, PlayerGraphics.PlayerObjectLooker self, PhysicalObject obj)
    {
        if (obj is EnergyCore)
        {
            return 0f;
        }
        return orig(self, obj);
    }

    private static void Player_MoveAbstractCore(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
    {
        orig(self, pos, newRoom, spitOutAllSticks);
        if (CoreFunc.cwtCore.TryGetValue(self.abstractCreature, out var abstractEnergyCore))
        {
            Plugin.Log("Changing room of core of player " + self.ToString() + " !");
            if (abstractEnergyCore.realizedObject != null)
			{
                EnergyCore energyCore = abstractEnergyCore.RealizedCore;
                if (energyCore.room != self.room)
                {
                    abstractEnergyCore.Abstractize(self.abstractCreature.pos);
                    abstractEnergyCore.RealizeInRoom();
                }
			}
            else
            {
                abstractEnergyCore.Move(self.abstractCreature.pos);
                abstractEnergyCore.RealizeInRoom();
            }
        }
    }
}