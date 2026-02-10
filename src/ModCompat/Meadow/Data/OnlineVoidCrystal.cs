using System;
using RainMeadow;
using JetBrains.Annotations;
using BeyondTheWest.Items;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineVoidCrystal : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public OnlineVoidCrystal() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineFieldHalf]
        public float containedVoidEnergy = 0;
        [OnlineField]
        public bool ignited = false;
        [OnlineField]
        public bool exploded = false;
        [OnlineField]
        public int explodeCounter = 0;
        
        //--------- ctor
        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo is not AbstractVoidCrystal abstractVoidCrystal) { return; }
            
            this.containedVoidEnergy = abstractVoidCrystal.containedVoidEnergy;
            if (abstractVoidCrystal.realizedObject is VoidCrystal voidCrystal)
            {
                this.ignited = voidCrystal.ignited;
                this.exploded = voidCrystal.exploded;
                this.explodeCounter = voidCrystal.explodeCounter.value;
            }
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo is not AbstractVoidCrystal abstractVoidCrystal)  { return; }
            if (abstractVoidCrystal.IsLocal()) { return; }

            abstractVoidCrystal.containedVoidEnergy = this.containedVoidEnergy;

            if (abstractVoidCrystal.realizedObject is VoidCrystal voidCrystal)
            {
                if (this.ignited && !voidCrystal.ignited)
                {
                    voidCrystal.InitExplosion();
                }
                if (this.exploded && !voidCrystal.exploded)
                {
                    voidCrystal.Explode();
                }
                voidCrystal.ignited = this.ignited;
                voidCrystal.exploded = this.exploded;
                voidCrystal.explodeCounter.value = this.explodeCounter;
            }
        }
        public override Type GetDataType()
        {
            return typeof(OnlineVoidCrystal);
        }

    }
}