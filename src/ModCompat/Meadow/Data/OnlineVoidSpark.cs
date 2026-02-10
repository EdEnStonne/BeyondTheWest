using System;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest.Items;
using UnityEngine;
using System.Runtime.CompilerServices;
using BeyondTheWest.MSCCompat;

namespace BeyondTheWest.MeadowCompat.Data;

public class OnlineVoidSpark : OnlineEntity // mostly copied from OnlinePhysicalObject 
{
    // Void Spark definition (values that don't change mostly)
    public class OnlineVoidSparkDefinition : EntityDefinition
    {
        [OnlineField]
        public int VoidSparkID = -1;
        [OnlineFieldColorRgb]
        public Color color = new(1f, 0.6f, 0.7f);
        [OnlineFieldHalf]
        public float damage = 0;

        public OnlineVoidSparkDefinition() { }

        public OnlineVoidSparkDefinition(OnlineVoidSpark onlineVoidSpark, OnlineResource inResource) : base(onlineVoidSpark, inResource)
        {
            this.VoidSparkID = onlineVoidSpark.voidSpark.ID;
            this.color = onlineVoidSpark.voidSpark.color;
            this.damage = onlineVoidSpark.voidSpark.damage;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
        {
            return new OnlineVoidSpark(this, inResource, (OnlineVoidSparkState)initialState);
        }
    }
    
    // Some essential values right there
    public readonly VoidSpark voidSpark;
    public static ConditionalWeakTable<VoidSpark, OnlineVoidSpark> map = new();
    public RoomSession roomSession => this.currentlyJoinedResource as RoomSession;
    
    // OnlineVoidSpark <-> VoidSpark smt smt
    public static OnlineVoidSpark RegisterVoidSpark(VoidSpark voidSpark)
    {
        OnlineVoidSpark newOVS = NewFromVoidSpark(voidSpark);
        RainMeadow.RainMeadow.Debug($"Registered new voidSpark <{voidSpark.ID}>");
        return newOVS;
    }
    public static OnlineVoidSpark NewFromVoidSpark(VoidSpark voidSpark)
    {
        bool transferable = !RainMeadow.RainMeadow.sSpawningAvatar;

        EntityId entityId = new EntityId(OnlineManager.mePlayer.inLobbyId, EntityId.IdType.custom, voidSpark.ID);
        if (OnlineManager.recentEntities.ContainsKey(entityId))
        {
            RainMeadow.RainMeadow.Error($"entity with repeated VoidSpark ID: {entityId}");
            voidSpark.ID = VoidSpark.NewID();
            entityId = new EntityId(OnlineManager.mePlayer.inLobbyId, EntityId.IdType.custom, voidSpark.ID);
            RainMeadow.RainMeadow.Error($"set as: {entityId}");
        }

        return new OnlineVoidSpark(voidSpark, entityId, OnlineManager.mePlayer, transferable);
    }
    protected virtual VoidSpark VoidSparkFromDef(OnlineVoidSparkDefinition newObjectEvent, OnlineResource inResource, OnlineVoidSparkState initialState)
    {
        VoidSpark voidSpark = new(initialState.position, newObjectEvent.damage, initialState.lifetime, true)
        {
            lastPosition = initialState.lastPosition,
            color = newObjectEvent.color,
            ID = newObjectEvent.VoidSparkID
        };
        return voidSpark;
    }

    // FINALLY the ctor
    public OnlineVoidSpark(VoidSpark voidSpark, EntityId id, OnlinePlayer owner, bool isTransferable)
        : base(id, owner, isTransferable)
    {
        this.voidSpark = voidSpark;
        map.Add(voidSpark, this);
    }
    static public bool creatingRemoteObject { get; private set; } = false;
    public OnlineVoidSpark(OnlineVoidSparkDefinition entityDefinition, OnlineResource inResource, OnlineVoidSparkState initialState) 
        : base(entityDefinition, inResource, initialState)
    {
        bool oldCreatingRemoteObject = creatingRemoteObject;
        creatingRemoteObject = true;
        try
        {
            this.voidSpark = VoidSparkFromDef(entityDefinition, inResource, initialState);
        }
        catch (Exception)
        {
            creatingRemoteObject = oldCreatingRemoteObject;
            throw;
        }
        creatingRemoteObject = oldCreatingRemoteObject; 

        map.Add(this.voidSpark, this);
    }

