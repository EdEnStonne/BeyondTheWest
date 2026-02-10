using System;
using UnityEngine;
using RainMeadow;

namespace BeyondTheWest.MeadowCompat.Gamemodes;

public class ArenaStockClientSettings : OnlineEntity.EntityData
{
    public int lives = -1;

    public ArenaStockClientSettings() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(this);
    }

    public class State : EntityDataState
    {
        [OnlineField]
        public int lives;
        public State() { }

        public State(ArenaStockClientSettings onlineEntity) : base()
        {
            lives = onlineEntity.lives;
        }

        public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
        {
            var avatarSettings = (ArenaStockClientSettings)entityData;
            avatarSettings.lives = lives;
        }

        public override Type GetDataType() => typeof(ArenaStockClientSettings);
    }
}
