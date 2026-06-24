using System;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest.Items;
using UnityEngine;
using System.Runtime.CompilerServices;
using BeyondTheWest.MSCCompat;
using BeyondTheWest.ArenaAddition;

namespace BeyondTheWest.MeadowCompat.Data;

public class OnlineArenaShield : OnlineEntity
{
    public class OnlineArenaShieldDefinition : EntityDefinition
    {
        [OnlineField]
        public int shieldTime;

        public OnlineArenaShieldDefinition() { }

        public OnlineArenaShieldDefinition(OnlineArenaShield onlineArenaShield, OnlineResource inResource) : base(onlineArenaShield, inResource)
        {
            this.shieldTime = onlineArenaShield.arenaShield.shieldTime;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
        {
            return new OnlineArenaShield(this, inResource, (OnlineArenaShieldState)initialState);
        }
    }
    
    // Some essential values right there
    public readonly ArenaShield arenaShield;
    public static ConditionalWeakTable<ArenaShield, OnlineArenaShield> map = new();
    public RoomSession roomSession => this.currentlyJoinedResource as RoomSession;
    
    // OnlineArenaShield <-> ArenaShield smt smt
    public static OnlineArenaShield RegisterArenaShield(ArenaShield arenaShield)
    {
        OnlineArenaShield newAL = NewFromArenaShield(arenaShield);
        RainMeadow.RainMeadow.Debug($"Registered new arenaShield for Player <{arenaShield.target}>");
        return newAL;
    }
    public static OnlineArenaShield NewFromArenaShield(ArenaShield arenaShield)
    {
        EntityId entityId = new(OnlineManager.mePlayer.inLobbyId, (EntityId.IdType)101, BTWFunc.GetPlayerArenaNumber(arenaShield.target));
        if (OnlineManager.recentEntities.ContainsKey(entityId))
        {
            throw new DuplicateWaitObjectException($"entity with repeated ArenaShield ID: {entityId}");
        }

        return new OnlineArenaShield(arenaShield, entityId, OnlineManager.mePlayer, false);
    }
    protected ArenaShield ArenaShieldFromDef(OnlineArenaShieldDefinition newObjectEvent, OnlineResource inResource, OnlineArenaShieldState initialState)
    {
        ArenaShield arenaShield = new(newObjectEvent.shieldTime)
        {
            life = initialState.life,
            destruction = initialState.destruction,
            target = initialState.onlinePlayerCreature?.abstractCreature?.realizedCreature as Player
        };
        return arenaShield;
    }

    // FINALLY the ctor
    public OnlineArenaShield(ArenaShield arenaShield, EntityId id, OnlinePlayer owner, bool isTransferable)
        : base(id, owner, isTransferable)
    {
        this.arenaShield = arenaShield;
        map.Add(arenaShield, this);
    }
    static public bool creatingRemoteObject { get; private set; } = false;
    public OnlineArenaShield(OnlineArenaShieldDefinition entityDefinition, OnlineResource inResource, OnlineArenaShieldState initialState) 
        : base(entityDefinition, inResource, initialState)
    {
        bool oldCreatingRemoteObject = creatingRemoteObject;
        creatingRemoteObject = true;
        try
        {
            this.arenaShield = ArenaShieldFromDef(entityDefinition, inResource, initialState);
        }
        catch (Exception)
        {
            creatingRemoteObject = oldCreatingRemoteObject;
            throw;
        }
        creatingRemoteObject = oldCreatingRemoteObject; 

        map.Add(this.arenaShield, this);
    }

    // All the functions
    public override EntityDefinition MakeDefinition(OnlineResource onlineResource)
    {
        return new OnlineArenaShieldDefinition(this, onlineResource);
    }
    public override void NewOwner(OnlinePlayer newOwner)
    {
        base.NewOwner(newOwner);
    }
    protected override EntityState MakeState(uint tick, OnlineResource inResource)
    {
        return new OnlineArenaShieldState(this, inResource, tick);
    }
    
    // leaving and entering
    protected override void JoinImpl(OnlineResource inResource, EntityState initialState)
    {
        RainMeadow.RainMeadow.Debug($"{this} joining {inResource}");
        try
        {
            if (inResource is RoomSession newRoom)
            {
                RainMeadow.RainMeadow.Debug($"room join");
                newRoom.absroom.realizedRoom?.AddObject(this.arenaShield);
            }
        }
        catch (Exception e)
        {
            RainMeadow.RainMeadow.Error(e);
        }
    }
    public void RemoveEntityFromRoom(bool onlineaware = true)
    {
        RainMeadow.RainMeadow.Debug("Removing Arena Shield from room: " + this);
        if (this.arenaShield.room is Room room)
        {
            room.RemoveObject(this.arenaShield);
            room.CleanOutObjectNotInThisRoom(this.arenaShield);
        }
    }

    protected override void LeaveImpl(OnlineResource inResource)
    {
        RainMeadow.RainMeadow.Debug($"{this} leaving {inResource}");
        try
        {
            if (inResource is RoomSession rs)
            {
                RemoveEntityFromRoom(true);
            }
        }
        catch (Exception e)
        {
            RainMeadow.RainMeadow.Error(e);
            this.arenaShield.RemoveFromRoom();
        }
    }
    public override void Deregister()
    {
        base.Deregister();
        RainMeadow.RainMeadow.Debug("Removing Arena Shield from OnlineArenaShield.map: " + this);
        this.arenaShield.Destroy();
        map.Remove(this.arenaShield);
    }
    
    // RPCs
    [RPCMethod]
    public static void DismissArenaShield(OnlineArenaShield onlineArenaShield)
    {
        if (onlineArenaShield is null 
            || onlineArenaShield.isMine 
            || onlineArenaShield.arenaShield is not ArenaShield shield) { return; }

        shield.Dismiss(false);

        BTWPlugin.Log("Arena Forcefield Dismissed by "+ shield.target +" !");
    }
}