    // All the functions
    public override EntityDefinition MakeDefinition(OnlineResource onlineResource)
    {
        return new OnlineVoidSparkDefinition(this, onlineResource);
    }
    public override void NewOwner(OnlinePlayer newOwner)
    {
        base.NewOwner(newOwner);
        this.voidSpark.fake = !newOwner.isMe;
    }
    protected override EntityState MakeState(uint tick, OnlineResource inResource)
    {
        return new OnlineVoidSparkState(this, inResource, tick);
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
                newRoom.absroom.realizedRoom?.AddObject(this.voidSpark);
            }
        }
        catch (Exception e)
        {
            RainMeadow.RainMeadow.Error(e);
        }
    }
    public void RemoveEntityFromRoom(bool onlineaware = true)
    {
        RainMeadow.RainMeadow.Debug("Removing Void Spark from room: " + this);
        if (this.voidSpark.room is Room room)
        {
            room.RemoveObject(this.voidSpark);
            room.CleanOutObjectNotInThisRoom(this.voidSpark);
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
            this.voidSpark.RemoveFromRoom();
        }
    }
    public override void Deregister()
    {
        base.Deregister();
        RainMeadow.RainMeadow.Debug("Removing Void Spark from OnlineVoidSpark.map: " + this);
        map.Remove(this.voidSpark);
    }
    
    // RPCs
    [RPCMethod]
    public void Dissipate(Vector2 pos)
    {
        if (this.voidSpark is null) return;
        if (this.voidSpark.room == null)
        {
            RainMeadow.RainMeadow.Error($"Trying to dissipate VoidSpark<{this.voidSpark.ID}> while the object has no room !");
            return;
        }
        this.voidSpark.position = pos;
        this.voidSpark.Dissipate();
    }
    
    [RPCMethod]
    public void Explode(Vector2 pos)
    {
        if (this.voidSpark is null) return;
        if (this.voidSpark.room == null)
        {
            RainMeadow.RainMeadow.Error($"Trying to explode VoidSpark<{this.voidSpark.ID}> while the object has no room !");
            return;
        }
        this.voidSpark.position = pos;
        this.voidSpark.HitWall();
    }
    
    [RPCMethod]
    public static void HitSomething(OnlineVoidSpark onlineVoidSpark, OnlineEntity onlineTarget, ushort damageCent, Vector2 direction, Vector2 lastPosition, OnlineCreature onlineKilltagholder)
    {
        if (onlineVoidSpark?.voidSpark is null || onlineVoidSpark?.voidSpark?.room == null)
        {
            HitSomethingSparkless(onlineTarget, damageCent, direction, lastPosition, onlineKilltagholder);
        }
        else
        {
            UpdatableAndDeletable target = null;
            AbstractCreature killtagholder = null;
            
            if (onlineTarget is OnlinePhysicalObject onlinePhysicalObject)
            {
                target = onlinePhysicalObject.apo?.realizedObject;
            }
            if (onlineKilltagholder is not null)
            {
                killtagholder = onlineKilltagholder.abstractCreature;
            }
            
            if (target is null) return;

            onlineVoidSpark.voidSpark.target = target;
            onlineVoidSpark.voidSpark.killTagHolder = killtagholder;
            onlineVoidSpark.voidSpark.direction = direction;
            onlineVoidSpark.voidSpark.position = VoidSpark.GetPosition(target);
            onlineVoidSpark.voidSpark.damage = damageCent / 100f;
            onlineVoidSpark.voidSpark.HitObject();
        }
    }
    [RPCMethod]
    public static void HitSomethingSparkless(OnlineEntity onlineTarget, ushort damageCent, Vector2 direction, Vector2 lastPosition, OnlineCreature onlineKilltagholder)
    {
        Room room = null;
        UpdatableAndDeletable target = null;
        AbstractCreature killtagholder = null;
        float damage = damageCent / 100f;
        
        if (onlineTarget is OnlinePhysicalObject onlinePhysicalObject)
        {
            target = onlinePhysicalObject.apo?.realizedObject;
            room = target?.room;
        }
        if (onlineKilltagholder is not null)
        {
            killtagholder = onlineKilltagholder.abstractCreature;
        }
        
        if (target is null || room is null) return;
        
        Vector2 position = VoidSpark.GetPosition(target);
        VoidSpark. MakeDraggedSparks(room, 25f + 10f * damage, position, 
            (byte)(BTWFunc.RandInt(15, 25) + damage), VoidSpark.defaultColor, 0.2f);
        
        BTWPlugin.Log($"VoidSpark (that was too quick to realize) hit [{target}] for <{damage}> dmg !");

        if (ModManager.MSC)
        {
            LightingArc arc = new(position, lastPosition, 
                    damage / 2f, 0.5f + Mathf.Log10(1 + damage), 10, VoidSpark.defaultColor);
            room.AddObject(arc);
        }
        VoidSpark.HitSomethingWithVoidSpark(target, damage, direction, killtagholder);
    }
}