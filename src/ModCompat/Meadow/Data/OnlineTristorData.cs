using System;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest.Items;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineTristorData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public OnlineTristorData() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineField]
        public int charge = 0;
        [OnlineField]
        public Tristor.State state = Tristor.State.Idle;
        [OnlineFieldHalf]
        public float g = 0.95f;
        
        //--------- ctor
        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo is not AbstractTristor abstractTristor)
            {
                return;
            }
            this.charge = abstractTristor.charge;
            if (abstractTristor.realizedObject is Tristor tristor)
            {
                this.state = tristor.state;
                this.g = tristor.g;
            }
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo is not AbstractTristor abstractTristor)
            {
                return;
            }
            if (abstractTristor.IsLocal()) { return; }

            abstractTristor.charge = this.charge;
            if (abstractTristor.realizedObject is Tristor tristor)
            {
                tristor.state = this.state;
                tristor.g = this.g;
            }
        }
        public override Type GetDataType()
        {
            return typeof(OnlineTristorData);
        }

    }
}