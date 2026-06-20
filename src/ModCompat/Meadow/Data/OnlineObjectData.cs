using System;
using RainMeadow;
using JetBrains.Annotations;
using UnityEngine;
using BeyondTheWest.ArenaAddition;
using static RainMeadow.Serializer;
using System.Collections.Generic;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;

namespace BeyondTheWest.MeadowCompat.Data;

public struct OnlineObjectDataList : ICustomSerializable // thanks invalidunits
{
    public ObjectData[] objectList = {};
    public byte len = 0;

    public OnlineObjectDataList() { }

    public OnlineObjectDataList(List<ObjectData> objectDatas)
    {
        this.objectList = objectDatas.ToArray();
        this.len = (byte)objectDatas.Count;
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
    
    //-------- To make it pass in a State
    public override readonly bool Equals(object obj)
    {
        return obj is OnlineObjectDataList onlineObjectDataList && this == onlineObjectDataList;
    }
    public static bool operator ==(OnlineObjectDataList left, OnlineObjectDataList right)
    {
        if (left.len == right.len)
        {
            for (int i = 0; i < left.objectList.Length; i++)
            {
                if (left.objectList[i] != right.objectList[i]) return false;
            }
            return true;
        }
        return false;
    }
    public static bool operator !=(OnlineObjectDataList left, OnlineObjectDataList right)
    {
        return !(left == right);
    }
    public override int GetHashCode()
    {
        int hash = 0;
        for (int i = 0; i < this.objectList.Length; i++)
        {
            hash += objectList.GetHashCode();
        }
        return this.len.GetHashCode() * hash;
    }
}