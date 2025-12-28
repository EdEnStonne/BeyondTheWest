using System;
using BeyondTheWest;
using UnityEngine;
using RWCustom;
using HUD;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using BeyondTheWest.MeadowCompat;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;
using MItemData = PlacedObject.MultiplayerItemData;
using System.Linq;

namespace BeyondTheWest.ArenaAddition;
public class ArenaItemSpawnManager
{
    public static ConditionalWeakTable<ArenaGameSession, ArenaItemSpawnManager> arenaItemManagers = new();
    public static bool TryGetManager(ArenaGameSession arena, out ArenaItemSpawnManager itemSpawnManager)
    {
        if (arenaItemManagers.TryGetValue(arena, out itemSpawnManager))
        {
            return true;
        }
        return false;
    }
    public static ArenaItemSpawnManager GetManager(ArenaGameSession arena)
    {
        TryGetManager(arena, out ArenaItemSpawnManager arenaSettings);
        return arenaSettings;
    }
    public static void AddManager(ArenaGameSession arena, out ArenaItemSpawnManager arenaItemSpawnManager)
    {
        if (arenaItemManagers.TryGetValue(arena, out _))
        {
            arenaItemManagers.Remove(arena);
        }
        arenaItemSpawnManager = new(arena);
        arenaItemManagers.Add(arena, arenaItemSpawnManager);
    }
    public static void AddManager(ArenaGameSession arena)
    {
        AddManager(arena, out _);
    }

    public static ObjectType GetObjectTypeFromMultiplayerItemData(MItemData mItemData)
    {
        ObjectType objectType = ObjectType.Rock;
        if (!(mItemData.type == MItemData.Type.Rock))
        {
            if (mItemData.type == MItemData.Type.Spear)
            {
                objectType = ObjectType.Spear;
            }
            else if (mItemData.type == MItemData.Type.ExplosiveSpear)
            {
                objectType = ObjectType.Spear;
            }
            else if (mItemData.type == MItemData.Type.Bomb)
            {
                objectType = ObjectType.ScavengerBomb;
            }
            else if (mItemData.type == MItemData.Type.SporePlant)
            {
                objectType = ObjectType.SporePlant;
            }
        }
        return objectType;
    }
    public static ObjectData GetNewObjectDataFromPlacedObject(ObjectType objectType, PlacedObject placedObject)
    {
        ObjectData objectData;
        ArenaItemSpawn.ArenaItemSpawnSetting settings = new();
        if (settings.randomItem)
        {
            objectData = ArenaItemSpawn.allPool.Pool();
        }
        else
        {
            objectData = new(objectType, (placedObject.data as MItemData).type == MItemData.Type.ExplosiveSpear ? 1 : 0);
            
            if (settings.diversity)
            {
                ObjectDataPool newPoll;
                if (objectType == ObjectType.Spear)
                {
                    newPoll = new(ArenaItemSpawn.spearPool);
                }
                else if (objectType == ObjectType.Rock || objectType == ObjectType.ScavengerBomb)
                {
                    newPoll = new(ArenaItemSpawn.rockPool);
                }
                else
                {
                    newPoll = new(ArenaItemSpawn.othersPool);
                }
                newPoll.AddToPool(objectData, 100);
                objectData = newPoll.Pool();
            }
        }
        return objectData;
    }
    public static ObjectData GetNewObjectDataFromPlacedObject(PlacedObject placedObject)
    {
        ObjectType objectType = ObjectType.Rock;
        if (placedObject.data is MItemData mItemData)
        {
            objectType = GetObjectTypeFromMultiplayerItemData(mItemData);
        }
        return GetNewObjectDataFromPlacedObject(objectType, placedObject);
    }
    public static void AddItemSpawnerFromPlacedObject(ArenaGameSession arena, Room room, ObjectType objectType, PlacedObject placedObject, int count)
    {
        List<ObjectData> objectList = new();
        for (int i = 1; i <= count; i++)
        {
            ObjectData objectData = GetNewObjectDataFromPlacedObject(objectType, placedObject);
            objectList.Add( objectData );
        }
        ArenaItemSpawn itemSpawn = new(placedObject.pos, objectList);
        if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
        {
            itemSpawn.spawnTime = (int)(2 * BTWFunc.FrameRate + BTWFunc.Random(3 * BTWFunc.FrameRate));
        }
        if (TryGetManager(arena, out var arenaItemSpawnManager))
        {
            arenaItemSpawnManager.itemSpawns.Add(itemSpawn);
            itemSpawn.itemSpawnManager = arenaItemSpawnManager;
        }
        room.AddObject(itemSpawn);
    }

    public ArenaItemSpawnManager(ArenaGameSession arena)
    {
        this.arena = arena;
        if (arena.room != null)
        {
            Init();
        }
    }

    private void Init()
    {
        if (arena.room != null)
        {
            this.room = arena.room;
            this.playersCount = BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena() ? 
                MeadowFunc.GetPlayersInLobby() : arena.arenaSitting.players.Count;
            this.availableSpawn = arena.room.roomSettings.placedObjects.FindAll(x => x.data is MItemData);
        }
    }

    public List<PlacedObject> GetAvailableSpawn()
    {
        List<PlacedObject> positionAvailables = new();
        foreach (var spotData in this.availableSpawn)
        {
            if (!this.itemSpawns.Exists(x => x.pos == spotData.pos) 
                && BTWFunc.GetAllObjectsInRadius(this.room, spotData.pos, this.objectLimitRadius).Count < this.objectLimit)
            {
                positionAvailables.Add(spotData);
            }
        }
        return positionAvailables;
    }
    public PlacedObject GetRandomAvailableSpawn()
    {
        List<PlacedObject> positionAvailables = GetAvailableSpawn();
        if (positionAvailables.Count > 0)
        {
            return positionAvailables[(int)(BTWFunc.random * positionAvailables.Count)];
        }
        return null;
    }

