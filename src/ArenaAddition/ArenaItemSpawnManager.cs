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
using BepInEx;

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
    public static ObjectData GetNewObjectDataFromPlacedObject(ObjectType objectType, PlacedObject placedObject, ObjectType[] blocklist = null)
    {
        ObjectData objectData;
        ObjectDataPool newPoll = new();
        ArenaItemSpawn.ArenaItemSpawnSetting settings = new();
        if (settings.randomItem)
        {
            newPoll = new(ArenaItemSpawn.allPool);
        }
        else
        {
            newPoll.AddToPool(
                new ObjectData(objectType, (placedObject.data as MItemData).type == MItemData.Type.ExplosiveSpear ? 1 : 0), 
                500);
            
            if (settings.diversity)
            {
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
            }
        }
        if (blocklist != null)
        {
            newPoll.RemoveFromPool(x => blocklist.Contains(x.objectData.objectType));
        }
        if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
        {
            MeadowFunc.RemoveRestrictedItemsInArenaFromPool(ref newPoll);
        }
        // BTWPlugin.Log($"New pool from [{objectType}] !");
        // newPoll.LogPool();
        if (newPoll.IsEmpty) { return new(); }
        objectData = newPoll.Pool();
        return objectData;
    }
    public static ObjectData GetNewObjectDataFromPlacedObject(PlacedObject placedObject, ObjectType[] blocklist = null)
    {
        ObjectType objectType = ObjectType.Rock;
        if (placedObject.data is MItemData mItemData)
        {
            objectType = GetObjectTypeFromMultiplayerItemData(mItemData);
        }
        return GetNewObjectDataFromPlacedObject(objectType, placedObject, blocklist);
    }
    public static void AddItemSpawnerFromPlacedObject(ArenaGameSession arena, Room room, ObjectType objectType, PlacedObject placedObject, int count, ObjectType[] blocklist = null)
    {
        List<ObjectData> objectList = new();
        for (int i = 1; i <= count; i++)
        {
            ObjectData objectData = GetNewObjectDataFromPlacedObject(objectType, placedObject, blocklist);
            if (objectData.IsDefault) { continue; }
            
            objectList.Add( objectData );
        }
        if (objectList.Count == 0) { return; }

        ArenaItemSpawn itemSpawn = new(placedObject.pos, objectList);
        if (BTWPlugin.meadowEnabled && MeadowFunc.ShouldHoldFireFromOnlineArenaTimer())
        {
            int timer = MeadowFunc.ArenaCountdownTimerCurrent();
            itemSpawn.spawnTime = (int)BTWFunc.Random(timer);
            BTWPlugin.Log($"Meadow setting detected ! Timer set at <{itemSpawn.spawnTime}> since timer is at <{timer}>");
        }
        else
        {
            itemSpawn.spawnTime = (int)BTWFunc.Random(BTWFunc.FrameRate * 3f, BTWFunc.FrameRate * 10f);
        }

        if (TryGetManager(arena, out var arenaItemSpawnManager))
        {
            arenaItemSpawnManager.itemSpawns.Add(itemSpawn);
            itemSpawn.itemSpawnManager = arenaItemSpawnManager;
        }
        itemSpawn.spawnTime = (int)Mathf.Clamp(itemSpawn.spawnTime, BTWFunc.FrameRate * 3f, BTWFunc.FrameRate * 60f);
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
            this.playersCount = arena.arenaSitting.players.Count;
            this.availableSpawn = arena.room.roomSettings.placedObjects.FindAll(x => x.data is MItemData);
            if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
            {
                MeadowCalls.BTWArena_ArenaItemSpawnManagerInit(this);
            }
        }
    }

    public List<PlacedObject> GetAvailableSpawn()
    {
        List<PlacedObject> positionAvailables = new();
        foreach (var spotData in this.availableSpawn)
        {
            if (!this.itemSpawns.Exists(x => (x.pos - spotData.pos).magnitude <= this.spawnerLimitRadius) 
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
    public Vector2 GetRandomAvailableSpawnPos()
    {
        PlacedObject availableSpawn = GetRandomAvailableSpawn();
        return availableSpawn != null ? availableSpawn.pos : Vector2.zero;
    }
    
    private bool IsValidSpawnSpot(IntVector2 tilePos)
    {
        if (this.room != null)
        {
            IntVector2 YPtilePos = new(tilePos.x, tilePos.y + 1);
            IntVector2 YNtilePos = new(tilePos.x, tilePos.y - 1);
            IntVector2 GroundtilePos = new(tilePos.x, tilePos.y - 2);
            IntVector2 XPtilePos = new(tilePos.x + 1, tilePos.y);
            IntVector2 XntilePos = new(tilePos.x - 1, tilePos.y);

            if (room.GetTile(tilePos).IsAir()
                && room.GetTile(YPtilePos).IsAir()
                && room.GetTile(YNtilePos).IsAir()
                && room.GetTile(XPtilePos).IsAir()
                && room.GetTile(XntilePos).IsAir()
                && room.GetTile(GroundtilePos).IsWalkable()
                && !this.itemSpawns.Exists(x => (x.pos - this.room.MiddleOfTile(tilePos)).magnitude <= this.spawnerLimitRadius)
            )
            {
                return true;
            }
        }
        return false;
    }
    public Vector2 GetRandomSpawnPos(int validPosAttempts = 16)
    {
        if (this.room != null)
        {
            IntVector2 tilePos = room.RandomTile();

            int attempt = 0;
            bool validPos = false;
            while (!validPos && attempt < validPosAttempts)
            {
                if (IsValidSpawnSpot(tilePos))
                {
                    validPos = true;
                }
                else if (room.GetTile(tilePos).IsWalkable())
                {
                    while (tilePos.y < room.Height)
                    {
                        tilePos.y++;
                        
                        if (IsValidSpawnSpot(tilePos))
                        {
                            validPos = true;
                            break;
                        }
                    }
                }
                else
                {
                    while (tilePos.y > -1)
                    {
                        tilePos.y--;
                        if (IsValidSpawnSpot(tilePos))
                        {
                            validPos = true;
                            break;
                        }
                    }
                }
                attempt++;
            }
            if (validPos)
            {
                Vector2 spot = room.MiddleOfTile(tilePos);
                tilePos.y--;
                spot += room.MiddleOfTile(tilePos);
                spot /= 2;
                return spot;
            }
            else
            {
                BTWPlugin.Log("Couldn't find a suitable spot !");
            }
        }
        return Vector2.zero;
    }

    public int SpearsAvailable()
    {
        int count = 0;
        foreach (ArenaItemSpawn itemSpawner in this.itemSpawns)
        {
            foreach (ObjectData item in itemSpawner.objectList)
            {
                if (ArenaItemSpawn.spearPool.pool.Exists(x => x.objectData.objectType == item.objectType))
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
                    && !spear.slatedForDeletetion
                    && BTWFunc.InRoomBounds(spear)
                    && spear.room != null
                    && spear.mode != Weapon.Mode.StuckInWall
                    && spear.mode != Weapon.Mode.StuckInCreature
                    && spear.mode != Weapon.Mode.Frozen)
                {
                    count++;
                    // BTWPlugin.Log($"Found [{spear}] <{count}> at [{spear.firstChunk.pos} : {abstractSpear.pos}]");
                }
            }
        }
        return count;
    }
    public int ThrowableAvailable()
    {
        int count = 0;
        foreach (ArenaItemSpawn itemSpawner in this.itemSpawns)
        {
            foreach (ObjectData item in itemSpawner.objectList)
            {
                if (ArenaItemSpawn.rockPool.pool.Exists(x => x.objectData.objectType == item.objectType))
                {
                    count++;
                }
            }
        }
        if (this.room != null)
        {
            foreach (AbstractPhysicalObject abstractPhysicalObject in this.room.abstractRoom.entities.FindAll(x => x is AbstractPhysicalObject apo && apo.realizedObject != null).Cast<AbstractPhysicalObject>())
            {
                if (ArenaItemSpawn.rockPool.pool.Exists(x => x.objectData.objectType == abstractPhysicalObject.type)
                    && !abstractPhysicalObject.realizedObject.slatedForDeletetion
                    && BTWFunc.InRoomBounds(abstractPhysicalObject.realizedObject))
                {
                    count++;
                }
            }
        }
        return count;
    }
    public int MiscellaniousAvailable()
    {
        int count = 0;
        foreach (ArenaItemSpawn itemSpawner in this.itemSpawns)
        {
            foreach (ObjectData item in itemSpawner.objectList)
            {
                if (ArenaItemSpawn.othersPool.pool.Exists(x => x.objectData.objectType == item.objectType))
                {
                    count++;
                }
            }
        }
        if (this.room != null)
        {
            foreach (AbstractPhysicalObject abstractPhysicalObject in this.room.abstractRoom.entities.FindAll(x => x is AbstractPhysicalObject apo && apo.realizedObject != null).Cast<AbstractPhysicalObject>())
            {
                if (ArenaItemSpawn.othersPool.pool.Exists(x => x.objectData.objectType == abstractPhysicalObject.type)
                    && !abstractPhysicalObject.realizedObject.slatedForDeletetion
                    && BTWFunc.InRoomBounds(abstractPhysicalObject.realizedObject))
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
            if (arena?.room != null)
            {
                Init();
            }
            return;
        }

        if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
        {
            this.playersCount = arena.arenaSitting.players.Count;
        }

        this.itemSpawns.RemoveAll(x => x.slatedForDeletetion);

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
                                this.arena, this.room, GetObjectTypeFromMultiplayerItemData(multiplayerItemData), 
                                availableSpawn, (int)(Mathf.Pow(BTWFunc.random, 5) * 2) + 1, 
                                this.noSpears ? ArenaItemSpawn.spearPool.AllItemsTypes().ToArray() : null);
                        }
                    }
                }
                for (int j = 0; j < this.RandomSpawnAttemps; j++)
                {
                    Vector2 spot = GetRandomSpawnPos();

                    if (spot != Vector2.zero)
                    {
                        List<ObjectData> objectList = new();
                        for (int i = 1; i <= (int)(Mathf.Pow(BTWFunc.random, 5) * 2) + 1; i++)
                        {
                            ObjectDataPool newPool = new(ArenaItemSpawn.allPool);
                            if (this.noSpears)
                            {
                                newPool.RemoveFromPool(x => ArenaItemSpawn.spearPool.AllItemsTypes().Exists(y => y == x.objectData.objectType));
                            }
                            if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
                            {
                                MeadowFunc.RemoveRestrictedItemsInArenaFromPool(ref newPool);
                            }
                            ObjectData objectData = newPool.Pool();
                            if (objectData.IsDefault) { continue; }
                            objectList.Add( objectData );
                        }
                        if (objectList.Count == 0) { continue; }
                        ArenaItemSpawn itemSpawn = new(spot, objectList);
                        if (BTWPlugin.meadowEnabled && MeadowFunc.ShouldHoldFireFromOnlineArenaTimer())
                        {
                            itemSpawn.spawnTime = (int)BTWFunc.Random(MeadowFunc.ArenaCountdownTimerCurrent());
                            BTWPlugin.Log($"Meadow setting detected ! Timer set at <{itemSpawn.spawnTime}> since timer is at <{MeadowFunc.ArenaCountdownTimerCurrent()}>");
                            itemSpawn.spawnTime = (int)Mathf.Clamp(itemSpawn.spawnTime, BTWFunc.FrameRate * 3f, BTWFunc.FrameRate * 60f);
                        }
                        this.itemSpawns.Add(itemSpawn);
                        itemSpawn.itemSpawnManager = this;
                        room.AddObject(itemSpawn);
                    }
                }
            }
        }

        if (this.doCheckSpearsCount && !this.noSpears)
        {
            int spearcount = SpearsAvailable();
            // BTWPlugin.Log($"Counting spears ! Spears <{spearcount}> VS Players : <{this.playersCount}>");
            while (spearcount < this.playersCount + 4)
            {
                int spawning = (int)(Mathf.Pow(BTWFunc.random, 5) * 2) + 1;
                Vector2 spot = GetRandomSpawnPos();
                BTWPlugin.Log($"Not enough spears ! <{spearcount}> for <{this.playersCount}> player ! Adding <{spawning}> spears at [{spot}]");

                if (spot != Vector2.zero)
                {
                    List<ObjectData> objectList = new();
                    for (int i = 1; i <= spawning; i++)
                    {
                        ObjectDataPool newPool = new(ArenaItemSpawn.spearPool);
                        if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
                        {
                            MeadowFunc.RemoveRestrictedItemsInArenaFromPool(ref newPool);
                        }
                        ObjectData objectData = newPool.Pool();
                        if (objectData.IsDefault) { continue; }
                        objectList.Add( objectData );
                    }
                    if (objectList.Count == 0) { continue; }
                    ArenaItemSpawn itemSpawn = new(spot, (int)(BTWFunc.Random(10, 20) * BTWFunc.FrameRate), objectList);
                    if (BTWPlugin.meadowEnabled && MeadowFunc.ShouldHoldFireFromOnlineArenaTimer())
                    {
                        itemSpawn.spawnTime = (int)BTWFunc.Random(MeadowFunc.ArenaCountdownTimerCurrent());
                        BTWPlugin.Log($"Meadow setting detected ! Timer set at <{itemSpawn.spawnTime}> since timer is at <{MeadowFunc.ArenaCountdownTimerCurrent()}>");
                        itemSpawn.spawnTime = (int)Mathf.Clamp(itemSpawn.spawnTime, BTWFunc.FrameRate * 3f, BTWFunc.FrameRate * 60f);
                    }
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

        if (this.doCheckThrowableCount)
        {
            int throwableCount = ThrowableAvailable();
            while (throwableCount < this.playersCount + (this.noSpears ? 5 : 1))
            {
                int spawning = (int)(Mathf.Pow(BTWFunc.random, 5) * 2) + 1;
                Vector2 spot = GetRandomSpawnPos();
                BTWPlugin.Log($"Not enough throwable ! <{throwableCount}> for <{this.playersCount}> player ! Adding <{spawning}> throwable at [{spot}]");

                if (spot != Vector2.zero)
                {
                    List<ObjectData> objectList = new();
                    for (int i = 1; i <= spawning; i++)
                    {
                        ObjectDataPool newPool = new(ArenaItemSpawn.rockPool);
                        if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
                        {
                            MeadowFunc.RemoveRestrictedItemsInArenaFromPool(ref newPool);
                        }
                        ObjectData objectData = newPool.Pool();
                        if (objectData.IsDefault) { continue; }
                        objectList.Add( objectData );
                    }
                    if (objectList.Count == 0) { continue; }
                    ArenaItemSpawn itemSpawn = new(spot, (int)(BTWFunc.Random(20, 30) * BTWFunc.FrameRate), objectList);
                    if (BTWPlugin.meadowEnabled && MeadowFunc.ShouldHoldFireFromOnlineArenaTimer())
                    {
                        itemSpawn.spawnTime = (int)BTWFunc.Random(MeadowFunc.ArenaCountdownTimerCurrent());
                        BTWPlugin.Log($"Meadow setting detected ! Timer set at <{itemSpawn.spawnTime}> since timer is at <{MeadowFunc.ArenaCountdownTimerCurrent()}>");
                        itemSpawn.spawnTime = (int)Mathf.Clamp(itemSpawn.spawnTime, BTWFunc.FrameRate * 5f, BTWFunc.FrameRate * 60f);
                    }
                    this.itemSpawns.Add(itemSpawn);
                    itemSpawn.itemSpawnManager = this;
                    room.AddObject(itemSpawn);

                    throwableCount += spawning;
                }
                else
                {
                    throwableCount++;
                }
            }
        }

        if (this.doCheckMiscellaniousCount)
        {
            int miscellaniousCount = MiscellaniousAvailable();
            while (miscellaniousCount < this.playersCount / 2)
            {
                int spawning = (int)(Mathf.Pow(BTWFunc.random, 5) * 2) + 1;
                Vector2 spot = GetRandomSpawnPos();
                BTWPlugin.Log($"Not enough miscellanious ! <{miscellaniousCount}> for <{this.playersCount}> player ! Adding <{spawning}> miscellanious at [{spot}]");

                if (spot != Vector2.zero)
                {
                    List<ObjectData> objectList = new();
                    for (int i = 1; i <= spawning; i++)
                    {
                        ObjectDataPool newPool = new(ArenaItemSpawn.othersPool);
                        if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
                        {
                            MeadowFunc.RemoveRestrictedItemsInArenaFromPool(ref newPool);
                        }
                        ObjectData objectData = newPool.Pool();
                        if (objectData.IsDefault) { continue; }
                        objectList.Add( objectData );
                    }
                    if (objectList.Count == 0) { continue; }
                    ArenaItemSpawn itemSpawn = new(spot, (int)(BTWFunc.Random(5, 60) * BTWFunc.FrameRate), objectList);
                    this.itemSpawns.Add(itemSpawn);
                    itemSpawn.itemSpawnManager = this;
                    room.AddObject(itemSpawn);

                    miscellaniousCount += spawning;
                }
                else
                {
                    miscellaniousCount++;
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
    public bool doCheckThrowableCount = true;
    public bool doCheckMiscellaniousCount = true;
    public bool noSpears = false;

    public bool doRespawn = true;
    public int RespawnAttemps = 10;
    public int RandomSpawnAttemps = 5;
    public Counter respawnCount = new(BTWFunc.FrameRate * 60);

    public int objectLimit = 5;
    public float objectLimitRadius = 20;
    public float spawnerLimitRadius = 50;
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
            if (ArenaItemSpawnManager.TryGetManager(self, out var itemSpawnManager) 
                && (!self.game.GamePaused || (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowLobby())))
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