using ObjectType = AbstractPhysicalObject.AbstractObjectType;

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
};