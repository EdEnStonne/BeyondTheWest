using System;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest.Items;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace BeyondTheWest.MeadowCompat.Data;

public class OnlineVoidSparkState : OnlineEntity.EntityState
{
    [OnlineField]
    public Vector2 position = Vector2.zero;
    [OnlineField]
    public Vector2 lastPosition = Vector2.zero;
    [OnlineField]
    public Vector2 momentum = Vector2.zero;
    [OnlineField]
    public int lifetime = 0;
    [OnlineField]
    public int destructionTime = 0;
    [OnlineField(nullable:true)]
    public OnlinePhysicalObject onlineTarget = null;
    [OnlineField]
    public bool isTargetEnergyCore = false;

    public OnlineVoidSparkState() : base() { }
    public OnlineVoidSparkState(OnlineVoidSpark onlineVoidSpark, OnlineResource inResource, uint ts) : base(onlineVoidSpark, inResource, ts)
    {
        if (onlineVoidSpark?.voidSpark is not VoidSpark voidSpark) return;
        this.position = voidSpark.position;
        this.lastPosition = voidSpark.lastPosition;
        this.momentum = voidSpark.momentum;
        this.lifetime = voidSpark.lifetime.value;
        this.destructionTime = voidSpark.destructionTime.value;
        this.isTargetEnergyCore = false;
        if ((voidSpark.target as PhysicalObject)?.abstractPhysicalObject?.GetOnlineObject() is OnlinePhysicalObject onlinePhysicalObject)
        {
            this.onlineTarget = onlinePhysicalObject;
        }
        else if ((voidSpark.target as EnergyCore)?.player?.abstractCreature?.GetOnlineCreature() is OnlineCreature onlineCreature)
        {
            this.onlineTarget = onlineCreature;
            this.isTargetEnergyCore = true;
        }
    }

    public override void ReadTo(OnlineEntity onlineEntity)
    {
        base.ReadTo(onlineEntity);
        if ((onlineEntity as OnlineVoidSpark)?.voidSpark is not VoidSpark voidSpark) return;

        voidSpark.position = this.position;
        voidSpark.lastPosition = this.lastPosition;
        voidSpark.momentum = this.momentum;
        voidSpark.lifetime.value = this.lifetime;
        voidSpark.destructionTime.value = this.destructionTime;
        voidSpark.target = this.isTargetEnergyCore 
            ? (this.onlineTarget?.apo?.realizedObject as Player)?.GetAEC()?.RealizedCore
            : this.onlineTarget?.apo?.realizedObject;
    }
}