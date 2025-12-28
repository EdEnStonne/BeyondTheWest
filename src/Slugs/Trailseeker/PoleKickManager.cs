using UnityEngine;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using BeyondTheWest.MSCCompat;
using System.Collections.Generic;

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
        if (Plugin.meadowEnabled)
        {
            MeadowCalls.PoleKickManager_Init(this);
        }
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
            // Plugin.Log("Pole Pounce Init !");

            int direction = this.MovementDirection;
            room.PlaySound(SoundID.Slugcat_From_Horizontal_Pole_Jump, player.mainBodyChunk, false, 1.0f, 1.5f);
            room.PlaySound(SoundID.Slugcat_Rocket_Jump, player.mainBodyChunk, false, 0.75f, 1.25f);
			player.animation = Player.AnimationIndex.RocketJump;

            Vector2 boost = new (direction * -8f, 11f);
			if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                boost.y += 7f;
                boost.x += 5f * -direction;
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
            player.jumpStun = 10 * -direction;
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                MTM.ApplyJumpTechBoost();
            }
            if (AbstractEnergyCore.TryGetCore(player.abstractCreature, out var core))
            {
                core.boostingCount = -20;
            }

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
            
            foreach (BodyChunk chunk in player.bodyChunks)
            {
                chunk.vel = boost;
            }
            player.jumpStun = 6 * direction;
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                MTM.ApplyJumpTechBoost();
            }
            if (AbstractEnergyCore.TryGetCore(player.abstractCreature, out var core))
            {
                core.boostingCount = -20;
            }

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
            int direction = this.MovementDirection;
            player.wantToJump = 0;

            Vector2 boost = new (-direction * 9f, 4f);
			if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                boost.y += 5f;
                boost.x += 5f * -direction;
            }
            if (ModManager.MSC && player.isSlugpup)
            {
                boost.y /= 3f;
                boost.x /= 2f;
            }
            Vector2 knockback = -boost;

            float weightRatio = player.TotalMass / target.TotalMass;
            float knockbackBonus = Mathf.Clamp(Mathf.Log(weightRatio, 5), -1, 1) + 1;
            knockback *= knockbackBonus;
            boost *= 2 - knockbackBonus;
            if (boost.magnitude < 5)
            {
                boost = new (5f * -direction, 0f);
            }

            player.animation = Player.AnimationIndex.RocketJump;
            player.bodyChunks[1].pos = player.bodyChunks[0].pos;
            player.bodyChunks[0].pos += boost.normalized * 10f;
            player.rollDirection = -direction;
            foreach (BodyChunk chunk in player.bodyChunks)
            {
                chunk.vel = boost;
            }
            player.jumpStun = 15 * direction;
            if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
            {
                MTM.ApplyJumpTechBoost();
            }
            Plugin.Log($"WOW ! [{player}] kicked [{target}] with a knockback of [{knockback}] and a boost of [{boost}] !");
            
            HitCreatureWithKick(player, chuckHit, knockback, knockbackBonus);
        }
    }
    public static void HitCreatureWithKick(Player kicker, BodyChunk chuckHit, Vector2 knockback, float knockbackBonus)
    {
        Creature target = chuckHit.owner as Creature;
        Room room = target.room;
        if (room != null && target != null)
        {
            
            if (BTWFunc.IsLocal(target.abstractCreature) && !(Plugin.meadowEnabled && MeadowFunc.ShouldHoldFireFromOnlineArenaTimer()))
            {
                BTWFunc.CustomKnockback(chuckHit, knockback);
                
                float dmg = 0.2f;
                float stun = 0.65f * BTWFunc.FrameRate * (1 + knockbackBonus);
                if (knockbackBonus == 0)
                {
                    dmg = 0.1f;
                    stun = 0f;
                }
                target.Violence(
                    kicker.bodyChunks[1], 
                    knockback.normalized,
                    chuckHit,
                    null, Creature.DamageType.Blunt,
                    dmg, stun
                );
                if (Plugin.meadowEnabled)
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
                Plugin.Log($"Kick of [{kicker}] on [{target}] dealt <{dmg}> damage and <{stun}> stun ! (WR of <{knockbackBonus}>) !");
                
            }
            if (Plugin.meadowEnabled && BTWFunc.IsLocal(kicker.abstractCreature) && MeadowFunc.IsMeadowLobby())
            {
                MeadowCalls.PoleKickManager_RPCKick(kicker, chuckHit, knockback, knockbackBonus);
            }

            room.PlaySound(SoundID.Rock_Hit_Creature, kicker.mainBodyChunk, false, 1.0f, BTWFunc.Random(0.85f,1.1f));
            room.PlaySound(SoundID.Slugcat_Jump_On_Creature, kicker.mainBodyChunk, false, 0.75f, BTWFunc.Random(1.1f,1.3f));
			room.AddObject(
                new ExplosionSpikes(
                    room, 
                    kicker.bodyChunks[1].pos + new Vector2(0f, -kicker.bodyChunks[1].rad),
                    5, 10f, 20f, 7.5f, 50f, new Color(1f, 1f, 1f, 0.5f)));
        }
    }
    
    private void InitPoleLoop(int poleLoopTileX)
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            if (this.lastPoleLoopTileX == poleLoopTileX)
            {
                if (this.poleLoopCount.reachedMax) { Plugin.Log("Maxuimum Pole Loop reached !"); return; }
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

            room.PlaySound(SoundID.Slugcat_From_Horizontal_Pole_Jump, player.mainBodyChunk, false, 2.5f, 1.15f);
			player.animation = Player.AnimationIndex.RocketJump;
            player.rollDirection = 0;
            
            // player.bodyChunks[0].pos.x = room.MiddleOfTile(new IntVector2(poleLoopTileX, 0)).x;
            if (AbstractEnergyCore.TryGetCore(player.abstractCreature, out var core))
            {
                core.boostingCount = -20;
            }

			player.room.AddObject(
                new ExplosionSpikes(
                    room, 
                    player.bodyChunks[1].pos + new Vector2(0f, -player.bodyChunks[1].rad),
                    9, 5f, 5f, 3.5f, 20f, new Color(1f, 1f, 1f, 0.5f)));
        }
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
            float smoothing = 0.75f;
            float XposTile = room.MiddleOfTile(new IntVector2(this.lastPoleLoopTileX, 0)).x;
            bool hasMTM = ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM);
            if (ModManager.MSC && MSCFunc.IsRivulet(player))
            {
                heightGained += 30f;
                loopLenght = 27.5f;
                smoothing = 0.35f;
            }
            if (hasMTM)
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

            if (this.poleLoopTick.ended)
            {
                // Plugin.Log($"Pole loop ended !");

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

                this.poleLoop = false;
                this.poleLoopDir = 0;
            }
            else
            {
                IntVector2 PoleTile = GetTileIntPos(0);
                PoleTile.x = this.lastPoleLoopTileX;
                PoleTile.y += 1;
                if (!room.GetTile(PoleTile).verticalBeam)
                {
                    Plugin.Log($"Pole loop flip exit !");

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
                    if (hasMTM)
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
    }
    
    // ------ Public Funcitions
    public void AnimationUpdate()
    {
        Player player = this.RealizedPlayer;
        if (player != null)
        {
            bool flipping = player.animation == Player.AnimationIndex.Flip;
            bool leaping = player.animation == Player.AnimationIndex.RocketJump;
            if (leaping && this.HoldToPoles)
            {
                // Plugin.Log($"<{intinput.y}>/<{intinput.x}>, <{dir}>, <{jumpHeld}>/<{jumpPressed}>, <{this.polePounceKaizoTick.valueDown}>/<{player.wantToJump}>, <{IsTileBeam(0)}>/<{IsTileBeam(1)}>, <{player.jumpStun * dir}>");

                if (this.poleLoop)
                {
                    if (this.IntDirectionalInput.y > 0)
                    {
                        CancelPoolLoop();
                        Plugin.Log($"Pole loop canceled because player held upward !");
                        int bonusSlideUp = 0;
                        if (ModifiedTechManager.TryGetManager(player.abstractCreature, out var MTM))
                        {
                            bonusSlideUp = MTM.poleBonus;
                        }
                        player.slideUpPole = (int)((20 + bonusSlideUp) * this.poleLoopCount.fractInv);
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
                                Plugin.Log($"Pole loop canceled because [{chuck}] has contact point [{contact}] !");
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
            else
            {
                this.isPolePounce = false;
                this.bodyInFrontOfPole = false;
                CancelPoolLoop();
            }
        }
        else
        {
            this.isPolePounce = false;
            this.bodyInFrontOfPole = false;
            CancelPoolLoop();
        }
    }
    public override void Update()
    {
        base.Update();
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null && !this.isFake)
        {
            AnimationUpdate();
            int dir = this.MovementDirection;
            IntVector2 intinput = this.IntDirectionalInput;
            bool jumpHeld = player.input[0].jmp;
            bool jumpPressed = jumpHeld && !player.input[1].jmp;
            bool flipping = player.animation == Player.AnimationIndex.Flip;
            bool leaping = player.animation == Player.AnimationIndex.RocketJump;

            // this.bodyInFrontOfPole = player.input[0].spec;
            
            if (this.poleLoop)
            {
                UpdatePoolLoop();
            }
            else if ((flipping || leaping)
                && Mathf.Abs(player.mainBodyChunk.vel.x) > 2f)
            {
                if (this.kickEnabled
                    && intinput.y <= 0
                    && intinput.x == dir
                    && player.wantToJump > 0
                    && player.jumpStun == 0
                )
                {
                    Vector2 centerCheck;
                    if (leaping)
                    {
                        centerCheck = player.bodyChunks[0].pos;
                        centerCheck += (player.bodyChunks[0].pos - player.bodyChunks[1].pos).normalized * this.kickRadius;
                    }
                    else
                    {
                        centerCheck = player.bodyChunks[1].pos;
                        centerCheck += (player.bodyChunks[1].pos - player.bodyChunks[0].pos).normalized * this.kickRadius;
                    }
                        
                    var KickRadiusCheck = BTWFunc.GetAllCreatureInRadius(room, centerCheck, this.kickRadius);
                    if (KickRadiusCheck.Count > 0)
                    {
                        int indexCreature = KickRadiusCheck.FindIndex(x => x.physicalObject != player 
                            && (this.DoDamagePlayers || x.physicalObject is not Player)
                            && player.TotalMass / (x.physicalObject as Creature).TotalMass <= 5);
                        if (indexCreature != -1)
                        {
                            InitKick(KickRadiusCheck[indexCreature].closestBodyChunk);
                            return;
                        }
                    }
                }

                if (this.HoldToPoles) {
                    // Plugin.Log("Pole tech updated !");
                    if (intinput.y <= 0
                        && intinput.x == (flipping && player.rollDirection != dir ? dir : -dir)
                        && player.wantToJump > 0
                        && poleLoopExitTick.ended
                        && ((IsTileBeam(0) && player.bodyChunks[0].pos.x * dir < GetTilePos(0).x * dir) 
                            || (IsTileBeam(0, new(dir, 0), out float dist1) && dist1 < 20f)
                        )
                        && player.jumpStun * -dir <= 0
                    )
                    {
                        InitPolePounce();
                    }
                    else if (intinput.y <= 0
                        && intinput.x == dir
                        && player.wantToJump > 0
                        && (IsTileBeam(1) || !poleLoopExitTick.ended)
                        && player.jumpStun * dir <= 0
                        && leaping
                    )
                    {
                        InitPoleHop();
                    }
                    else if (intinput.y < 0
                        && intinput.x == -dir
                        && (GetIntDirInput(1).x != -dir 
                            || GetIntDirInput(2).x != -dir 
                            || GetIntDirInput(3).x != -dir 
                            || GetIntDirInput(4).x != -dir 
                            || GetIntDirInput(5).x != -dir)
                        && !jumpHeld
                        && player.wantToJump <= 0
                        && (IsTileBeam(0, new(-dir, 0)) || !poleLoopExitTick.ended)
                        && !IsTileBeam(0)
                        && leaping
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
    }

    // ------ Variables

    // Objects

    // Basic
    public bool isFake = false;

    public bool isPolePounce = false;
    public bool kickEnabled = false;
    public float kickRadius = 30f;

    public bool poleLoop = false;
    public int lastPoleLoopTileX = -1;
    public float lastPoleLoopY = -1;
    public int poleLoopDir = 0;
    public Counter poleLoopCount = new(2);
    public Counter poleLoopTick = new(8);
    public Counter poleLoopExitTick = new(7);

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
        Plugin.Log("PoleKickManagerHooks ApplyHooks Done !");
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
        if (PoleKickManager.TryGetManager(self.abstractCreature, out var PKM) && !PKM.isFake)
        {
            PKM.Update();
        }
    }
    private static void Player_PoleKickManager_Init(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        bool local = BTWFunc.IsLocal(self);
        bool trailseeker = TrailseekerFunc.IsTrailseeker(self);
        bool toEveryone;
        if (Plugin.meadowEnabled)
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
                Plugin.Log("PoleKickManager initiated");

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

                Plugin.Log("PoleKickManager created !");
            }
        }
    }
}