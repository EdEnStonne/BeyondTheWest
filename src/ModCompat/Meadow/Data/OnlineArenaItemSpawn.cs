using System;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest.Items;
using UnityEngine;
using System.Runtime.CompilerServices;
using BeyondTheWest.MSCCompat;
using BeyondTheWest.ArenaAddition;
using System.Linq;

namespace BeyondTheWest.MeadowCompat.Data;

public class OnlineArenaItemSpawn : OnlineEntity
{
    public class OnlineArenaItemSpawnDefinition : EntityDefinition
    {
        [OnlineField]
        public Vector2 position;
        [OnlineField]
        public int spawnTime;
        [OnlineField]
        public OnlineObjectDataList onlineObjectList;

        public OnlineArenaItemSpawnDefinition() { }

        public OnlineArenaItemSpawnDefinition(OnlineArenaItemSpawn onlineArenaItemSpawn, OnlineResource inResource) : base(onlineArenaItemSpawn, inResource)
        {
            this.position = onlineArenaItemSpawn.itemSpawn.pos;
            this.spawnTime = onlineArenaItemSpawn.itemSpawn.spawnTime;
            this.onlineObjectList = new(onlineArenaItemSpawn.itemSpawn.objectList);
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
        {
            return new OnlineArenaItemSpawn(this, inResource, (OnlineArenaItemSpawnState)initialState);
        }
    }
    
    // Some essential values right there
    public readonly ArenaItemSpawn itemSpawn;
    public const string spawningLock = "forceSpawn";
    public static ConditionalWeakTable<ArenaItemSpawn, OnlineArenaItemSpawn> map = new();
    public static int NextID;
    public RoomSession roomSession => this.currentlyJoinedResource as RoomSession;
    
    // OnlineArenaItemSpawn <-> ArenaItemSpawn smt smt
    public static OnlineArenaItemSpawn RegisterArenaItemSpawn(ArenaItemSpawn itemSpawn)
    {
        OnlineArenaItemSpawn newAL = NewFromArenaItemSpawn(itemSpawn);
        RainMeadow.RainMeadow.Debug($"Registered new itemSpawn at <{itemSpawn.pos}>");
        return newAL;
    }
    public static OnlineArenaItemSpawn NewFromArenaItemSpawn(ArenaItemSpawn itemSpawn)
    {
        EntityId entityId = new(OnlineManager.mePlayer.inLobbyId, (EntityId.IdType)103, ++NextID);
        if (OnlineManager.recentEntities.ContainsKey(entityId))
        {
            throw new DuplicateWaitObjectException($"entity with repeated ArenaItemSpawn ID: {entityId}");
        }

        return new OnlineArenaItemSpawn(itemSpawn, entityId, OnlineManager.mePlayer, false);
    }
    protected ArenaItemSpawn ArenaItemSpawnFromDef(OnlineArenaItemSpawnDefinition newObjectEvent, OnlineResource inResource, OnlineArenaItemSpawnState initialState)
    {
        ArenaItemSpawn itemSpawn = new(newObjectEvent.position, newObjectEvent.spawnTime, newObjectEvent.onlineObjectList.objectList.ToList(), true)
        {
            spawnCount = initialState.spawnCount,
            destructionCount = initialState.destructionCount
        };
        return itemSpawn;
    }

    // FINALLY the ctor
    public OnlineArenaItemSpawn(ArenaItemSpawn itemSpawn, EntityId id, OnlinePlayer owner, bool isTransferable)
        : base(id, owner, isTransferable)
    {
        this.itemSpawn = itemSpawn;
        map.Add(itemSpawn, this);
    }
    static public bool creatingRemoteObject { get; private set; } = false;
    public OnlineArenaItemSpawn(OnlineArenaItemSpawnDefinition entityDefinition, OnlineResource inResource, OnlineArenaItemSpawnState initialState) 
        : base(entityDefinition, inResource, initialState)
    {
        bool oldCreatingRemoteObject = creatingRemoteObject;
        creatingRemoteObject = true;
        try
        {
            this.itemSpawn = ArenaItemSpawnFromDef(entityDefinition, inResource, initialState);
        }
        catch (Exception)
        {
            creatingRemoteObject = oldCreatingRemoteObject;
            throw;
        }
        creatingRemoteObject = oldCreatingRemoteObject; 

        map.Add(this.itemSpawn, this);
    }

    // All the functions
    public override EntityDefinition MakeDefinition(OnlineResource onlineResource)
    {
        return new OnlineArenaItemSpawnDefinition(this, onlineResource);
    }
    public override void NewOwner(OnlinePlayer newOwner)
    {
        base.NewOwner(newOwner);
    }
    protected override EntityState MakeState(uint tick, OnlineResource inResource)
    {
        return new OnlineArenaItemSpawnState(this, inResource, tick);
    }
    
    // leaving and entering
    protected override void JoinImpl(OnlineResource inResource, EntityState initialState)
    {
        RainMeadow.RainMeadow.Debug($"{this}<{itemSpawn.pos}> joining {inResource}");
        try
        {
            if (inResource is RoomSession newRoom)
            {
                RainMeadow.RainMeadow.Debug($"room join");
                newRoom.absroom.realizedRoom?.AddObject(this.itemSpawn);
            }
        }
        catch (Exception e)
        {
            RainMeadow.RainMeadow.Error(e);
        }
    }
    public void RemoveEntityFromRoom(bool onlineaware = true)
    {
        RainMeadow.RainMeadow.Debug($"Removing Item Spawn at <{itemSpawn.pos}> from room: " + this);
        if (this.itemSpawn.room is Room room)
        {
            room.RemoveObject(this.itemSpawn);
            room.CleanOutObjectNotInThisRoom(this.itemSpawn);
        }
    }

    protected override void LeaveImpl(OnlineResource inResource)
    {
        RainMeadow.RainMeadow.Debug($"{this}<{itemSpawn.pos}> leaving {inResource}");
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
            this.itemSpawn.RemoveFromRoom();
        }
    }
    public override void Deregister()
    {
        base.Deregister();
        RainMeadow.RainMeadow.Debug($"Removing Item Spawn at <{itemSpawn.pos}> from OnlineArenaItemSpawn.map: " + this);
        this.itemSpawn.Destroy();
        map.Remove(this.itemSpawn);
    }
    
    // RPCs
    [RPCMethod]
    public static void ForceSpawnItem(OnlineArenaItemSpawn onlineArenaItemSpawn)
    {
        if (onlineArenaItemSpawn is null 
            || !onlineArenaItemSpawn.isMine 
            || onlineArenaItemSpawn.itemSpawn is not ArenaItemSpawn itemSpawn
            || itemSpawn.spawnCount >= itemSpawn.spawnTime) { return; }

        itemSpawn.SpawnItems();

        BTWPlugin.Log($"Arena ItemSpawn at <{itemSpawn.pos}> forced to spawn by RPC.");
    }
}