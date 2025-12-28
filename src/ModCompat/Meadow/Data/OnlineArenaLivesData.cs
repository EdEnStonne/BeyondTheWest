using System;
using RainMeadow;
using JetBrains.Annotations;
using UnityEngine;
using BeyondTheWest.ArenaAddition;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineArenaLivesData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public OnlineArenaLivesData() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }

    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineField]
        public int lifesleft = 1;
        [OnlineField]
        public bool countedAlive = true;
        [OnlineField]
        public int reviveCounter = 0;
        [OnlineField]
        public int livesDisplayCounter = 0;
        [OnlineField]
        public int circlesAmount = 0;
        [OnlineFieldHalf]
        public float firstPosX;
        [OnlineFieldHalf]
        public float firstPosY;

        //--------- ctor

        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Creature creature)
            {
                return;
            }

            if (!ArenaLives.TryGetLives(creature.abstractCreature, out var lives) || lives.fake)
            {
                return;
            }
            
            this.lifesleft = lives.lifesleft;
            this.countedAlive = lives.countedAlive;
            this.reviveCounter = lives.reviveCounter;
            this.livesDisplayCounter = lives.livesDisplayCounter;
            this.circlesAmount = lives.circlesAmount;
            this.firstPosX = lives.firstPos.x;
            this.firstPosY = lives.firstPos.y;
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Creature creature)
            {
                return;
            }

            if (!ArenaLives.TryGetLives(creature.abstractCreature, out var lives) || !lives.fake)
            {
                return;
            }
            if (lives.lifesleft != this.lifesleft || lives.countedAlive != this.countedAlive)
            {
                lives.karmaSymbolNeedToChange = true;
            }
            if (lives.countedAlive == false 
                && this.countedAlive == true 
                && creature is Player player && player != null)
            {
                MeadowFunc.ResetDeathMessage(creature.abstractCreature);
                MeadowFunc.ResetSlugcatIcon(creature.abstractCreature);
            }
            if (lives.wasAbstractCreatureDestroyed && this.lifesleft <= 0 && lives.lifesleft > 0)
            {
                lives.abstractTarget?.Destroy();
                lives.Dismiss();
            }
            else
            {
                lives.lifesleft = this.lifesleft;
                lives.reviveCounter = this.reviveCounter;
            }
            lives.countedAlive = this.countedAlive;
            lives.livesDisplayCounter = this.livesDisplayCounter;
            lives.circlesAmount = this.circlesAmount;
            lives.firstPos = new Vector2(this.firstPosX, this.firstPosY);

        }
        public override Type GetDataType()
        {
            return typeof(OnlineArenaLivesData);
        }

    }
}