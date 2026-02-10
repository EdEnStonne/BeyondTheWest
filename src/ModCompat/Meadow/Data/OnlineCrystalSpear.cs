using System;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest.Items;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineCrystalSpear : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public OnlineCrystalSpear() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineField]
        public bool exploded = false;
        
        //--------- ctor
        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo is not AbstractCrystalSpear abstractCrystalSpear) { return; }
            
            if (abstractCrystalSpear.realizedObject is CrystalSpear crystalSpear)
            {
                this.exploded = crystalSpear.exploded;
            }
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo is not AbstractCrystalSpear abstractCrystalSpear)  { return; }
            if (abstractCrystalSpear.IsLocal()) { return; }


            if (abstractCrystalSpear.realizedObject is CrystalSpear crystalSpear)
            {
                if (this.exploded && !crystalSpear.exploded)
                {
                    crystalSpear.Explode();
                }
            }
        }
        public override Type GetDataType()
        {
            return typeof(OnlineVoidCrystal);
        }

    }
}