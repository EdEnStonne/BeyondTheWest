using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest;

public class WallClimbManager : AdditionnalTechManager<WallClimbManager>
{
    public static void AddManager(AbstractCreature creature, out WallClimbManager WCM)
    {
        WCM = new(creature);
        AddNewManager(creature, WCM);
    }
    public static void AddManager(AbstractCreature creature)
    {
        AddManager(creature, out _);
    }
    
    public WallClimbManager(AbstractCreature abstractCreature) : base(abstractCreature)
    {
        this.abstractPlayer = abstractCreature;
        if (Plugin.meadowEnabled)
        {
            MeadowCalls.WallClimbMeadow_Init(this);
        }
    }
    
    // ------ Local Functions

    // WallClimb
    void InitWallClimb()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            // Plugin.Log("Init Wall Climb !");
            Vector2 position = player.mainBodyChunk.pos;
            int wallJumpDir = this.WallJumpDirection;

            this.wallClimbCount = this.MaxWallClimbCount;
            this.wallClimbLeft--;
            this.wallGrip = 1f;
            this.wallGripCount = 0;
            this.wallClimbExtend = false;
            this.canWallClimb = false;

            player.Blink(3);
            room.PlaySound(SoundID.Slugcat_Belly_Slide_Init, player.mainBodyChunk, false, 1f, 1.15f);
            for (int i = (int)UnityEngine.Random.Range(8f, 10f); i > 0; i--)
            {
                room.AddObject(
                    new WaterDrip(
                        position,
                        new Vector2(UnityEngine.Random.Range(0f, 10f) * wallJumpDir, -5f),
                        false));
            }
        }
    }
    void UpdateWallClimb()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            Vector2 position = player.mainBodyChunk.pos;
            IntVector2 intDir = this.IntDirectionalInput;
            int wallJumpDir = this.WallJumpDirection;

            if (this.wallClimbCount == 0)
            {
                this.wallGrip = -2f;
                return;
            }
            if (player.bodyMode != Player.BodyModeIndex.WallClimb)
            {
                this.wallGrip = 0f;
                return;
            }
            if (player.bodyChunks[0].ContactPoint.y == 1)
            {
                player.Blink(24);
                player.stun = 8;
                this.wallClimbCount = 0;
                this.wallGrip = -10f;
                return;
            }
            this.wallClimbCount--;

            float countRatio = ((float)this.wallClimbCount) / (this.wallClimbExtend ? this.MaxWallClimbExtendCount : this.MaxWallClimbCount);

            foreach (BodyChunk bodyChunk in player.bodyChunks)
            {
                bodyChunk.vel.y = player.g + this.WallClimbForce * BTWFunc.EaseIn(countRatio, 3);
                bodyChunk.vel.x = -wallJumpDir;
            }
            // Plugin.Log("Vel : "+ (player.mainBodyChunk.vel.y - player.g) +" / Count : "+ this.wallClimbCount);

            // room.AddObject(
            //     new WaterDrip(
            //         position,
            //         new Vector2(UnityEngine.Random.Range(0f, 5f) * wallJumpDir, -10f),
            //         false));
        }
    }

    // WallKick
    void InitWallKick(bool flip = false)
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            // Plugin.Log("Init Wall Kick !");
            Vector2 position = player.bodyChunks[1].pos;

            this.wallGrip = -5f;
            this.wallClimbCount = 0;
            this.wallGripCount = 0;
            this.canWallKick = 0;
            this.wallKickDirection = this.lastWallJumpDirection;
            this.canWallClimb = true;

            this.flipFromWallKick = false;
            this.rocketJumpFromWallKick = false;
            if (flip)
            {
                player.animation = Player.AnimationIndex.Flip;
                this.flipFromWallKick = true;
            }
            else
            {
                player.animation = Player.AnimationIndex.RocketJump;
                this.rocketJumpFromWallKick = true;
            }

            player.rollDirection = this.wallKickDirection;
            player.slideDirection = this.wallKickDirection;

            if (!flip)
            {
                player.bodyChunks[1].pos = player.bodyChunks[0].pos;
                player.bodyChunks[0].pos += new Vector2(this.wallKickDirection * 10f, 0f);
            }

            foreach (BodyChunk bodyChunk in player.bodyChunks)
            {
                bodyChunk.vel.x = this.wallKickDirection * (this.flipFromWallKick ? this.WallKickFlipForce : this.WallKickForce);
                bodyChunk.vel.y = Mathf.Max(bodyChunk.vel.y, this.flipFromWallKick ? this.WallKickFlipForce : this.WallKickForce);
            }

            room.PlaySound(SoundID.Slugcat_Rocket_Jump, player.mainBodyChunk, false, 1f, 0.85f);
            if (flip)
            {
                room.AddObject(
                    new ExplosionSpikes(
                        room,
                        position,
                        6, 7f, 5f, 8.5f, 40f,
                        new Color(1f, 1f, 1f, 0.25f)));
            }
            else
            {
                for (int i = (int)UnityEngine.Random.Range(3f, 5f); i > 0; i--)
                {
                    room.AddObject(
                        new WaterDrip(
                            position,
                            new Vector2(UnityEngine.Random.Range(-0.5f, 2f) * this.wallKickDirection, 2f),
                            false));
                }
            }
        }
    }
    void UpdateWallKickPounce()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            player.bodyChunks[0].pos = player.bodyChunks[1].pos + new Vector2(this.wallKickDirection, player.bodyChunks[0].vel.y / 4f).normalized * 10f;
            foreach (BodyChunk bodyChunk in player.bodyChunks)
            {
                bodyChunk.vel.x = this.wallKickDirection * this.WallKickForce;
            }
        }
    }

    // WallVerticalPounce
    void InitWallVerticalPounce()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            // Plugin.Log("Init Wall Vertical Pounce !");
            int wallJumpDir = this.WallJumpDirection;
            Vector2 position = player.bodyChunks[1].pos;

            this.rocketJumpFromWallVerticalPounce = true;
            this.wallGrip = 0f;
            this.wallGripCount = 0;
            this.wallClimbCount = 0;
            this.wallVerticalPounceDirection = -wallJumpDir;
            this.canWallClimb = true;
            this.canWallKick = 0;
            this.wallVerticalPounceCount = 0;
            this.wantToJumpFromWVP = -10;

            player.animation = Player.AnimationIndex.RocketJump;
            player.rollDirection = this.wallVerticalPounceDirection;
            player.slideDirection = this.wallVerticalPounceDirection;

            player.bodyChunks[1].pos = player.bodyChunks[0].pos;
            player.bodyChunks[0].pos += new Vector2(this.wallVerticalPounceDirection * 5f, 15f);

            foreach (BodyChunk bodyChunk in player.bodyChunks)
            {
                bodyChunk.vel.x = -this.wallVerticalPounceDirection * 20f;
                bodyChunk.vel.y = this.WallVerticalPounceForce + player.g;
            }

            room.PlaySound(SoundID.Slugcat_Rocket_Jump, player.mainBodyChunk, false, 1f, 1.25f);
            room.AddObject(
                new ExplosionSpikes(
                    room,
                    position,
                    9, 7f, 10f, 6f, 30f,
                    new Color(1f, 1f, 1f, 0.25f)));
        }
    }
    void UpdateWallVerticalPounce()
    {
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            wallVerticalPounceCount++;
            if (this.wantToJumpFromWVP > -this.WallVerticalPounceBufferPenalityFrames - 1) 
                { this.wantToJumpFromWVP--; }
            if (this.wantToJumpFromWVP < -this.WallVerticalPounceBufferPenalityFrames && player.input[0].jmp && !player.input[1].jmp) 
                { this.wantToJumpFromWVP = this.WallVerticalPounceBufferFrames; }
            if ((player.bodyChunks[0].ContactPoint.x == this.wallVerticalPounceDirection || player.bodyChunks[1].ContactPoint.x == this.lastWallJumpDirection)
                && this.wantToJumpFromWVP > 0)
            {
                IntVector2 intDir = this.IntDirectionalInput;
                // if (intDir.y == 1)
                // {
                //     this.rocketJumpFromWallVerticalPounce = false;
                //     player.bodyMode = Player.BodyModeIndex.WallClimb;
                //     InitWallClimb();
                //     return;
                // }
                // else
                if (intDir.x == -this.wallVerticalPounceDirection)
                {
                    this.rocketJumpFromWallVerticalPounce = false;
                    InitWallKick(true);
                    return;
                }
            }
            if (player.bodyChunks[0].ContactPoint.y == 1)
            {
                this.rocketJumpFromWallVerticalPounce = false;
                player.Blink(25);
                player.stun = 5;
                return;
            }
            if (player.mainBodyChunk.vel.y <= 0f)
            {
                this.rocketJumpFromWallVerticalPounce = false;
                return;
            }
            player.bodyChunks[0].pos = player.bodyChunks[1].pos + new Vector2(player.bodyChunks[0].vel.x / 2f, 1).normalized * 10f;
            foreach (BodyChunk bodyChunk in player.bodyChunks)
            {
                bodyChunk.vel.x = Mathf.Min(20f, this.wallVerticalPounceDirection * (wallVerticalPounceCount - 10f));
                // bodyChunk.vel.y = this.WallVerticalPounceForce + player.g;
            }
        }
    }

    // ------ Public Funcitions
    public void AnimationUpdate()
    {
        Player player = this.RealizedPlayer;
        if (player != null)
        {
            if (this.flipFromWallKick && player.animation != Player.AnimationIndex.Flip)
            {
                this.flipFromWallKick = false;
            }
            if (this.rocketJumpFromWallKick && player.animation != Player.AnimationIndex.RocketJump)
            {
                this.rocketJumpFromWallKick = false;
            }
            if (this.rocketJumpFromWallVerticalPounce && player.animation != Player.AnimationIndex.RocketJump)
            {
                this.rocketJumpFromWallVerticalPounce = false;
            }
            if (player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                this.lastWallJumpDirection = this.WallJumpDirection;

                if (this.canWallKick != this.WallKickFrames)
                {
                    this.canWallKick = this.WallKickFrames;
                }

                if (this.wallGripCount < this.MaxWallGripCount)
                {
                    this.wallGripCount++;
                    if (this.wallGrip > 1f)
                    {
                        this.wallGrip = 1f;
                    }
                    else if (this.wallGrip < 1f)
                    {
                        this.wallGrip += 0.1f;
                    }
                }
                else
                {
                    this.wallGrip = 0f;
                }
            }
            else
            {
                if (this.wallClimbCount > 0 && !this.wallClimbExtend
                    && !this.flipFromWallKick
                    && !this.rocketJumpFromWallKick
                    && !this.rocketJumpFromWallVerticalPounce
                )
                {
                    foreach (BodyChunk bodyChunk in player.bodyChunks)
                    {
                        bodyChunk.vel.y = Mathf.Min(5f + player.g, bodyChunk.vel.y/2);
                    }
                }
                if (this.canWallKick > 0)
                {
                    this.canWallKick--;
                }
                else if (this.wallClimbCount > 0)
                {
                    this.wallClimbCount = 0;
                }
                if (this.wallGrip > 0f)
                {
                    this.wallGrip = 0f;
                }
                else if (this.wallGrip < 0f)
                {
                    this.wallGrip += 0.1f;
                }
            }
        }
        else
        {
            this.flipFromWallKick = false;
            this.rocketJumpFromWallKick = false;
            this.rocketJumpFromWallVerticalPounce = false;
            this.canWallKick = 0;
            this.wallClimbCount = 0;
            this.wallGrip = 0f;
        }
    }
    public override void Update()
    {
        base.Update();
        Player player = this.RealizedPlayer;
        Room room = player.room;
        if (player != null && room != null)
        {
            AnimationUpdate();
            if (this.indicatorUI == null)
            {
                this.indicatorUI = new WallClimbManagerIndicatorUI(this);
                room.AddObject(this.indicatorUI);
            }
            if (this.indicatorUI != null && room != this.indicatorUI.room)
            {
                this.indicatorUI.RemoveFromRoom();
                room.AddObject(this.indicatorUI);
            }

            if (this.wallClimbLeft <= 0)
            {
                player.Blink(5);
            }

            IntVector2 intDir = this.IntDirectionalInput;
            bool jumpHeld = player.input[0].jmp;
            bool jumpPressed = jumpHeld && !player.input[1].jmp;
            bool specHeld = player.input[0].spec;
            bool specPressed = specHeld && !player.input[1].spec;
            bool ignorePoleToggle = BTWRemix.TrailseekerIgnorePoleToggle.Value;

            if (ignorePoleToggle)
            {
                if (specPressed)
                {
                    this.holdToPoles = !this.holdToPoles;
                    this.indicatorUI?.ShowPoleIcon();
                }
            }
            else
            {
                this.holdToPoles = !specHeld;
                if (specPressed) { this.indicatorUI?.ShowPoleIcon(); }
            }

            if (player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                if (intDir.y == 1)
                {
                    // if (this.wallGrip < 1f) { Plugin.Log("Gripping ! " + this.wallGrip); }
                    foreach (BodyChunk bodyChunk in player.bodyChunks)
                    {
                        bodyChunk.vel.y = player.g * this.wallGrip;
                    }
                }

                if (this.wallClimbCount <= 0)
                {
                    if (intDir.y == 1 && jumpPressed && this.wallClimbLeft > 0 && this.canWallClimb)
                    {
                        InitWallClimb();
                    }
                }
                else
                {
                    if (intDir.y == 1 && jumpPressed && this.wallClimbCount > 0)
                    {
                        if (this.wallClimbCount <= this.MaxWallClimbCount - this.WallVerticalPounceMinimunFrames)
                        {
                            InitWallVerticalPounce();
                        }
                        else
                        {
                            this.wallClimbCount = 0;
                            player.Jump();
                        }
                    }
                }
            }
            else
            {
                if (this.Landed)
                {
                    this.wallClimbLeft = this.MaxWallClimb;
                    this.canWallClimb = true;
                    this.wallGripCount = 0;
                }
                if (this.canWallKick > 0 && (this.wallClimbCount > 0 ? jumpHeld : jumpPressed) && intDir.x == this.lastWallJumpDirection)
                {
                    InitWallKick(this.wallClimbCount >= this.MaxWallClimbCount - this.WallKickFlipMaximumFrames);
                }
            }

            if (this.rocketJumpFromWallKick)
            {
                UpdateWallKickPounce();
            }
            if (this.wallClimbCount > 0)
            {
                UpdateWallClimb();
            }
            if (this.rocketJumpFromWallVerticalPounce)
            {
                UpdateWallVerticalPounce();
            }
        }
        else if (this.indicatorUI != null)
        {
            room.RemoveObject(this.indicatorUI);
            this.indicatorUI.Destroy();
            this.indicatorUI = null;
        }
    }

    // ------ Variables

    // Objects
    public WallClimbManagerIndicatorUI indicatorUI;

    // Basic
    public float wallGrip = 0f;
    public int wallGripCount = 0;
    public int wallClimbCount = 0;
    public int wallVerticalPounceCount = 0;
    public int wallClimbLeft = 3;
    public int canWallKick = 0;
    public int wallKickDirection = 0;
    public int lastWallJumpDirection = 0;
    public int wallVerticalPounceDirection = 0;
    public int wantToJumpFromWVP = 0;
    public bool canWallClimb = true;
    public bool wallClimbExtend = false;
    public bool flipFromWallKick = false;
    public bool rocketJumpFromWallKick = false;
    public bool rocketJumpFromWallVerticalPounce = false;
    public bool holdToPoles = true;

    public int MaxWallClimb = 3;
    public int MaxWallClimbCount = 20;
    public int MaxWallClimbExtendCount = 50;
    public int MaxWallGripCount = 15 * BTWFunc.FrameRate;
    public float WallClimbForce = 25f;
    public float WallKickForce = 10f;
    public float WallKickFlipForce = 12.5f;
    public float WallVerticalPounceForce = 15f;
    public int WallClimbExtendFrames = 10;
    public int WallKickFrames = 8;
    public int WallVerticalPounceMinimunFrames = 7;
    public int WallKickFlipMaximumFrames = 5;
    public int WallVerticalPounceBufferFrames = 5;
    public int WallVerticalPounceBufferPenalityFrames = 20;
    
    // Get - Set
    public int WallJumpDirection
    {
        get
        {
            int direction = 0;
            Player player = this.RealizedPlayer;
            if (player != null)
            {
                if (player.canWallJump != 0)
                {
                    direction = Math.Sign(player.canWallJump);
                }
                else if (player.bodyChunks[0].ContactPoint.x != 0)
                {
                    direction = -player.bodyChunks[0].ContactPoint.x;
                }
                else
                {
                    direction = -player.flipDirection;
                }
            }
            return direction;
        }
    }
}

