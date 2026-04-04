using System;
using RainMeadow;
using JetBrains.Annotations;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineBTWPlayerData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public OnlineBTWPlayerData() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineField]
        public int dizzy = 0;
        [OnlineField]
        public int onlineBlind = 0;
        [OnlineField]
        public int exhausted = 0;
        

        //--------- ctor

        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player
                || player.GetBTWPlayerData() is not BTWPlayerData bTWPlayerData)
            {
                return;
            }

            dizzy = bTWPlayerData.dizzy;
            onlineBlind = bTWPlayerData.onlineBlind;
            exhausted = bTWPlayerData.exhausted;
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player
                || player.GetBTWPlayerData() is not BTWPlayerData bTWPlayerData)
            {
                return;
            }

            bTWPlayerData.dizzy = dizzy;
            bTWPlayerData.onlineBlind = onlineBlind;
            bTWPlayerData.exhausted = exhausted;
        }
        public override Type GetDataType()
        {
            return typeof(OnlineBTWPlayerData);
        }

    }
}