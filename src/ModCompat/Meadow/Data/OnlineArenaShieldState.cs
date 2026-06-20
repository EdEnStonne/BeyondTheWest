using System;
using RainMeadow;
using JetBrains.Annotations;
using UnityEngine;
using BeyondTheWest.ArenaAddition;
using System.Collections.Generic;
using System.Linq;
using static RainMeadow.OnlineEntity;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineArenaShieldState : EntityState
    {
        //--------- Variables
        [OnlineField(nullable:true)]
        public OnlineCreature onlinePlayerCreature;
        [OnlineField]
        public int life = 0;
        [OnlineField]
        public int destruction = 0;

        //--------- ctor

        public OnlineArenaShieldState() : base() { }
        public OnlineArenaShieldState(OnlineArenaShield onlineArenaShield, OnlineResource inResource, uint ts) : base(onlineArenaShield, inResource, ts)
        {
            if (onlineArenaShield.arenaShield is not ArenaShield arenaShield) return;

            this.onlinePlayerCreature = arenaShield.target?.abstractCreature?.GetOnlineCreature();
            this.life = arenaShield.life;
            this.destruction = arenaShield.destruction;
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity onlineEntity)
        {
            if (onlineEntity is not OnlineArenaShield onlineArenaShield
                || onlineArenaShield.arenaShield is not ArenaShield arenaShield) return;

            arenaShield.target = this.onlinePlayerCreature?.abstractCreature?.realizedCreature as Player;
            arenaShield.life = this.life;
            arenaShield.destruction = this.destruction;
        }
    }