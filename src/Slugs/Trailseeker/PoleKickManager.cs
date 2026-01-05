using UnityEngine;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using BeyondTheWest.MSCCompat;
using System.Collections.Generic;
using System;

namespace BeyondTheWest;

public class PoleKickManager : AdditionnalTechManager<PoleKickManager>
{
    public static void AddManager(AbstractCreature creature, out PoleKickManager PKM)
    {
        RemoveManager(creature);
        PKM = new(creature);
        managers.Add(creature, PKM);
    }
    public static void AddManager(AbstractCreature creature)
    {
        AddManager(creature, out _);
    }
    
    public PoleKickManager(AbstractCreature abstractCreature, bool isFake = false) : base(abstractCreature)
    {
        this.isFake = isFake;
        if (BTWPlugin.meadowEnabled)
        {
            MeadowCalls.PoleKickManager_Init(this);
        }
        // this.debugEnabled = true;
    }
    
    // ------ Local Functions
    private IntVector2 GetTileIntPos(uint bodyIndex, IntVector2 offset)
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null && player.bodyChunks.Length > bodyIndex)
        {
            return room.GetTilePosition(player.bodyChunks[bodyIndex].pos) + offset;
        }
        return new IntVector2(0,0);
    }
    private IntVector2 GetTileIntPos(uint bodyIndex)
    {
        return GetTileIntPos(bodyIndex, new IntVector2(0,0));
    }
    private Vector2 GetTilePos(uint bodyIndex, IntVector2 offset)
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null && player.bodyChunks.Length > bodyIndex)
        {
            return room.MiddleOfTile(GetTileIntPos(bodyIndex, offset));
        }
        return Vector2.zero;
    }
    private Vector2 GetTilePos(uint bodyIndex)
    {
        return GetTilePos(bodyIndex, new IntVector2(0,0));
    }
    private bool IsTileBeam(uint bodyIndex, IntVector2 offset, out float distance)
    {
        distance = float.PositiveInfinity;
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null && player.bodyChunks.Length > bodyIndex)
        {
            IntVector2 tileIntPos = GetTileIntPos(bodyIndex, offset);
            Vector2 tilePos = GetTilePos(bodyIndex, offset);
            distance = Mathf.Abs(tilePos.magnitude - player.bodyChunks[bodyIndex].pos.magnitude);
            return room.GetTile(tileIntPos).verticalBeam;
        }
        return false;
    }
    private bool IsTileBeam(uint bodyIndex, IntVector2 offset)
    {
        return IsTileBeam(bodyIndex, offset, out _);
    }
    private bool IsTileBeam(uint bodyIndex, out float distance)
    {
        return IsTileBeam(bodyIndex, new IntVector2(0,0), out distance);
    }
    private bool IsTileBeam(uint bodyIndex)
    {
        return IsTileBeam(bodyIndex, out _);
    }

    private void InitPolePounce()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            this.isPolePounce = true;
            player.wantToJump = 0;
            ResetPlayerCustomStates();
            // Plugin.Log("Pole Pounce Init !");

            int direction = this.MovementDirection;
            room.PlaySound(SoundID.Slugcat_From_Horizontal_Pole_Jump, player.mainBodyChunk, false, 1.0f, 1.5f);
            room.PlaySound(SoundID.Slugcat_Rocket_Jump, player.mainBodyChunk, false, 0.75f, 1.25f);
			player.animation = Player.AnimationIndex.RocketJump;

            Vector2 boost = new (direction * -8f, 11f);
            player.jumpStun = 15 * -direction;

			if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                boost.y += 7f;
                boost.x += 5f * -direction;
                player.jumpStun = 10 * -direction;
            }
            if (ModManager.MSC && player.isSlugpup)
            {
                boost.y /= 2f;
                boost.x -= 5f;
            }
			player.bodyChunks[1].pos = player.bodyChunks[0].pos;
            player.bodyChunks[0].pos += boost.normalized * 10f;
            
            foreach (BodyChunk chunk in player.bodyChunks)
            {
                chunk.vel = boost;
            }
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                MTM.ApplyJumpTechBoost();
            }
            this.poleTechCooldown.Reset(7);

			for (int i = 0; i < 5; i++)
			{
				player.room.AddObject(
                    new WaterDrip(
                        player.mainBodyChunk.pos + new Vector2(player.mainBodyChunk.rad * direction, 0f), 
                        new Vector2(player.mainBodyChunk.rad * direction, 0f) + BTWFunc.RandomCircleVector(player.mainBodyChunk.rad),
                        false));
			}
        }
    }
    private void InitPoleHop()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            this.isPolePounce = true;
            player.wantToJump = 0;
            ResetPlayerCustomStates();
            // Plugin.Log("Pole Hop Init !");

            int direction = this.MovementDirection;
            room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, player.mainBodyChunk, false, 1.5f, BTWFunc.Random(1.3f,1.6f));
            room.PlaySound(SoundID.Slugcat_Super_Jump, player.mainBodyChunk, false, 0.85f, BTWFunc.Random(1.1f,1.3f));
			player.animation = Player.AnimationIndex.RocketJump;

            Vector2 boost = new (direction * 9f, 7.5f);
            if (!this.poleLoopExitTick.ended)
            {
                boost += new Vector2(direction * 1f, 0.5f) * Mathf.Min(5, this.poleLoopCount.value);
                if (this.poleLoopCount.value > 2)
                {
                    player.animation = Player.AnimationIndex.Flip;
                    boost.y += 4f;
                    boost.x -= 7f * direction;
                }
                this.poleLoopExitTick.ResetUp();
            }
			if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                boost.y += 5f;
                boost.x += 10f * direction;
            }
            if (ModManager.MSC && player.isSlugpup)
            {
                boost.y -= 3f;
                boost.x /= 2f;
            }
            if (player.bodyChunks[1].vel.x * direction > boost.x * direction)
            {
                boost.x += 3f * direction;
            }
            if (player.bodyChunks[1].vel.y > boost.y)
            {
                boost.y = player.bodyChunks[1].vel.y;
            }

			player.bodyChunks[1].pos = player.bodyChunks[0].pos;
            player.bodyChunks[0].pos += boost.normalized * 10f;
            player.jumpStun = 5 * (int)Mathf.Sign(boost.x);
            
            foreach (BodyChunk chunk in player.bodyChunks)
            {
                chunk.vel = boost;
            }
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                MTM.ApplyJumpTechBoost();
            }
            this.poleTechCooldown.Reset(4);

			for (int i = 0; i < 3; i++)
			{
				player.room.AddObject(
                    new WaterDrip(
                        player.mainBodyChunk.pos + new Vector2(player.mainBodyChunk.rad * direction, 0f), 
                        new Vector2(player.mainBodyChunk.rad * direction, 0f) + BTWFunc.RandomCircleVector(player.mainBodyChunk.rad),
                        false));
			}
        }
    }
    private void InitKick(BodyChunk chuckHit)
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        Creature target = chuckHit.owner as Creature;
        if (player != null && room != null && target != null)
        {
            player.wantToJump = 0;
            ResetPlayerCustomStates();

            Vector2 boost = (((player.bodyChunks[0].pos + player.bodyChunks[1].pos) / 2) - chuckHit.pos).normalized * 6f;
            bool flippin = boost.normalized.y > 0.9f && !this.kickExhausted;
            boost.y += 6f;

			if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                boost *= 2;
            }
            if (ModManager.MSC && player.isSlugpup)
            {
                boost.y /= 3f;
                boost.x /= 2f;
            }
            if (this.kickExhausted)
            {
                boost /= 1.5f;
            }

            if (flippin)
            {
                player.animation = Player.AnimationIndex.Flip;
                player.flipFromSlide = true;
                flippin = true;
                boost.x *= -1;
                kickExhaustCount.Add(BTWFunc.FrameRate * 7);
            }
            else
            {
                player.animation = Player.AnimationIndex.RocketJump;
                player.jumpStun = 12 * (int)Mathf.Sign(boost.x);
                kickExhaustCount.Add(BTWFunc.FrameRate * 5);
            }
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                if (flippin)
                {
                    boost.x *= MTM.flipScalarVel.x;
                    boost.y *= MTM.flipScalarVel.y;
                }
                else
                {
                    boost.x *= MTM.othersScalarVel.x;
                    boost.y *= MTM.othersScalarVel.y;
                }
            }

            Vector2 knockback = -boost * (flippin ? 2f : 5f);
            knockback.y /= flippin ? 0.75f : 4f;
            float weightRatio = player.TotalMass / target.TotalMass;
            float knockbackBonus = Mathf.Clamp(Mathf.Log(weightRatio, 8), -1, 1) + 1;
            boost *= Mathf.Clamp(Mathf.Pow(2 - knockbackBonus, 4), 0.25f, 1.25f);

            player.bodyChunks[1].pos = player.bodyChunks[0].pos;
            player.bodyChunks[0].pos += boost.normalized * 10f;
            foreach (BodyChunk chunk in player.bodyChunks)
            {
                chunk.vel = boost;
            }
            this.poleTechCooldown.Reset(9);
            room.PlaySound(SoundID.Slugcat_Rocket_Jump, player.mainBodyChunk, false, 1.25f, BTWFunc.Random(1.3f,1.5f));
            
            BTWPlugin.Log($"WOW ! [{player}] kicked [{target}] with a knockback of [{knockback}] and a boost of [{boost}] !");
            
            HitCreatureWithKick(player, chuckHit, knockback, knockbackBonus);
        }
    }
    public static void HitCreatureWithKick(Player kicker, BodyChunk chuckHit, Vector2 knockback, float knockbackBonus)
    {
        Creature target = chuckHit.owner as Creature;
        Room room = target.room;
        if (room != null && target != null)
        {
            bool exhausted = kicker.exhausted || (PoleKickManager.TryGetManager(kicker.abstractCreature, out var PKM) && PKM.kickExhausted);
            if (BTWFunc.IsLocal(target.abstractCreature) && !(BTWPlugin.meadowEnabled && MeadowFunc.ShouldHoldFireFromOnlineArenaTimer()))
            {
                BTWFunc.CustomKnockback(chuckHit, knockback);
                
                float dmg = 0.15f;
                float stun = 0.65f * BTWFunc.FrameRate * (1 + knockbackBonus);
                if (knockbackBonus == 0)
                {
                    dmg = 0.1f;
                    stun = 0f;
                }
                else if (knockbackBonus == 2)
                {
                    dmg = 0.35f;
                    stun *= 2;
                }
                if (exhausted)
                {
                    dmg /= 5;
                    stun /= 3;
                }
                target.Violence(
                    kicker.bodyChunks[1], 
                    knockback.normalized,
                    chuckHit,
                    null, Creature.DamageType.Blunt,
                    dmg, stun
                );
                if (BTWPlugin.meadowEnabled)
                {
                    ArenaDeathTracker.SetDeathTrackerOfCreature(target.abstractCreature, BTWFunc.random > 0.25 ? 25 : 26);
                }
                if (ModManager.MSC && target is Player player)
                {
                    player.playerState.permanentDamageTracking += dmg / player.Template.baseDamageResistance;
                    if (player.playerState.permanentDamageTracking >= 1.0)
                    {
                        player.Die();
                    }
                }
                BTWPlugin.Log($"Kick of [{kicker}] on [{target}] dealt <{dmg}> damage and <{stun}> stun ! (WR of <{knockbackBonus}>) !");
                
            }
            if (BTWPlugin.meadowEnabled && BTWFunc.IsLocal(kicker.abstractCreature) && MeadowFunc.IsMeadowLobby())
            {
                MeadowCalls.PoleKickManager_RPCKick(kicker, chuckHit, knockback, knockbackBonus);
            }

            if ((target.State is HealthState && (target.State as HealthState).ClampedHealth == 0f) || target.State.dead)
            {
                room.PlaySound(SoundID.Spear_Stick_In_Creature, kicker.mainBodyChunk, false, exhausted? 1.1f : 1.5f, BTWFunc.Random(0.95f, 1.1f));
            }
            else
            {
                room.PlaySound(SoundID.Rock_Hit_Creature, kicker.mainBodyChunk, false, exhausted? 0.55f : 0.75f, BTWFunc.Random(1.0f, 1.1f));
            }
            room.PlaySound(SoundID.Rock_Hit_Creature, kicker.mainBodyChunk, false, exhausted? 1f : 1.25f, BTWFunc.Random(1.5f, 1.6f));
            room.PlaySound(SoundID.Slugcat_Jump_On_Creature, kicker.mainBodyChunk, false, exhausted? knockbackBonus/2f : knockbackBonus, BTWFunc.Random(1.2f, 1.3f));
			room.AddObject(
                new ExplosionSpikes(
                    room, 
                    chuckHit.pos,
                    5, 10f, 20f, 7.5f, 50f, new Color(1f, 1f, 1f, 0.5f)));
        }
    }
    
    private void InitPoleLoop(int poleLoopTileX)
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            ResetPlayerCustomStates();
            if (this.lastPoleLoopTileX == poleLoopTileX)
            {
                if (this.poleLoopCount.reachedMax) { BTWPlugin.Log("Maxuimum Pole Loop reached !"); return; }
                this.poleLoopCount.Add();
            }
            else
            {
                this.poleLoopCount.Reset();
                this.poleLoopCount.Add();
                this.lastPoleLoopTileX = poleLoopTileX;
            }
            // Plugin.Log("Pole Loop Init !");
            this.poleLoopTick.Reset();
            this.poleLoopExitTick.Reset();
            this.poleLoop = true;
            this.poleLoopDir = -this.MovementDirection;
            this.lastPoleLoopY = player.bodyChunks[0].pos.y;

            this.nextLoopBuffered = false;
            this.slideUpLoopBuffered = false;
            this.jumpLoopBuffered = false;

            room.PlaySound(SoundID.Slugcat_From_Horizontal_Pole_Jump, player.mainBodyChunk, false, 2.5f, 1.15f);
			player.animation = Player.AnimationIndex.RocketJump;
            player.rollDirection = 0;
            
            // player.bodyChunks[0].pos.x = room.MiddleOfTile(new IntVector2(poleLoopTileX, 0)).x;

			player.room.AddObject(
                new ExplosionSpikes(
                    room, 
                    player.bodyChunks[1].pos + new Vector2(0f, -player.bodyChunks[1].rad),
                    9, 5f, 5f, 3.5f, 20f, new Color(1f, 1f, 1f, 0.5f)));
        }
    }
    private void InitPoleLoop(int poleLoopTileX, int dir)
    {
        InitPoleLoop(poleLoopTileX);
        this.poleLoopDir = dir;
    }
    private void UpdatePoolLoop()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            this.poleLoopTick.Tick();
            float heightGained = 40f - (this.poleLoopCount.value - 1) * 5f;
            float loopLenght = 22.5f;
            float smoothing = 0.65f;
            float XposTile = room.MiddleOfTile(new IntVector2(this.lastPoleLoopTileX, 0)).x;
            if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                heightGained += 30f;
                loopLenght = 27.5f;
                smoothing = 0.35f;
            }
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                heightGained *= MTM.othersScalarVel.y;
            }
            if (ModManager.MSC && player.isSlugpup)
            {
                heightGained /= 1.5f;
            }
            if (heightGained < 10f)
            {
                heightGained = 10f;
            }
            Vector2 idealPos0 = new(
                XposTile + loopLenght * Mathf.Cos(this.poleLoopTick.fract * Mathf.PI - Mathf.PI/6) * -this.poleLoopDir, 
                this.lastPoleLoopY + this.poleLoopTick.fract * heightGained);
            Vector2 idealPos1 = new(
                XposTile + loopLenght * Mathf.Cos((this.poleLoopTick.fract - 0.5f) * Mathf.PI - Mathf.PI/6) * -this.poleLoopDir, 
                this.lastPoleLoopY + (this.poleLoopTick.fract - 0.5f) * heightGained);

            // Plugin.Log($"Player at [{player.bodyChunks[0].pos}], ideal pos at[{idealPos0}], diff is [{idealPos0 - player.bodyChunks[0].pos}], tick at <{this.poleLoopTick.value}>, fract at <{this.poleLoopTick.fract}>");
            player.bodyChunks[0].vel = (idealPos0 - player.bodyChunks[0].pos) * smoothing;
            player.bodyChunks[1].vel = (idealPos1 - player.bodyChunks[1].pos) * smoothing;
            // player.bodyChunks[1].pos = idealPos1;
            // player.bodyChunks[0].pos = idealPos0;

            if (AbstractEnergyCore.TryGetCore(player.abstractCreature, out var core))
            {
                core.boostingCount = -5;
            }
            IntVector2 intinput = this.IntDirectionalInput;
            if (intinput.x == -this.poleLoopDir && intinput.y == -1)
            {
                this.nextLoopBuffered = true;
            }
            if (intinput.y == 1)
            {
                this.slideUpLoopBuffered = true;
            }
            if (player.input[0].jmp)
            {
                this.jumpLoopBuffered = true;
            }

            if (this.poleLoopTick.ended)
            {
                // Plugin.Log($"Pole loop ended !")

                IntVector2 PoleTile = GetTileIntPos(0);
                PoleTile.x = this.lastPoleLoopTileX;

                if (this.slideUpLoopBuffered && room.GetTile(PoleTile).verticalBeam) // end loop sliding up the pole
                {
                    EndPoleLoopSlideUp();
                }
                else if (this.jumpLoopBuffered) // loop jump buffered
                {
                    this.poleLoop = false;
                    this.poleLoopDir = 0;
                    InitPoleHop();
                }
                else if (this.nextLoopBuffered && room.GetTile(PoleTile).verticalBeam) // loop buffered
                {
                    this.poleLoop = false;
                    InitPoleLoop(this.lastPoleLoopTileX, -this.poleLoopDir);
                }
                else 
                {
                    PoleTile.y += 1;
                    if (!room.GetTile(PoleTile).verticalBeam) // loop exit with a flip !
                    {
                        EndPoleLoopFlipExit();
                    }
                    else // normal loop end
                    {
                        EndPoleLoopNormal();
                    }
                }
            }
        }
    }
    private void EndPoleLoopFlipExit()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            BTWPlugin.Log($"Pole loop flip exit !");

            Vector2 boost = new (this.poleLoopDir * 5f, 10f);
            if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                boost.y += 10f;
                boost.x += this.poleLoopDir * 3f;
            }
            if (ModManager.MSC && player.isSlugpup)
            {
                boost.y /= 2f;
                boost.x /= 2f;
            }
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                boost.y *= MTM.flipScalarVel.y;
                boost.x *= MTM.flipScalarVel.x;
            }
            foreach (BodyChunk chunk in player.bodyChunks)
            {
                chunk.vel = boost;
            }
            
            player.animation = Player.AnimationIndex.Flip;
            room.PlaySound(SoundID.Slugcat_Flip_Jump, player.mainBodyChunk, false, 1f, 1.25f);
            player.room.AddObject(
                new ExplosionSpikes(
                    room, 
                    player.bodyChunks[1].pos + new Vector2(0f, -player.bodyChunks[1].rad),
                    7, 10f, 5f, 7.5f, 30f, new Color(1f, 1f, 1f, 0.5f)));

            this.poleLoop = false;
            this.poleLoopDir = 0;
            this.poleLoopExitTick.ResetUp();
        }
    }
    private void EndPoleLoopNormal()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            Vector2 boost = new (this.poleLoopDir * 6f, 2f);
            if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                boost.y += 2f;
                boost.x += this.poleLoopDir * 4f;
            }
            if (ModManager.MSC && player.isSlugpup)
            {
                boost.y /= 2f;
                boost.x /= 2f;
            }
            foreach (BodyChunk chunk in player.bodyChunks)
            {
                chunk.vel = boost;
            }
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                MTM.ApplyJumpTechBoost();
            }

            this.poleLoop = false;
            this.poleLoopDir = 0;
        }
    }
    private void EndPoleLoopSlideUp()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            int bonusSlideUp = 0;
            IntVector2 PoleTile = GetTileIntPos(0);
            PoleTile.x = this.lastPoleLoopTileX;

            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                bonusSlideUp = MTM.poleBonus;
            }
            player.slideUpPole = (int)((20 + bonusSlideUp) * this.poleLoopCount.fractInv);

            player.bodyChunks[0].pos = room.MiddleOfTile(PoleTile);
            player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
            if (room.GetTile(PoleTile).verticalBeam)
            {
                player.animation = Player.AnimationIndex.ClimbOnBeam;
                player.GrabVerticalPole();
            }
            if (player.bodyChunks[1].pos.y > player.mainBodyChunk.pos.y)
            {
                float addedSpin = (player.bodyChunks[1].pos.x < player.mainBodyChunk.pos.x) ? -2f : 2f;
                player.bodyChunks[1].vel.x += addedSpin;
                player.bodyChunks[1].pos.x += addedSpin;
            }
            player.dropGrabTile = null;
            CancelPoolLoop();
        }
    }
    public void CancelPoolLoop()
    {
        this.poleLoopCount.Reset();
        this.poleLoopTick.Reset();
        this.poleLoopExitTick.ResetUp();
        this.poleLoopDir = 0;
        this.lastPoleLoopTileX = -1;
        this.poleLoop = false;
        this.nextLoopBuffered = false;
        this.slideUpLoopBuffered = false;
        this.jumpLoopBuffered = false;
    }
    
    // ------ Public Funcitions
    
    public void AnimationUpdate()
    {
        Player player = this.RealizedPlayer;
        if (player != null)
        {
            bool flipping = player.animation == Player.AnimationIndex.Flip;
            bool leaping = player.animation == Player.AnimationIndex.RocketJump;
            if ((leaping || flipping) && this.HoldToPoles)
            {
                this.kickKaizo.ResetUp();
                // Plugin.Log($"<{intinput.y}>/<{intinput.x}>, <{dir}>, <{jumpHeld}>/<{jumpPressed}>, <{this.polePounceKaizoTick.valueDown}>/<{player.wantToJump}>, <{IsTileBeam(0)}>/<{IsTileBeam(1)}>, <{player.jumpStun * dir}>");
                if (this.poleLoop)
                {
                    if (this.IntDirectionalInput.y > 0)
                    {
                        BTWPlugin.Log($"Pole loop canceled because player held upward !");
                        EndPoleLoopSlideUp();
                    }
                    else
                    {
                        foreach (var chuck in player.bodyChunks)
                        {
                            IntVector2 contact = chuck.ContactPoint;
                            if (contact.x != 0 || contact.y != 0)
                            {
                                CancelPoolLoop();
                                player.animation = Player.AnimationIndex.None;
                                BTWPlugin.Log($"Pole loop canceled because [{chuck}] has contact point [{contact}] !");
                            }
                        }
                    }
                }
                else
                {
                    this.poleLoopExitTick.Tick();
                }
                if (!this.poleLoopExitTick.ended)
                {
                    this.bodyInFrontOfPole = this.poleLoopCount.value%2 == 1;
                }
                else
                {
                    this.bodyInFrontOfPole = false;
                }
            }
            else if (!(leaping || flipping))
            {
                if (this.isPolePounce && player.jumpStun != 0)
                {
                    player.jumpStun = 0;
                }
                this.isPolePounce = false;
                this.bodyInFrontOfPole = false;
                if (this.IntDirectionalInput.y > 0 && this.poleLoop)
                {
                    BTWPlugin.Log($"Pole loop canceled because player held upward ! Detected after cancelling.");
                    EndPoleLoopSlideUp();
                }
                CancelPoolLoop();
                this.kickKaizo.Down();
            }

            this.kickExhaustCount.Down();
            this.poleTechCooldown.Tick();
            if (this.kickExhaustCount.reachedMax)
            {
                this.kickExhausted = true;
            }
            if (this.kickExhausted)
            {
                player.Blink(5);
                player.slowMovementStun = 5;
                this.kickExhaustCount.Down();
            }
            if (this.kickExhausted && this.kickExhaustCount.atZero)
            {
                this.kickExhausted = false;
            }
            if (this.Landed)
            {
                this.kickExhaustCount.Down();
                this.kickKaizo.Reset();
                if (this.isPolePounce && player.jumpStun != 0)
                {
                    player.jumpStun = 0;
                }
            }
        }
        else
        {
            this.isPolePounce = false;
            this.poleTechCooldown.ResetUp();
            this.bodyInFrontOfPole = false;
            this.kickKaizo.Reset();
            CancelPoolLoop();
        }
    }
    public override void Update()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null && !this.isFake)
        {
            AnimationUpdate();
            int dir = this.MovementDirection;
            float speed = Mathf.Abs((player.bodyChunks[0].vel + player.bodyChunks[1].vel).magnitude / 2);
            IntVector2 intinput = this.IntDirectionalInput;
            bool jumpHeld = player.input[0].jmp;
            bool jumpPressed = jumpHeld && !player.input[1].jmp;
            bool flipping = player.animation == Player.AnimationIndex.Flip;
            bool leaping = player.animation == Player.AnimationIndex.RocketJump;
            bool superJump = BTWPlayerData.TryGetManager(abstractPlayer, out var BTWData) && BTWData.isSuperLaunchJump;
            this.debugCircleVisible = false;

            // this.bodyInFrontOfPole = player.input[0].spec;
            
            if (this.poleLoop)
            {
                UpdatePoolLoop();
            }
            else if ((flipping || leaping || superJump || !this.kickKaizo.atZero)
                && dir != 0
                && this.poleTechCooldown.ended)
            {
                if (this.kickEnabled
                    && ((leaping || superJump) && intinput.x == dir
                        || flipping && (intinput.x != 0 || intinput.y != 0))
                    && player.wantToJump > 0
                )
                {
                    Vector2 centerCheck;
                    float radius = 20f;
                    Vector2 bodydir = (player.bodyChunks[0].pos - player.bodyChunks[1].pos).normalized;

                    if (leaping)
                    {
                        radius = Mathf.Sqrt(Mathf.Abs(player.bodyChunks[0].vel.x)) * 10f;
                        centerCheck = player.bodyChunks[0].pos;
                        centerCheck += bodydir * radius * 0.65f;
                    }
                    else
                    {
                        radius = Mathf.Sqrt(player.bodyChunks[1].vel.magnitude) * 8f;
                        centerCheck = player.bodyChunks[1].pos;
                        centerCheck -= bodydir * radius * 0.30f;
                    }
                    radius = Mathf.Clamp(radius, 20f, 100f);

                    this.debugCircleVisible = true;
                    if (this.debugCircle != null)
                    {
                        this.debugCircle.pos = centerCheck;
                        this.debugCircle.radius = radius;
                        this.debugCircle.innerRatio = 0.05f;
                    }
                        
                    var KickRadiusCheck = BTWFunc.GetAllCreatureInRadius(room, centerCheck, radius);
                    if (KickRadiusCheck.Count > 0)
                    {
                        if (this.debugCircle != null) { this.debugCircle.innerRatio = 0.10f; }

                        KickRadiusCheck.RemoveAll(x => x.physicalObject == player 
                            || (!this.DoDamagePlayers && x.physicalObject is Player)
                            || player.TotalMass / (x.physicalObject as Creature).TotalMass > 8
                            || x.physicalObject.grabbedBy.Exists(x => x.grabber == player));
                        
                        if (KickRadiusCheck.Count > 0)
                        {
                            if (this.debugCircle != null) { this.debugCircle.innerRatio = 0.25f; }
                            int indexCreature = 0;
                            bool deadCreature = (KickRadiusCheck[0].physicalObject as Creature).dead;
                            for (int i = 1; i < KickRadiusCheck.Count; i++)
                            {
                                bool isdead = (KickRadiusCheck[i].physicalObject as Creature).dead;
                                if (!isdead || (isdead && deadCreature))
                                {
                                    if (deadCreature && !isdead)
                                    {
                                        indexCreature = i;
                                        deadCreature = isdead;
                                    }
                                    else if (KickRadiusCheck[i].distance < KickRadiusCheck[indexCreature].distance)
                                    {
                                        indexCreature = i;
                                        deadCreature = isdead;
                                    }
                                }
                            }
                            InitKick(KickRadiusCheck[indexCreature].closestBodyChunk);
                            return;

                        }
                    }
                }

                if (this.HoldToPoles) {
                    // Plugin.Log("Pole tech updated !");
                    float dist;
                    if (intinput.y <= 0
                        && intinput.x == dir
                        && speed > 4f
                        && player.wantToJump > 0
                        && poleLoopExitTick.ended
                        && ((IsTileBeam(0) && player.bodyChunks[0].pos.x * dir < GetTilePos(0).x * dir) 
                            || (IsTileBeam(0, new(dir, 0), out dist) && dist < 20f)
                        )
                        && (flipping || leaping || superJump)
                    )
                    {
                        InitPolePounce();
                    }
                    else if (intinput.y == 1
                        && !poleLoopExitTick.ended
                        && !jumpHeld
                        && player.wantToJump <= 0
                        && room.GetTile(new IntVector2(this.lastPoleLoopTileX, GetTileIntPos(0).y)).verticalBeam
                        && (flipping || leaping)
                    )
                    {
                        EndPoleLoopSlideUp();
                    }
                    else if (intinput.y <= 0
                        && intinput.x == dir
                        && speed > 7f
                        && player.wantToJump > 0
                        && (!poleLoopExitTick.ended
                            || (IsTileBeam(1) && player.bodyChunks[1].pos.x * dir > GetTilePos(1).x * dir) 
                            || (IsTileBeam(1, new(-dir, 0), out dist) && dist < 20f))
                        && (leaping || superJump)
                    )
                    {
                        InitPoleHop();
                    }
                    else if (intinput.y < 0
                        && intinput.x == -dir
                        && speed > 5f
                        && speed < 50f
                        && (GetIntDirInput(1).x != -dir 
                            || GetIntDirInput(2).x != -dir 
                            || GetIntDirInput(3).x != -dir 
                            || GetIntDirInput(4).x != -dir 
                            || GetIntDirInput(5).x != -dir)
                        && !jumpHeld
                        && player.wantToJump <= 0
                        && ((IsTileBeam(0, new(-dir, 0)) && !IsTileBeam(0)) || !poleLoopExitTick.ended)
                        && (leaping || superJump)
                    )
                    {
                        InitPoleLoop(!poleLoopExitTick.ended ? lastPoleLoopTileX : GetTileIntPos(0, new(-dir, 0)).x);
                    }
                }
                else
                {
                    // Plugin.Log("No pole holding ! No tech !");
                }
                 
            }
        }
        base.Update();
    }

    // ------ Variables

    // Objects

    // Basic
    public bool isFake = false;

    public bool isPolePounce = false;

    public bool kickEnabled = false;
    public bool kickExhausted = false;
    public Counter kickExhaustCount = new(BTWFunc.FrameRate * 10);
    public Counter kickKaizo = new(10);

    public bool poleLoop = false;
    public int lastPoleLoopTileX = -1;
    public float lastPoleLoopY = -1;
    public int poleLoopDir = 0;
    public Counter poleLoopCount = new(2);
    public Counter poleLoopTick = new(8);
    public Counter poleLoopExitTick = new(7);
    public Counter poleTechCooldown = new(5);
    public bool nextLoopBuffered = false;
    public bool jumpLoopBuffered = false;
    public bool slideUpLoopBuffered = false;

    public bool bodyInFrontOfPole = false;
    public bool lastBodyInFrontOfPole = false;
    public List<int> bodyPartInMG = new();

    // Get - Set
    public bool DoDamagePlayers
    {
        get {
            Player player = this.RealizedPlayer;
            Room room = player.room;
            if (player != null && room != null)
            {
                if (ModManager.CoopAvailable)
                {
                    return !player.isNPC && Custom.rainWorld.options.friendlyFire;
                }
                return room.game.IsArenaSession;
            }
            return false;
        }
    }
    public bool HoldToPoles
    {
        get
        {
            Player player = this.RealizedPlayer;
            if (player != null)
            {
                return !WallClimbManager.TryGetManager(player.abstractCreature, out var WCM) || WCM.holdToPoles;
            }
            return false;
        }
    }
}

