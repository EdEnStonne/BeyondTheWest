using System;
using RainMeadow;
using JetBrains.Annotations;
using UnityEngine;
using BeyondTheWest.ArenaAddition;
using System.Collections.Generic;
using System.Linq;
using static RainMeadow.OnlineEntity;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineArenaLivesState : EntityState
{
    //--------- Variables
    [OnlineField]
    public int lifesleft = 1;
    [OnlineField]
    public bool countedAlive = true;
    [OnlineField]
    public bool reinforced = false;
    [OnlineField]
    public int reviveCounter = 0;
    [OnlineField]
    public int killChain = 0;
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

    public OnlineArenaLivesState() : base() { }
    public OnlineArenaLivesState(OnlineArenaLives onlineArenaLives, OnlineResource inResource, uint ts) : base(onlineArenaLives, inResource, ts)
    {
        if (onlineArenaLives.arenalives is not ArenaLives lives)
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
        this.reinforced = lives.reinforced;
        this.killChain = lives.killChain;
    }
    //--------- Functions
    public override void ReadTo(OnlineEntity onlineEntity)
    {
        if (onlineEntity is not OnlineArenaLives onlineArenaLives
            || onlineArenaLives.arenalives is not ArenaLives lives)
        {
            return;
        }

        if (!lives.karmaSymbolNeedToChange 
            && (lives.lifesleft != this.lifesleft 
                || lives.reinforced != this.reinforced))
        {
            lives.karmaSymbolNeedToChange = true;
            BTWPlugin.Log($"Detected a life change for [{onlineEntity} : {lives.playerID}] : <{this.lifesleft}> <{this.countedAlive}> <{this.reinforced}>");
        }
        
        if (this.countedAlive 
            && lives.room?.abstractRoom != null 
            && lives.abstractTarget != null)
        {
            if (!lives.room.abstractRoom.creatures.Exists(x => x == lives.abstractTarget))
            {
                BTWPlugin.Log($"[{lives.abstractTarget}] was removed from the creature list ! Adding it back"); 
                lives.room.abstractRoom.creatures.Add(lives.abstractTarget);
            }
        }
        
        if (lives.abstractTarget is not null && lives.abstractTarget.slatedForDeletion) 
        { 
            lives.abstractTarget = null; 
        }
        
        if (this.countedAlive 
            && !lives.countedAlive
            && lives.room?.game?.GetArenaGameSession is not null)
        {
            lives.abstractTarget = BTWFunc.GetPlayerFromArenaNumber(lives.playerID, lives.room.game.GetArenaGameSession);
            if (lives.abstractTarget?.GetOnlineCreature() is not null)
            {
                lives.countedAlive = this.countedAlive;
                lives.karmaSymbolNeedToChange = true;
                MeadowFunc.ResetDeathMessage(lives.abstractTarget);
                MeadowFunc.ResetSlugcatIcon(lives.abstractTarget);
                BTWPlugin.Log($"Fake Player [{lives.abstractTarget}] icon and death message was reset !");
            }
        }
        else if (lives.countedAlive != this.countedAlive)
        {
            lives.karmaSymbolNeedToChange = true;
            lives.countedAlive = this.countedAlive;
        }
        
        lives.lifesleft = this.lifesleft;
        lives.reviveCounter = this.reviveCounter;
        lives.reinforced = this.reinforced;
        lives.killChain = this.killChain;
        lives.livesDisplayCounter = this.livesDisplayCounter;
        lives.circlesAmount = this.circlesAmount;
        lives.respawnPos = new Vector2(this.respawnPosX, this.respawnPosY);
        lives.respawnExit = this.respawnExit;
        
    }
}