using System;
using RainMeadow;
using JetBrains.Annotations;
using UnityEngine;
using BeyondTheWest.ArenaAddition;
using static RainMeadow.Serializer;
using System.Collections.Generic;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;

namespace BeyondTheWest.MeadowCompat.Data;
public struct OnlineObjectData : ICustomSerializable
{
    public static OnlineObjectData DataToOnline(ObjectData objectData)
    {
        return new(objectData);
    }
    public static List<OnlineObjectData> DataToOnline(List<ObjectData> objectList)
    {
        List<OnlineObjectData> onlineObjectList = new();
        foreach (ObjectData objectData in objectList)
        {
            onlineObjectList.Add( DataToOnline(objectData) );
        }
        return onlineObjectList;
    }
    public static ObjectData OnlineToData(OnlineObjectData onlineObjectData)
    {
        return new(onlineObjectData.objectType, onlineObjectData.intData);
    }
    public static List<ObjectData> OnlineToData(List<OnlineObjectData> onlineObjectList)
    {
        List<ObjectData> objectList = new();
        foreach (OnlineObjectData onlineObjectData in onlineObjectList)
        {
            objectList.Add( OnlineToData(onlineObjectData) );
        }
        return objectList;
    }

    public ObjectType objectType = ObjectType.Rock;
    public byte intData = 0;

    public OnlineObjectData() { }
    public OnlineObjectData(ObjectType objectType)
    {
        this.objectType = objectType;
    }
    public OnlineObjectData(ObjectType objectType, int intData) : this(objectType)
    {
        this.intData = (byte)Mathf.Clamp(intData, byte.MinValue, byte.MaxValue);
    }
    public OnlineObjectData(ObjectData objectData)
    {
        this.intData = (byte)Mathf.Clamp(objectData.intData, byte.MinValue, byte.MaxValue);
        this.objectType = objectData.objectType;
    }

    public void CustomSerialize(Serializer serializer)
    {
        serializer.Serialize(ref this.intData);
        serializer.SerializeExtEnum(ref this.objectType);
    }
}

public struct OnlineObjectDataList : ICustomSerializable // thanks invalidunits
{
    public ObjectData[] objectList = {};
    public uint len = 0;

    public OnlineObjectDataList() { }

    public OnlineObjectDataList(List<ObjectData> objectDatas)
    {
        this.objectList = objectDatas.ToArray();
        this.len = (uint)objectDatas.Count;
    }

    public void CustomSerialize(Serializer serializer)
    {
        if (serializer.IsWriting)
        {  
            serializer.Serialize(ref this.len);
            for (int i = 0; i < len; i++)
            {
               serializer.SerializeExtEnum(ref objectList[i].objectType);
               serializer.Serialize(ref objectList[i].intData);
            }
            
        }
        else if (serializer.IsReading)
        {
             serializer.Serialize(ref this.len);
             objectList = new ObjectData[this.len];
             for (int i = 0; i < this.len; i++)
             {
                 ObjectData data = new();
                 serializer.SerializeExtEnum(ref data.objectType);
                 serializer.Serialize(ref data.intData);
                 objectList[i] = data;
             }
        }
    }
}