using System;
using System.Collections.Generic;
using BeyondTheWest;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;

namespace BeyondTheWest.ArenaAddition;
public class ObjectDataPool
{
    public struct ObjectDataPoolItem
    {
        public ObjectData objectData = new();
        public int weight = 1;
        public ObjectDataPoolItem() { }
        public ObjectDataPoolItem(ObjectData objectData)
        {
            this.objectData = objectData;
        }
        public ObjectDataPoolItem(ObjectData objectData, int weight) : this(objectData)
        {
            this.weight = weight;
        }
    }
    public List<ObjectDataPoolItem> pool = new();
    public int totalWeight = 0;
    public ObjectDataPool() { }
    public ObjectDataPool(List<ObjectData> objectList)
    {
        foreach (var objectData in objectList)
        {
            AddToPool(new(objectData, 1));
        }
    }
    public ObjectDataPool(ObjectDataPool objectDataPool)
    {
        foreach (var objectDataPollItem in objectDataPool.pool)
        {
            AddToPool(objectDataPollItem);
        }
    }
    public void AddToPool(ObjectDataPoolItem objectDataPoolItem)
    {
        this.totalWeight += objectDataPoolItem.weight;
        this.pool.Add(objectDataPoolItem);
    }
    public void AddToPool(ObjectData objectData, int weight = 1)
    {
        this.totalWeight += weight;
        this.pool.Add(new(objectData, weight));
    }
    public void AddToPool(ObjectType objectType, int weight = 1)
    {
        this.totalWeight += weight;
        this.pool.Add(new(new(objectType), weight));
    }
    public void AddToPool(ObjectType objectType, int intData, int weight = 1)
    {
        this.totalWeight += weight;
        this.pool.Add(new(new(objectType, intData), weight));
    }
    public void RemoveFromPool(Predicate<ObjectDataPoolItem> match)
    {
        List<ObjectDataPoolItem> toRemove = this.pool.FindAll(match);
        for (int i = 0; i < toRemove.Count; i++)
        {
            this.totalWeight -= toRemove[i].weight;
            this.pool.Remove(toRemove[i]);
        }
    }
    public void ReCalcutaleTotalWeight()
    {
        this.totalWeight = 0;
        for (int i = 0; i < this.pool.Count; i++)
        {
            this.totalWeight += this.pool[i].weight;
        }
    }
    public ObjectData Pool()
    {
        if (this.totalWeight > 0)
        {
            float chance = BTWFunc.random * this.totalWeight;
            int currentWeight = 0;
            foreach (ObjectDataPoolItem objectDataPoolItem in this.pool)
            {
                currentWeight += objectDataPoolItem.weight;
                if (currentWeight > chance)
                {
                    return objectDataPoolItem.objectData;
                }
            }
        }
        return new ObjectData();
    }
    public ObjectData[] AllItemsData()
    {
        ObjectData[] list = new ObjectData[this.pool.Count];
        for (int i = 0; i < this.pool.Count; i++)
        {
            list[i] = this.pool[i].objectData;
        }
        return list;
    }
    public List<ObjectType> AllItemsTypes()
    {
        List<ObjectType> list = new();
        for (int i = 0; i < this.pool.Count; i++)
        {
            if (!list.Exists(x => x == this.pool[i].objectData.objectType))
            {
                list.Add(this.pool[i].objectData.objectType);
            }
        }
        return list;
    }
    public void LogPool()
    {
        this.pool.Sort((x,y) => y.weight - x.weight);
        foreach (var item in this.pool)
        {
            BTWPlugin.Log($"    > [{item.objectData.objectType}]<{item.objectData.intData}> : <{item.weight}>");
        }
        BTWPlugin.Log($"Total weight : <{this.totalWeight}>");
    }
    public bool IsEmpty => this.pool.Count == 0;
}