    public int SpearsAvailable()
    {
        int count = 0;
        foreach (ArenaItemSpawn itemSpawner in this.itemSpawns)
        {
            foreach (ObjectData item in itemSpawner.objectList)
            {
                if (item.objectType == ObjectType.Spear)
                {
                    count++;
                }
            }
        }
        if (this.room != null)
        {
            foreach (AbstractSpear abstractSpear in this.room.abstractRoom.entities.FindAll(x => x is AbstractSpear).Cast<AbstractSpear>())
            {
                if (abstractSpear.realizedObject is Spear spear
                    && spear.mode != Weapon.Mode.StuckInWall
                    && spear.mode != Weapon.Mode.StuckInCreature
                    && spear.mode != Weapon.Mode.Frozen)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public void Update()
    {
        if (this.room == null)
        {
            if (arena.room != null)
            {
                Init();
            }
            return;
        }

        if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
        {
            this.playersCount = arena.Players.Count;
        }

        if (this.doRespawn)
        {
            respawnCount.Tick();
            if (respawnCount.ended)
            {
                respawnCount.Reset();
                BTWPlugin.Log($"Respawning some items !");
                for (int i = 0; i < this.RespawnAttemps; i++)
                {
                    PlacedObject availableSpawn = GetRandomAvailableSpawn();
                    MItemData multiplayerItemData = (MItemData)availableSpawn?.data;
                    if (availableSpawn != null && multiplayerItemData != null)
                    {
                        if (BTWFunc.random < multiplayerItemData.chance)
                        {
                            AddItemSpawnerFromPlacedObject(
                                this.arena, this.room, GetObjectTypeFromMultiplayerItemData(multiplayerItemData), availableSpawn, 1);
                        }
                    }
                }
            }
        }
        if (this.doCheckSpearsCount)
        {
            int spearcount = SpearsAvailable();
            // BTWPlugin.Log($"Counting spears ! Spears <{spearcount}> VS Players : <{this.playersCount}>");
            while (spearcount < this.playersCount + 1)
            {
                int spawning = (int)Mathf.Pow(BTWFunc.random, 3) + 1;
                PlacedObject availableSpawn = GetRandomAvailableSpawn();
                MItemData multiplayerItemData = (MItemData)availableSpawn?.data;
                BTWPlugin.Log($"Not enough spears ! Adding <{spawning}> spears at [{availableSpawn.pos}]");

                if (availableSpawn != null && multiplayerItemData != null)
                {
                    List<ObjectData> objectList = new();
                    for (int i = 1; i <= spawning; i++)
                    {
                        ObjectData objectData = ArenaItemSpawn.spearPool.Pool();
                        objectList.Add( objectData );
                    }
                    ArenaItemSpawn itemSpawn = new(availableSpawn.pos, (int)(BTWFunc.Random(10, 20) * BTWFunc.FrameRate), objectList);
                    this.itemSpawns.Add(itemSpawn);
                    itemSpawn.itemSpawnManager = this;
                    room.AddObject(itemSpawn);

                    spearcount += spawning;
                }
                else
                {
                    spearcount++;
                }
            }
        }
    }

    public ArenaGameSession arena;
    public Room room;
    public int playersCount = 0;

    public List<ArenaItemSpawn> itemSpawns = new();
    public List<PlacedObject> availableSpawn = new();

    public bool doCheckSpearsCount = true;
    public bool doCheckRockCount = true;
    public bool doCheckOthersCount = true;

    public bool doRespawn = true;
    public int RespawnAttemps = 15;
    public Counter respawnCount = new(BTWFunc.FrameRate * 60);

    public int objectLimit = 5;
    public float objectLimitRadius = 20;
}

public class ArenaItemSpawnManagerHooks
{
    public static void ApplyHooks()
    {
        On.ArenaGameSession.Initiate += ArenaGameSession_AddArenaItemSpawnManager;
        On.ArenaGameSession.Update += ArenaGameSession_UpdateArenaItemSpawnManager;
        BTWPlugin.Log("ArenaItemSpawnManagerHooks ApplyHooks Done !");
    }


    private static void ArenaGameSession_AddArenaItemSpawnManager(On.ArenaGameSession.orig_Initiate orig, ArenaGameSession self)
    {
        orig(self);
        if (self is CompetitiveGameSession)
        {
            bool manager = false;
            BTWPlugin.Log($"Adding Spawn Manager to [{self}]...");
            if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
            {
                BTWPlugin.Log("Applying meadow settings !");
                manager = MeadowFunc.CheckIfShouldAddItemManagerOnline();
            }
            else
            {
                BTWPlugin.Log("Applying normal settings !");
                manager = BTWRemix.NewItemSpawningSystem.Value;
            }
            if (manager)
            {
                ArenaItemSpawnManager.AddManager(self, out var spawnManager);
                foreach (ArenaItemSpawn itemSpawner in self.room.updateList.FindAll(x => x is ArenaItemSpawn).Cast<ArenaItemSpawn>())
                {
                    spawnManager.itemSpawns.Add(itemSpawner);
                }
                BTWPlugin.Log($"Added Spawn Manager to [{self}] !");
            }
        }
    }
    private static void ArenaGameSession_UpdateArenaItemSpawnManager(On.ArenaGameSession.orig_Update orig, ArenaGameSession self)
    {
        orig(self);
        try
        {
            if (ArenaItemSpawnManager.TryGetManager(self, out var itemSpawnManager))
            {
                itemSpawnManager.Update();
            }
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
    }
}