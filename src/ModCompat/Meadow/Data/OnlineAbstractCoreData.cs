using System;
using RainMeadow;
using JetBrains.Annotations;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineAbstractCoreData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public OnlineAbstractCoreData() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineFieldHalf]
        public float energy;
        [OnlineFieldHalf]
        public float grayScale;
        [OnlineField]
        public int boostingCount;
        [OnlineField]
        public int antiGravityCount;
        [OnlineField]
        public byte state;

        //--------- ctor

        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
            {
                return;
            }

            if (!AbstractEnergyCore.TryGetCore(player.abstractCreature, out var AEC))
            {
                return;
            }

            this.energy = AEC.energy;
            this.boostingCount = AEC.boostingCount;
            this.antiGravityCount = AEC.antiGravityCount;
            this.state = AEC.state;
            this.grayScale = 0f;
            if (AEC.RealizedCore != null)
            {
                this.grayScale = AEC.RealizedCore.grayScale;
            }
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            var AEC = MeadowFunc.GetOnlinePlayerAbstractEnergyCore(onlineEntity);
            if (AEC == null || !AEC.isMeadowFakePlayer || AEC.active) { return; }

            AEC.energy = this.energy;
            AEC.boostingCount = this.boostingCount;
            AEC.antiGravityCount = this.antiGravityCount;
            AEC.state = this.state;
            if (AEC.RealizedCore != null)
            {
                AEC.RealizedCore.grayScale = this.grayScale;
            }
        }
        public override Type GetDataType()
        {
            return typeof(OnlineAbstractCoreData);
        }

    }
}