using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest;

public abstract class AdditionnalTechManager<TSelf> : BTWManager<TSelf> where TSelf : AdditionnalTechManager<TSelf>
{
    public AdditionnalTechManager(AbstractCreature abstractCreature) : base(abstractCreature)
    {
        this.abstractPlayer = abstractCreature;
    }

    public Vector2 GetDirInput(uint index = 0, bool takePupeterInput = false)
    {
        Player pupeter = this.RealizedPlayerInControl;
        Player player = takePupeterInput && pupeter != null ? pupeter : this.RealizedPlayer;
        return BTWFunc.GetDirInput(player, index);
    }
    public IntVector2 GetIntDirInput(uint index = 0, bool takePupeterInput = false)
    {
        Player pupeter = this.RealizedPlayerInControl;
        Player player = takePupeterInput && pupeter != null ? pupeter : this.RealizedPlayer;
        return BTWFunc.GetIntDirInput(player, index);
    }

    public override void Update()
    {
        base.Update();
        if (this.RealizedPlayer is Player player && player.room is Room room)
        {
            if (BTWPlugin.meadowEnabled)
            {
                if (player != null
                    && MeadowFunc.TryGetBottomPlayer(player, out Player pupeter))
                {
                    abstractPlayerInControl = pupeter.abstractCreature;
                }
                else
                {
                    abstractPlayerInControl = null;
                }
            }
            if (this.debugEnabled)
            {
                if (this.debugCircle != null && (room == null || this.debugCircle.room != room))
                {
                    this.debugCircle.room?.RemoveObject( this.debugCircle );
                    this.debugCircle.Destroy();
                    this.debugCircle = null;
                }
                else if (room != null)
                {
                    if (this.debugCircle == null)
                    {
                        this.debugCircle = new();
                        room.AddObject( this.debugCircle );
                    }
                    this.debugCircle.visible = this.debugCircleVisible;
                }
            }
            else if (this.debugCircle != null)
            {
                room?.RemoveObject( this.debugCircle );
                this.debugCircle.Destroy();
                this.debugCircle = null;
            }
        }
    }
    public virtual void ResetPlayerCustomStates()
    {
        Player player = this.RealizedPlayer;
        if (player != null)
        {
            if (AbstractEnergyCore.TryGetCore(player.abstractCreature, out var core))
            {
                core.boostingCount = -20;
            }
            if (this is not WallClimbManager && WallClimbManager.TryGetManager(player.abstractCreature, out var WCM))
            {
                WCM.flipFromWallKick = false;
                WCM.rocketJumpFromWallKick = false;
                WCM.rocketJumpFromWallVerticalPounce = false;
            }
            if (this is not PoleKickManager && PoleKickManager.TryGetManager(player.abstractCreature, out var PKM))
            {
                PKM.poleTechCooldown.Reset(10);
                PKM.isPolePounce = false;
                PKM.bodyInFrontOfPole = false;
                PKM.kickWhiff.Reset();
                PKM.kickActive.Reset();
                PKM.kickKaizo.Reset();
                PKM.kickPole.Reset();
                PKM.CancelPoolLoop();
            }
            if (this is not BTWPlayerData && BTWPlayerData.TryGetManager(player.abstractCreature, out var BTWPD))
            {
                BTWPD.isSuperLaunchJump = false;
            }
        }
    }
    // ------ Variables

    // Objects
    public AbstractCreature abstractPlayer;
    public AbstractCreature abstractPlayerInControl = null;
    
    public DebugCircle debugCircle;

    // Basic
    public bool debugCircleVisible = false;
    public bool debugEnabled = false;
    
    // Get - Set
    public Player RealizedPlayer
    {
        get
        {
            return this.abstractPlayer?.realizedCreature as Player;
        }
    }
    public Player RealizedPlayerInControl
    {
        get
        {
            if (this.abstractPlayerInControl != null
                && this.abstractPlayerInControl.realizedCreature != null
                && this.abstractPlayerInControl.realizedCreature is Player player)
            {
                return player;
            }
            return this.RealizedPlayer;
        }
    }
    public Vector2 DirectionalInput
    {
        get
        {
            return GetDirInput();
        }
    }
    public IntVector2 IntDirectionalInput
    {
        get
        {
            return GetIntDirInput();
        }
    }
    public bool Landed
    {
        get
        {
            Player player = this.RealizedPlayer;
            if (player == null) { return false; }
            return player.canJump > 0 
                || player.bodyMode == Player.BodyModeIndex.CorridorClimb 
                || player.bodyMode == Player.BodyModeIndex.Swimming;
        }
    }
    public int MovementDirection
    {
        get
        {
            Player player = this.RealizedPlayer;
            if (player != null)
            {
                return (int)Mathf.Sign(player.bodyChunks[0].vel.x + player.bodyChunks[1].vel.x);
            }
            return 0;
        }
    }
}