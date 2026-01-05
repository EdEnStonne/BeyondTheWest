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
        public float respawnPosX;
        [OnlineFieldHalf]
        public float respawnPosY;
        [OnlineField]
        public int respawnExit;

        //--------- ctor

        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo is not AbstractCreature abstractCreature)
            {
                return;
            }

            if (!ArenaLives.TryGetLives(abstractCreature, out var lives) || lives.fake)
            {
                return;
            }
            
            this.lifesleft = lives.lifesleft;
            this.countedAlive = lives.countedAlive;
            this.reviveCounter = lives.reviveCounter;
            this.livesDisplayCounter = lives.livesDisplayCounter;
            this.circlesAmount = lives.circlesAmount;
            this.respawnPosX = lives.respawnPos.x;
            this.respawnPosY = lives.respawnPos.y;
            this.respawnExit = lives.respawnExit;
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if ((onlineEntity as OnlinePhysicalObject)?.apo is not AbstractCreature abstractCreature)
            {
                return;
            }

            if (!ArenaLives.TryGetLives(abstractCreature, out var lives) || !lives.fake)
            {
                return;
            }
            if (lives.lifesleft != this.lifesleft || lives.countedAlive != this.countedAlive)
            {
                lives.karmaSymbolNeedToChange = true;
                BTWPlugin.Log($"Detected a life change for [{onlineEntity} : {abstractCreature}] : <{this.lifesleft}> <{this.countedAlive}>");
            }
            if (this.countedAlive && lives.room != null && lives.abstractTarget?.realizedCreature != null)
            {
                if (!lives.room.abstractRoom.creatures.Exists(x => x == lives.abstractTarget))
                {
                    BTWPlugin.Log($"[{lives.abstractTarget}] was removed from the creature list ! Adding it back"); 
                    lives.room.abstractRoom.creatures.Add(lives.abstractTarget);
                }
            }
            if (lives.countedAlive == false 
                && this.countedAlive == true 
                && abstractCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat)
            {
                MeadowFunc.ResetDeathMessage(abstractCreature);
                MeadowFunc.ResetSlugcatIcon(abstractCreature);
            }
            if (lives.wasAbstractCreatureDestroyed && this.lifesleft <= 0 && lives.lifesleft > 0)
            {
                lives.lifesleft = this.lifesleft;
                lives.abstractTarget?.Destroy();
                lives.Dismiss();
            }
            else
            {
                lives.lifesleft = this.lifesleft;
            }
            lives.reviveCounter = this.reviveCounter;
            // BTWPlugin.Log($"Counter of Reviving entity [{onlineEntity} : {abstractCreature}] to <{lives.reviveCounter}>");
            lives.countedAlive = this.countedAlive;
            lives.livesDisplayCounter = this.livesDisplayCounter;
            lives.circlesAmount = this.circlesAmount;
            lives.respawnPos = new Vector2(this.respawnPosX, this.respawnPosY);
            lives.respawnExit = this.respawnExit;

        }
        public override Type GetDataType()
        {
            return typeof(OnlineArenaLivesData);
        }

    }
}