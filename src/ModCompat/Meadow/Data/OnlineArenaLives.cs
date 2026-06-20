using System;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest.Items;
using UnityEngine;
using System.Runtime.CompilerServices;
using BeyondTheWest.MSCCompat;
using BeyondTheWest.ArenaAddition;

namespace BeyondTheWest.MeadowCompat.Data;

public class OnlineArenaLives : OnlineEntity
{
    public class OnlineArenaLivesDefinition : EntityDefinition
    {
        [OnlineField]
        public int playerID;
        [OnlineField]
        public int lifes;
        [OnlineField]
        public int reviveTime;
        [OnlineField]
        public int reviveAdditionnalTime;
        [OnlineField]
        public bool blockArenaOut;

        public OnlineArenaLivesDefinition() { }

        public OnlineArenaLivesDefinition(OnlineArenaLives OnlineArenaLives, OnlineResource inResource) : base(OnlineArenaLives, inResource)
        {
            this.playerID = OnlineArenaLives.arenalives.playerID;
            this.lifes = OnlineArenaLives.arenalives.lifes;
            this.reviveTime = OnlineArenaLives.arenalives.reviveTime;
            this.reviveAdditionnalTime = OnlineArenaLives.arenalives.reviveAdditionnalTime;
            this.blockArenaOut = OnlineArenaLives.arenalives.blockArenaOut;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
        {
            return new OnlineArenaLives(this, inResource, (OnlineArenaLivesState)initialState);
        }
    }
    
    // Some essential values right there
    public readonly ArenaLives arenalives;
    public static ConditionalWeakTable<ArenaLives, OnlineArenaLives> map = new();
    public RoomSession roomSession => this.currentlyJoinedResource as RoomSession;
    
    // OnlineArenaLives <-> ArenaLives smt smt
    public static OnlineArenaLives RegisterArenaLives(ArenaLives arenalives)
    {
        OnlineArenaLives newAL = NewFromArenaLives(arenalives);
        RainMeadow.RainMeadow.Debug($"Registered new arena lives for Player <{arenalives.playerID}>");
        return newAL;
    }
    public static OnlineArenaLives NewFromArenaLives(ArenaLives arenalives)
    {
        EntityId entityId = new(OnlineManager.mePlayer.inLobbyId, (EntityId.IdType)102, arenalives.playerID);
        if (OnlineManager.recentEntities.ContainsKey(entityId))
        {
            throw new DuplicateWaitObjectException($"entity with repeated ArenaLives ID: {entityId}");
        }

        return new OnlineArenaLives(arenalives, entityId, OnlineManager.mePlayer, false);
    }
    protected ArenaLives ArenaLivesFromDef(OnlineArenaLivesDefinition newObjectEvent, OnlineResource inResource, OnlineArenaLivesState initialState)
    {
        ArenaLives arenaLives = new(newObjectEvent.playerID, 
            newObjectEvent.lifes, 
            newObjectEvent.reviveTime, 
            newObjectEvent.reviveAdditionnalTime, 
            newObjectEvent.blockArenaOut, true)
        {
            lifesleft = initialState.lifesleft,
            countedAlive = initialState.countedAlive,
            reinforced = initialState.reinforced,
            reviveCounter = initialState.reviveCounter,
            killChain = initialState.killChain,
            livesDisplayCounter = initialState.livesDisplayCounter,
            circlesAmount = initialState.circlesAmount,
            respawnPos = new Vector2(initialState.respawnPosX, initialState.respawnPosY),
            respawnExit = initialState.respawnExit
        };
        return arenaLives;
    }

    // FINALLY the ctor
    public OnlineArenaLives(ArenaLives arenalives, EntityId id, OnlinePlayer owner, bool isTransferable)
        : base(id, owner, isTransferable)
    {
        this.arenalives = arenalives;
        map.Add(arenalives, this);
    }
    static public bool creatingRemoteObject { get; private set; } = false;
    public OnlineArenaLives(OnlineArenaLivesDefinition entityDefinition, OnlineResource inResource, OnlineArenaLivesState initialState) 
        : base(entityDefinition, inResource, initialState)
    {
        bool oldCreatingRemoteObject = creatingRemoteObject;
        creatingRemoteObject = true;
        try
        {
            this.arenalives = ArenaLivesFromDef(entityDefinition, inResource, initialState);
        }
        catch (Exception)
        {
            creatingRemoteObject = oldCreatingRemoteObject;
            throw;
        }
        creatingRemoteObject = oldCreatingRemoteObject; 

        map.Add(this.arenalives, this);
    }

    // All the functions
    public override EntityDefinition MakeDefinition(OnlineResource onlineResource)
    {
        return new OnlineArenaLivesDefinition(this, onlineResource);
    }
    public override void NewOwner(OnlinePlayer newOwner)
    {
        base.NewOwner(newOwner);
        this.arenalives.fake = !newOwner.isMe;
    }
    protected override EntityState MakeState(uint tick, OnlineResource inResource)
    {
        return new OnlineArenaLivesState(this, inResource, tick);
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
                newRoom.absroom.realizedRoom?.AddObject(this.arenalives);
            }
        }
        catch (Exception e)
        {
            RainMeadow.RainMeadow.Error(e);
        }
    }
    public void RemoveEntityFromRoom(bool onlineaware = true)
    {
        RainMeadow.RainMeadow.Debug("Removing Arena Lifes from room: " + this);
        if (this.arenalives.room is Room room)
        {
            room.RemoveObject(this.arenalives);
            room.CleanOutObjectNotInThisRoom(this.arenalives);
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
            this.arenalives.RemoveFromRoom();
        }
    }
    public override void Deregister()
    {
        base.Deregister();
        RainMeadow.RainMeadow.Debug("Removing Arena Lifes from OnlineArenaLives.map: " + this);
        this.arenalives.Destroy();
        map.Remove(this.arenalives);
    }
    
    // RPCs
}