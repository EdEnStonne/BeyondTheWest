using System;
using RainMeadow;
using JetBrains.Annotations;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineStaticChargeManagerData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public OnlineStaticChargeManagerData() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineFieldHalf]
        public float charge;

        //--------- ctor

        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
            {
                return;
            }

            if (!StaticChargeManager.TryGetManager(player.abstractCreature, out var SCM) || !SCM.active)
            {
                return;
            }

            charge = SCM.Charge;
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            var SCM = MeadowFunc.GetOnlinePlayerStaticChargeManager(onlineEntity);
            if (SCM == null|| !SCM.isMeadowFakePlayer || SCM.active) { return; }

            SCM.Charge = this.charge;
        }
        public override Type GetDataType()
        {
            return typeof(OnlineStaticChargeManagerData);
        }

    }
}