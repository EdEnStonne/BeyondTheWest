using ObjectType = AbstractPhysicalObject.AbstractObjectType;
using System;

namespace BeyondTheWest.ArenaAddition;
public struct ObjectData
{
    public ObjectType objectType = ObjectType.Rock;
    public int intData = -1;
    public readonly bool IsDefault => this.objectType == ObjectType.Rock && this.intData == -1;

    public ObjectData() { }
    public ObjectData(ObjectType objectType)
    {
        this.objectType = objectType;
        this.intData = 0;
    }
    public ObjectData(ObjectType objectType, int intData) : this(objectType)
    {
        this.intData = intData;
    }

    public static bool operator ==(ObjectData left, ObjectData right)
    {
        return left.objectType == right.objectType
            && left.intData == right.intData;
    }
    public static bool operator !=(ObjectData left, ObjectData right)
    {
        return !(left == right);
    }
    public override readonly bool Equals(object obj)
    {
        return obj is ObjectData objectData && this == objectData;
    }
    public override int GetHashCode()
    {
        return objectType.GetHashCode() + intData.GetHashCode();
    }
};