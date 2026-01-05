using System.Collections.Generic;
using UnityEngine;
using System;
using RWCustom;
using System.Runtime.CompilerServices;
using BeyondTheWest.MeadowCompat;

namespace BeyondTheWest;
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
        BTWPlugin.Log($"Core ctor [{this}] done !");
        //this.evenUpdate = !player.evenUpdate;
        //logger.LogDebug(player.mainBodyChunk.owner);
        //logger.LogDebug(this.firstChunk.owner);
        //this.firstChunk.MoveWithOtherObject(true, player.mainBodyChunk, new Vector2(0.5f, -10f));
    }

    //----------------- Local functions
    byte GetCurrentState()
    {
        if (this.player != null)
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
    private void SetCoreMesh(RoomCamera.SpriteLeaser sLeaser)
    {
        if (sLeaser == null || sLeaser.sprites == null) { return; }
        TriangleMesh CoreMesh = (TriangleMesh)sLeaser.sprites[1];

        if (this.player != null && this.AEC != null && CoreMesh != null)
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

                        float meltRatioSQRT = (float)(Math.Sqrt(-energy) / Math.Sqrt(melt_energy));
                        float imeltRatioSQRT = 0.25f + 0.75f * (1 - meltRatioSQRT);

                        CoreMesh.MoveVertice(0, new Vector2(-2f - 2f * imeltRatioSQRT, -4f * imeltRatioSQRT));
                        CoreMesh.MoveVertice(1, new Vector2(-2f - 2f * imeltRatioSQRT, 4f * imeltRatioSQRT));
                        CoreMesh.MoveVertice(2, new Vector2(2f + 2f * imeltRatioSQRT, 4f * imeltRatioSQRT));

                        CoreMesh.MoveVertice(3, new Vector2(-2f - 2f * imeltRatioSQRT, -4f * imeltRatioSQRT));
                        CoreMesh.MoveVertice(4, new Vector2(2f + 2f * imeltRatioSQRT, 4f * imeltRatioSQRT));
                        CoreMesh.MoveVertice(5, new Vector2(2f + 2f * imeltRatioSQRT, -4f * imeltRatioSQRT));

                        if (this.grayScale > 0 && red > 0.95f) { this.grayScale = Mathf.Lerp(this.grayScale, 0f, 0.02f); }
                        break;
                    }
                default: // Dead or OFF
                    {
                        break;
                    }
            }
            this.color = Color.Lerp(this.color, this.gray, this.grayScale);
            if (this.player.dead)
            {
                CoreMesh.MoveVertice(0, new Vector2(-3f, 0f));
                CoreMesh.MoveVertice(1, new Vector2(0f, 3f));
                CoreMesh.MoveVertice(2, new Vector2(0f, -3f));

                CoreMesh.MoveVertice(3, new Vector2(0f, 3f));
                CoreMesh.MoveVertice(4, new Vector2(0f, -3f));
                CoreMesh.MoveVertice(5, new Vector2(3f, 0f));

                if (this.grayScale < 0.99f)
                {
                    this.grayScale = Mathf.Lerp(this.grayScale, 1f, 0.005f);
                    // Plugin.Log($"Core of player [{this.player}] grayscale at [{this.grayScale}]. Color at <{this.color}>");
                }
                else
                {
                    this.grayScale = 1f;
                }  
            }
            sLeaser.sprites[1] = CoreMesh;
        }
    }
    private void StateSyncFakePlayer()
    {
        if (this.player != null && this.AEC != null && this.room != null && this.player.room == this.room && !this.AEC.active && this.AEC.isMeadowFakePlayer)
        {
            byte state = this.AEC.state;
            if (state == 5 && this.player.animation == Player.AnimationIndex.Flip)
            {
                foreach (var b in this.player.bodyChunks)
                {
                    b.vel.y += this.player.gravity * this.AEC.CoreAntiGravity;
                }
            }
            else if (state == 7)
            {
                this.player.airInLungs = 0.85f;
            }
        }
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
                MeadowCalls.CoreMeadow_DisableRPC(this.AEC);
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
            bool chargedJump = false;
            bool slideUpBoost = false;
            var pos = this.firstChunk.pos;
            Vector2 inputDir = this.DirectionalInput;
            Vector2 intInput = this.IntDirectionalInput;

            this.canSlam = false;

            if (isReal && (this.Landed || (player.superLaunchJump >= 20 && BTWFunc.CanSuperJump(this.player))))
            {
                boostJump = true;
                if (player.superLaunchJump >= 20 && BTWFunc.CanSuperJump(this.player))
                {
                    chargedJump = true;
                    this.player.superLaunchJump = (int)pow;
                    this.allowJumpException = true;
                    this.player.Jump();
                    this.allowJumpException = false;
                }
                else if (this.player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
                {
                    if (intInput.y == -1)
                    {
                        boostJump = false;
                        this.player.animation = Player.AnimationIndex.None;
                    }
                    else if (this.player.animation == Player.AnimationIndex.ClimbOnBeam && intInput.y == 1 && this.player.slideUpPole < 1)
                    {
                        slideUpBoost = true;
                        this.player.slideUpPole = (int)Mathf.Min(pow / 4 + 10, 60);
                        this.player.Blink(this.player.slideUpPole / 3);
                        this.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, this.player.mainBodyChunk, false, 0.8f, 1f);
                    }
                    else
                    {
                        this.allowJumpException = true;
                        this.player.Jump();
                        this.allowJumpException = false;
                        this.player.animation = Player.AnimationIndex.None;
                    }
                }
                else
                {
                    this.allowJumpException = true;
                    this.player.Jump();
                    this.allowJumpException = false;
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

                if (chargedJump)
                {
                    foreach (var b in this.player.bodyChunks)
                    {
                        b.vel.x *= 1 + Mathf.Pow(pow / 200f, 2f);
                        b.vel.y *= Mathf.Max(pow / 75f, 1);
                    }
                }
                else
                {
                    Vector2 baseBoost = Vector2.zero;
                    Vector2 scaleBoost = Vector2.zero;
                    float diagonalPenalty;
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
                    if (!boostJump && intInput.y == -1 && pow > 40)
                    {
                        this.canSlam = true;
                    }
                }
                
                this.AEC.boostingCount = -15;
            }

            if (this.AEC.energy > 0)
            {
                float powPenalty = 3.5f;
                if (boostJump) { powPenalty = 2.5f; }
                if (slideUpBoost) { powPenalty = 2f; }
                if (chargedJump) { powPenalty = 1.5f; }
                this.AEC.energy -= Mathf.Clamp(pow * powPenalty, 50f, 1000f);
                if (this.AEC.energy <= 0) { this.AEC.energy = -1; MeltdownStart(); }
            }
            else
            {
                this.AEC.energy -= 100f;
            }
            
            if (isReal && this.AEC.active && this.AEC.isMeadow)
            {
                byte reducedPow = (byte)Mathf.Clamp(pow, 0, byte.MaxValue);
                MeadowCalls.CoreMeadow_BoostRPC(this.AEC, reducedPow);
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
                MeadowCalls.CoreMeadow_ShockwaveRPC(this.AEC);
            }
        }
    }
    public void Explode(bool isReal = true)
    {
        if (this.player != null)
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
                MeadowCalls.CoreMeadow_ExplodeRPC(this.AEC);
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
                MeadowCalls.CoreMeadow_PopRPC(this.AEC);
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
                    b.vel *= 0.85f;
                }
            }
            else if (ShouldZeroG && this.AEC.boostingCount < 5 && triggered && (this.AEC.energy > 0f))
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
                    this.AEC.energy -= (Underwater ? this.AEC.Core0GWaterEnergyUsage : this.AEC.Core0GSpaceEnergyUsage) / BTWFunc.FrameRate;
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
            if (this.player.room != null && (this.player.airInLungs <= 0.85f || this.player.dead) && this.AEC.energy > 100f && this.oxygenCooldown)
            {
                List<RadiusCheckResultObject> creatureList = 
                    BTWFunc.GetAllCreatureInRadius(this.player.room, this.player.mainBodyChunk.pos, this.OxygenRange * 10f);
                bool creatureFound = false;
                foreach (RadiusCheckResultObject resultCreature in creatureList)
                {
                    if (resultCreature.physicalObject is Player otherplayer
                        && (otherplayer.abstractPhysicalObject.rippleBothSides || this.player.abstractPhysicalObject.rippleBothSides || this.player.abstractPhysicalObject.rippleLayer == otherplayer.abstractPhysicalObject.rippleLayer)
                        && otherplayer.airInLungs <= 0.85f
                        && !otherplayer.dead)
                    {
                        creatureFound = true;
                        this.AEC.energy -= this.AEC.CoreOxygenEnergyUsage * Mathf.Max(0f, 0.85f - otherplayer.airInLungs);
                        otherplayer.airInLungs = 0.85f;

                        if (this.AEC.isMeadow && this.AEC.active && otherplayer != this.player)
                        {
                            MeadowCalls.CoreMeadow_OxygenGiveRPC(this.AEC, otherplayer);
                        }
                    }
                }
                if (creatureFound)
                {
                    this.AEC.oxygenCount++;
                    if (this.AEC.energy <= 100f)
                    {
                        this.oxygenCooldown = false;
                        this.room?.PlaySound(SoundID.Death_Lightning_Spark_Object, this.firstChunk.pos, 0.75f, 0.75f);
                    }
                }
            }
            else if (this.AEC.oxygenCount > 0 && (this.player.airInLungs > 0.95f || this.player.dead) && this.AEC.energy > 100f)
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
                this.room.AddObject(new Spark(this.firstChunk.pos, BTWFunc.RandomCircleVector(10f), Color.red, null, 3, 10));
                this.room.PlaySound(SoundID.Centipede_Shock, this.firstChunk.pos, 0.15f, UnityEngine.Random.Range(0.75f, 0.9f));
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
                this.canSlam = false;
                if (!this.Underwater && !this.ZeroG) { this.AEC.antiGravityCount = 0; }
                this.AEC.coreBoostLeft = this.AEC.CoreMaxBoost;
                this.flipFromBoost = false;
            }

            // if (player.superLaunchJump > 0)
            // {
            //     this.AEC.boostingCount = -20;
            // }
        }
    }
    public void RepairUpdate()
    {
        if (this.player != null && !this.player.dead && this.AEC.energy <= 0f && this.room != null)
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
    private void DebugUpdate()
    {
        Player player = this.player;
        Room room = this.room;
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
                    this.AEC.energy = 0;
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    this.AEC.energy = -this.AEC.CoreMeltdown;
                }
                else
                {
                    this.AEC.energy = this.AEC.CoreMaxEnergy;
                }
            }
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
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
        if (this.slatedForDeletetion || this.room != rCam.room || this.AEC == null)
        {
            sLeaser.CleanSpritesAndRemove();
            return;
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
            Vector2 coreSpritePos = (PlayerMiddleSpritePos != null ?
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
                sLeaser.sprites[3].scale = Mathf.Lerp(sLeaser.sprites[3].scale, OxygenRange, 0.05f);
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

        if (this.player != null && this.AEC != null && this.room != null && this.player.room == this.room)
        {
            if (this.room.game.devToolsActive) { DebugUpdate(); }
            var cinput = this.player.input[0];
            if (BTWPlugin.meadowEnabled && this.AEC.isMeadowArenaTimerCountdown && !BTWFunc.OnlineArenaTimerOn())
            {
                this.AEC.isMeadowArenaTimerCountdown = false;
            }
            if (!this.AEC.active && this.AEC.isMeadowFakePlayer)
            {
                StateSyncFakePlayer();
            }
            if (this.consideredDead && !this.player.dead)
            {
                this.consideredDead = false;
                this.grayScale = 0f;
                BTWFunc.ResetCore(this.player);
            }
            else if (this.AEC.active) {

                SetCurrentState();
                RepairUpdate();
                RegenUpdate();

                if (!this.player.dead)
                {
                    if (this.AEC.IsBetaBoost ? cinput.jmp : cinput.spec)
                    {
                        if (this.ShouldZeroG && this.AEC.boostingCount < 5)
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
                                this.AEC.boostingCount += (this.AEC.IsBetaBoost && this.player.input[0].spec ? 3 : 1) 
                                    * (player.superLaunchJump > 0 && BTWFunc.CanSuperJump(this.player) ? 2 : 1);
                                this.player.slowMovementStun = 10;
                                if (this.AEC.boostingCount > 400)
                                {
                                    if (this.AEC.isShockwaveEnabled) { ShockWave(true); }
                                    else { MeltdownStart(); this.AEC.boostingCount = 0; }
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
                            this.AEC.boostingCount = -20;
                            this.allowJumpException = true;
                            this.player.Jump();
                            this.allowJumpException = false;
                        }
                        else
                        {
                            Boost(Mathf.Min(200, this.AEC.boostingCount), true);
                        }
                    }
                }
                AntiGravityUpdate();
                OxygenUpdate();

                if (this.AEC.energy < -this.AEC.CoreMeltdown)
                {
                    Explode(true);
                }
                if (this.canSlam && this.player.mainBodyChunk.vel.y >= 0)
                {
                    this.canSlam = false;
                }
            }
        }
        else
        {
            BTWPlugin.Log($"[{this}] is not in the same room as player ! AEC : [{this.AEC}], Room : [{this.room}], Player Room : [{this.player?.room}]. Deleting...");
            this.AbstractEnergyCore.Abstractize(this.player != null ? this.player.abstractCreature.pos : this.AbstractEnergyCore.pos);
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
            BTWPlugin.Log("The core has been hit by "+ weapon +" from "+ weapon.thrownBy +" but nothing happened !");
        }
    }

    public override string ToString()
    {
        return $"EnergyCore <{this.abstractPhysicalObject?.ID}> of [{this.player}]";
    }
    
    //---------------- Variables

    // Objects
    public Color color = new(0.25f, 1f, 0.25f);
    public Color gray = new(0.25f, 0.25f, 0.25f);
    public Player player;

    // Basic
    public float scale = 1f;
    public float grayScale = 0f;
    public bool flipFromBoost = false;
    public bool canSlam = false;
    public bool oxygenCooldown = true;
    public bool consideredDead = false;
    public bool allowJumpException = false;
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
                    // this.player.animation != Player.AnimationIndex.GetUpToBeamTip && 
                    this.player.animation != Player.AnimationIndex.HangFromBeam;
            }
            return false;
        }
    }
    public float OxygenRange
    {
        get
        {
            if (this.AEC != null)
            {
                float eRatio = Mathf.Clamp01(this.AEC.energy / this.AEC.CoreMaxEnergy);
                return 4f + 12f * eRatio;
            }
            return 0f;
        }
    }
}
