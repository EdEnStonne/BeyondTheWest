using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest;

public abstract class AdditionnalTechManager<TSelf> where TSelf : AdditionnalTechManager<TSelf>
{
    public static ConditionalWeakTable<AbstractCreature, TSelf> managers = new();
    public static bool TryGetManager(AbstractCreature creature, out TSelf manager)
    {
        return managers.TryGetValue(creature, out manager);
    }
    public static TSelf GetManager(AbstractCreature creature)
    {
        TryGetManager(creature, out TSelf manager);
        return manager;
    }
    public static void AddNewManager(AbstractCreature creature, TSelf manager)
    {
        RemoveManager(creature);
        managers.Add(creature, manager);
    }
    public static void RemoveManager(AbstractCreature creature)
    {
        if (TryGetManager(creature, out _))
        {
            managers.Remove(creature);
        }
    }
    
    public AdditionnalTechManager(AbstractCreature abstractCreature)
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

    public virtual void Update()
    {
        if (Plugin.meadowEnabled)
        {
            if (RealizedPlayer is Player player 
                && player != null
                && MeadowFunc.TryGetBottomPlayer(player, out Player pupeter))
            {
                abstractPlayerInControl = pupeter.abstractCreature;
            }
            else
            {
                abstractPlayerInControl = null;
            }
        }
    }
    
    // ------ Variables

    // Objects
    public AbstractCreature abstractPlayer;
    public AbstractCreature abstractPlayerInControl = null;

    // Basic
    
    // Get - Set
    public Player RealizedPlayer
    {
        get
        {
            if (this.abstractPlayer != null
                && this.abstractPlayer.realizedCreature != null
                && this.abstractPlayer.realizedCreature is Player player)
            {
                return player;
            }
            return null;
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
    public Room RealizedRoom
    {
        get
        {
            Player player = RealizedPlayer;
            return player?.room;
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
                return (int)Mathf.Sign(player.mainBodyChunk.vel.x);
            }
            return 0;
        }
    }
}