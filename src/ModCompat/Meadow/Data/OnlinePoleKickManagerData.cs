using System;
using RainMeadow;
using JetBrains.Annotations;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlinePoleKickManagerData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public OnlinePoleKickManagerData() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineField]
        public bool bodyInFrontOfPole;

        //--------- ctor

        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
            {
                return;
            }

            if (!PoleKickManager.TryGetManager(player.abstractCreature, out var PKM) || PKM.isFake)
            {
                return;
            }

            this.bodyInFrontOfPole = PKM.bodyInFrontOfPole;
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
            {
                return;
            }

            if (!PoleKickManager.TryGetManager(player.abstractCreature, out var PKM) || !PKM.isFake)
            {
                return;
            }

            PKM.bodyInFrontOfPole = this.bodyInFrontOfPole;
        }
        public override Type GetDataType()
        {
            return typeof(OnlinePoleKickManagerData);
        }

    }
}