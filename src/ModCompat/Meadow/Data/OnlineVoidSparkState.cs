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
    public int lifetime = 0;

    public OnlineVoidSparkState() : base() { }
    public OnlineVoidSparkState(OnlineVoidSpark onlineVoidSpark, OnlineResource inResource, uint ts) : base(onlineVoidSpark, inResource, ts)
    {
        this.position = onlineVoidSpark.voidSpark.position;
        this.lastPosition = onlineVoidSpark.voidSpark.lastPosition;
        this.lifetime = onlineVoidSpark.voidSpark.lifetime;
    }

    public override void ReadTo(OnlineEntity onlineEntity)
    {
        base.ReadTo(onlineEntity);

        var onlineVoidSpark = onlineEntity as OnlineVoidSpark;
        onlineVoidSpark.voidSpark.position = this.position;
        onlineVoidSpark.voidSpark.lastPosition = this.lastPosition;
        onlineVoidSpark.voidSpark.lifetime = this.lifetime;
    }
}