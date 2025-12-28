using ObjectType = AbstractPhysicalObject.AbstractObjectType;

namespace BeyondTheWest.ArenaAddition;
public struct ObjectData
{
    public ObjectType objectType = ObjectType.Rock;
    public int intData = 0;

    public ObjectData() { }
    public ObjectData(ObjectType objectType)
    {
        this.objectType = objectType;
    }
    public ObjectData(ObjectType objectType, int intData) : this(objectType)
    {
        this.intData = intData;
    }
};