public static class WallClimbManagerHooks
{
    public static void ApplyHooks()
    {
        On.Player.Update += Player_Trailseeker_WallClimbManager_Update;

        On.Player.WallJump += Player_WallClimbManager_CancelWallJump;
        IL.Player.TerrainImpact += Player_WallClimbManager_CancelTechOnWallPounce;
        IL.Player.ThrowObject += Player_WallClimbManager_WallClimbExtend;
        IL.Player.Update += Player_WallClimbManager_StopLoopSoundOnGrip;

        IL.Player.UpdateBodyMode += Player_WallClimbManager_StopGrippingPoles;
        IL.Player.UpdateAnimation += Player_WallClimbManager_StopGrippingPoles;
        IL.Player.GrabVerticalPole += Player_WallClimbManager_StopGrippingPoles;
        IL.Player.Update += Player_WallClimbManager_StopGrippingPoles;
        IL.Player.MovementUpdate += Player_WallClimbManager_StopGrippingPoles;
        
        Plugin.Log("WallClimbManagerHooks ApplyHooks Done !");
    }
    private static void Player_Trailseeker_WallClimbManager_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (WallClimbManager.TryGetManager(self.abstractCreature, out var WCM))
        {
            WCM.Update();
        }
        orig(self, eu);
    }
    
    private static void Player_WallClimbManager_StopGrippingPoles(ILContext il)
    {
        Plugin.Log("WallClimbManager IL 5 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            Instruction start = cursor.Next;

            static bool CheckPoleGrab(bool orig_bool, Player player)
            {
                if (WallClimbManager.TryGetManager(player.abstractCreature, out var WCM) && !WCM.holdToPoles)
                {
                    // Plugin.Log("Pole Grab Cancelled !");
                    return false;
                }
                return orig_bool;
            }

            while (cursor.TryGotoNext(MoveType.After,x => x.MatchLdfld<Room.Tile>(nameof(Room.Tile.verticalBeam))))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(CheckPoleGrab);
            }
            cursor.TryGotoPrev(MoveType.Before, x => x == start);
            while (cursor.TryGotoNext(MoveType.After,x => x.MatchLdfld<Room.Tile>(nameof(Room.Tile.horizontalBeam))))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(CheckPoleGrab);
            }

            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("WallClimbManager IL 5 ends");
    }
    private static void Player_WallClimbManager_CancelWallJump(On.Player.orig_WallJump orig, Player self, int direction)
    {
        if (WallClimbManager.TryGetManager(self.abstractCreature, out var WCM) && (
           ( WCM.IntDirectionalInput.y == 1 && WCM.canWallClimb) ||
            WCM.wallClimbCount > 0 ||
            WCM.rocketJumpFromWallVerticalPounce ||
            WCM.rocketJumpFromWallKick ||
            WCM.flipFromWallKick
        ))
        {
            return;
        }
        orig(self, direction);
    }
    
    private static void Player_WallClimbManager_CancelTechOnWallPounce(ILContext il)
    {
        Plugin.Log("WallClimbManager IL 2 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<MoreSlugcats.MMF>(nameof(MoreSlugcats.MMF.cfgWallpounce)),
                x => x.MatchCallvirt(typeof(Configurable<bool>).GetProperty(nameof(Configurable<bool>.Value)).GetGetMethod())
            ) && cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.standing)),
                x => x.MatchBrtrue(out _)
            ))
            {
                static void WallClimbingCancel(Player player)
                {
                    if (WallClimbManager.TryGetManager(player.abstractCreature, out var WCM))
                    {
                        if (WCM.rocketJumpFromWallKick) { WCM.rocketJumpFromWallKick = false; }
                        if (WCM.rocketJumpFromWallVerticalPounce) { WCM.rocketJumpFromWallVerticalPounce = false; }
                    }
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(WallClimbingCancel);
            }
            else
            {
                Plugin.logger.LogError("Couldn't find IL hook :<");
                Plugin.Log(il);
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("WallClimbManager IL 2 ends");
    }
    
    private static void Player_WallClimbManager_WallClimbExtend(ILContext il)
    {
        Plugin.Log("WallClimbManager IL 3 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.firstChunk)).GetGetMethod()),
                x => x.MatchLdfld<BodyChunk>(nameof(BodyChunk.pos)),
                x => x.MatchLdloca(0),
                x => x.MatchCall<IntVector2>(nameof(IntVector2.ToVector2)),
                x => x.MatchLdcR4(10)
            ))
            {
                static void WallClimbExtend(Player player, ref IntVector2 intVector)
                {
                    // Plugin.Log("Um...");
                    if (WallClimbManager.TryGetManager(player.abstractCreature, out var WCM))
                    {
                        // Plugin.Log("Wall Climb Extend Check : "+ WCM.wallClimbCount +" / "+ WCM.wallClimbExtend +" / "+ (WCM.MaxWallClimbCount - WCM.WallClimbExtendFrames) +" / "+ WCM.IntDirectionalInput.y);
                        if (player.bodyMode == Player.BodyModeIndex.WallClimb && WCM.IntDirectionalInput.y == -1 && WCM.wallGrip == 1f)
                        {
                            intVector = new IntVector2(0, -1);
                            WCM.wallGrip = 0f;
                        }
                        if (
                            WCM.wallClimbCount > 0 &&
                            !WCM.wallClimbExtend &&
                            WCM.wallClimbCount >= WCM.MaxWallClimbCount - WCM.WallClimbExtendFrames &&
                            WCM.IntDirectionalInput.y == -1
                            )
                        {
                            WCM.wallClimbCount = WCM.MaxWallClimbExtendCount;
                            WCM.wallClimbExtend = true;
                            player.room.AddObject(
                                new ExplosionSpikes(
                                    player.room, player.bodyChunks[1].pos + new Vector2(0f, -40f),
                                    6, 5.5f, 4f, 4.5f, 21f, new Color(1f, 1f, 1f, 0.25f)));
				            player.room.PlaySound(SoundID.Slugcat_Rocket_Jump, player.mainBodyChunk, false, 1f, 1f);
                            Plugin.Log("Wall Climb Extend !!");
                        }
                    }
                }

                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloca_S, (byte)0);
                cursor.EmitDelegate(WallClimbExtend);
                // cursor.Emit(OpCodes.Brfalse, cursor.Next);

                // var ctor = typeof(IntVector2).GetConstructor(new[] { typeof(int), typeof(int) });
                // if (ctor == null) { Plugin.logger.LogError("IntVector2(int, int) constructor not found."); }
                // else
                // {
                //     cursor.Emit(OpCodes.Ldloca_S, (byte)0);
                //     cursor.Emit(OpCodes.Ldc_I4_0);
                //     cursor.Emit(OpCodes.Ldc_I4_M1);
                //     cursor.Emit(OpCodes.Call, typeof(IntVector2).GetConstructor(new[] { typeof(int), typeof(int) }));
                // }
                // Plugin.Log(il);
            }
            else
            {
                Plugin.logger.LogError("Couldn't find IL hook :<");
                Plugin.Log(il);
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("WallClimbManager IL 3 ends");
    }
    private static void Player_WallClimbManager_StopLoopSoundOnGrip(ILContext il)
    {
        Plugin.Log("WallClimbManager IL 4 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<SoundID>(nameof(SoundID.None)),
                x => x.MatchStloc(0)
            ) &&
            cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.bodyMode)),
                x => x.MatchLdsfld<Player.BodyModeIndex>(nameof(Player.BodyModeIndex.WallClimb)),
                x => x.MatchCall(out _)
            ))
            {
                static bool IsGrippingOntoWall(bool orig, Player player)
                {
                    if (orig && WallClimbManager.TryGetManager(player.abstractCreature, out var WCM))
                    {
                        return orig && !(WCM.wallGrip > 0.8f && WCM.IntDirectionalInput.y == 1);
                    }
                    return orig;
                }
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(IsGrippingOntoWall);
            }
            else
            {
                Plugin.logger.LogError("Couldn't find IL hook :<");
                Plugin.Log(il);
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("WallClimbManager IL 4 ends");
    }

}