public static class PoleKickManagerHooks
{
    public static void ApplyHooks()
    {
        On.Player.ctor += Player_PoleKickManager_Init;
        On.Player.Update += Player_PoleKickManager_Update;
        On.Player.Collide += Player_PoleKickManager_CancelPoleLoop;
        BTWPlugin.Log("PoleKickManagerHooks ApplyHooks Done !");
    }

    private static void Player_PoleKickManager_CancelPoleLoop(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        orig(self, otherObject, myChunk, otherChunk);
        if (PoleKickManager.TryGetManager(self.abstractCreature, out var PKM) && PKM.poleLoop)
        {
            PKM.CancelPoolLoop();
        }
    }
    private static void Player_PoleKickManager_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        try
        {
            if (PoleKickManager.TryGetManager(self.abstractCreature, out var PKM) && !PKM.isFake)
            {
                PKM.Update();
            }
        }
        catch (System.Exception ex)
        {
            BTWPlugin.logger.LogError($"PoleKickManager had an error on update : {ex}");
        }
    }
    private static void Player_PoleKickManager_Init(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        bool local = BTWFunc.IsLocal(self);
        bool trailseeker = TrailseekerFunc.IsTrailseeker(self);
        bool toEveryone;
        if (BTWPlugin.meadowEnabled)
        {
            toEveryone = MeadowFunc.ShouldGiveNewPoleTechToEveryone();
        }
        else
        {
            toEveryone = BTWRemix.EveryoneCanPoleTech.Value;
        }
        if (toEveryone || trailseeker)
        {
            if (!PoleKickManager.TryGetManager(self.abstractCreature, out _))
            {
                BTWPlugin.Log("PoleKickManager initiated");

                PoleKickManager.AddManager(abstractCreature, out var PKM);
                PKM.kickEnabled = trailseeker;
                if (ModManager.MSC && MSCFunc.IsRivulet(self))
                {
                    PKM.poleLoopCount = new(15);
                    PKM.poleLoopTick = new(6);
                    PKM.poleLoopExitTick = new(5);
                }
                else if (trailseeker)
                {
                    PKM.poleLoopCount = new(4);
                }
                PKM.isFake = !local;

                BTWPlugin.Log("PoleKickManager created !");
            }
        }
    }
}