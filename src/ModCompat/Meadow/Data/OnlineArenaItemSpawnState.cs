using System;
using RainMeadow;
using JetBrains.Annotations;
using UnityEngine;
using BeyondTheWest.ArenaAddition;
using System.Collections.Generic;
using System.Linq;
using static RainMeadow.OnlineEntity;

namespace BeyondTheWest.MeadowCompat.Data;
public class OnlineArenaItemSpawnState : EntityState
    {
        //--------- Variables
        [OnlineField]
        public int spawnCount = 0;
        [OnlineField]
        public int destructionCount = 0;

        //--------- ctor

        public OnlineArenaItemSpawnState() : base() { }
        public OnlineArenaItemSpawnState(OnlineArenaItemSpawn onlineArenaItemSpawn, OnlineResource inResource, uint ts) : base(onlineArenaItemSpawn, inResource, ts)
        {
            if (onlineArenaItemSpawn.itemSpawn is not ArenaItemSpawn arenaItemSpawn) return;
            
            this.spawnCount = arenaItemSpawn.spawnCount;
            this.destructionCount = arenaItemSpawn.destructionCount;
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity onlineEntity)
        {
            if (onlineEntity is not OnlineArenaItemSpawn onlineArenaItemSpawn
                || onlineArenaItemSpawn.itemSpawn is not ArenaItemSpawn arenaItemSpawn) return;

            if (onlineArenaItemSpawn.IsLocked(OnlineArenaItemSpawn.spawningLock)) return;

            arenaItemSpawn.spawnCount = this.spawnCount;
            arenaItemSpawn.destructionCount = this.destructionCount;
        }
    }