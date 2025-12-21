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
        return new();
    }
}