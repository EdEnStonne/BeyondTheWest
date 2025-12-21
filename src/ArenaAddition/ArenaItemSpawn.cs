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

namespace BeyondTheWest.ArenaAddition;
public class ArenaItemSpawn : UpdatableAndDeletable, IDrawable
{
    public struct ArenaItemSpawnSetting
    {
        public float multiplier;
        public float multiplierPerPlayer;
        public bool doScalePerPlayer;
        public bool diversity;
        public bool randomItem;
        public bool newSystem;
        public ArenaItemSpawnSetting()
        {
            this.newSystem = BTWRemix.NewItemSpawningSystem.Value;
            this.multiplier = BTWRemix.ItemSpawnMultiplier.Value;
            this.multiplierPerPlayer = BTWRemix.ItemSpawnMultiplierPerPlayers.Value;
            this.doScalePerPlayer = BTWRemix.DoItemSpawnScalePerPlayers.Value;
            this.randomItem = BTWRemix.ItemSpawnRandom.Value;
            this.diversity = BTWRemix.ItemSpawnDiversity.Value;

            if (Plugin.meadowEnabled && MeadowFunc.IsMeadowArena())
            {
                MeadowFunc.SetArenaItemSpawnSettings(ref this);
            }
        }
    }
    public static List<List<ObjectData>> testObjectLists = new()
    {
        new(){
            new(ObjectType.Rock, 0)
        },
        new(){
            new(ObjectType.Spear, 0)
        },
        new(){
            new(ObjectType.ScavengerBomb, 0)
        },
        new(){
            new(ObjectType.SporePlant, 0)
        },
        new(){
            new(ObjectType.Rock, 0),
            new(ObjectType.Rock, 0),
            new(ObjectType.Rock, 0),
        },
        new(){
            new(ObjectType.Spear, 1),
            new(ObjectType.Spear, 2),
            new(ObjectType.Spear, 3),
            new(ObjectType.Spear, 4),
        },
    };
    public static List<ObjectData> GetRandomTestList()
    {
        return testObjectLists[(int)Mathf.Clamp(BTWFunc.Random(testObjectLists.Count), 0, testObjectLists.Count - 1)];
    }
    
    public static ObjectDataPool spearPool = new();
    public static ObjectDataPool rockPool = new();
    public static ObjectDataPool othersPool = new();
    public static ObjectDataPool allPool = new();

    public ArenaItemSpawn(Vector2 position, int spawnTime, List<ObjectData> objectList, bool notifyMeadow = true, bool isFake = false)
    {
        this.pos = position;
        this.spawnTime = spawnTime;
        this.objectList = objectList;
        this.isFake = isFake;

        if (Plugin.meadowEnabled && notifyMeadow && !isFake)
        {
            MeadowCalls.BTWArena_RPCAddItemSpawnerToAll(this);
        }
    }
    public ArenaItemSpawn(Vector2 position, int spawnTime, ObjectType objectType, int intdata = 0, bool notifyMeadow = false)
        : this(position, spawnTime, new List<ObjectData>{ new(objectType, intdata) }, notifyMeadow) {}
    public ArenaItemSpawn(Vector2 position, List<ObjectData> objectList, bool notifyMeadow = false)
        : this(position, (int)(5 * BTWFunc.FrameRate + BTWFunc.Random(10 * BTWFunc.FrameRate)), objectList, notifyMeadow) {}
    public ArenaItemSpawn(Vector2 position, ObjectType objectType, int intdata = 0, bool notifyMeadow = false)
        : this(position, new List<ObjectData>{ new(objectType, intdata) }, notifyMeadow) {}
    public ArenaItemSpawn(Vector2 position, bool notifyMeadow = false)
        : this(position, ObjectType.Rock, 0, notifyMeadow) {}

    public void SpawnItems()
    {
        if (this.room != null)
        {
            World world = this.room.world;
            WorldCoordinate coords = this.room.GetWorldCoordinate(this.pos);
            Plugin.Log($"Spawning items at [{pos}]/[{coords}] !");
            for (int j = 0; j < this.objectList.Count; j++)
            {
                EntityID newID = world.game.GetNewID();

                try
                {
                    if (this.objectList[j].objectType == ObjectType.Rock)
                    {
                        AbstractPhysicalObject item = new(
                            world, ObjectType.Rock, null, coords, newID
                        );

                        this.room.abstractRoom.AddEntity(item);
                        item.RealizeInRoom();
                    }
                    else if (this.objectList[j].objectType == ObjectType.Spear)
                    {
                        AbstractSpear item = new(
                            world, null, coords, newID, false);
                        if (this.objectList[j].intData == 1)
                        {
                            item.explosive = true;
                        }
                        else if (this.objectList[j].intData == 2)
                        {
                            item.electric = true;
                            item.electricCharge = (int)(1 + BTWFunc.random * 4);
                        }
                        else if (this.objectList[j].intData == 3)
                        {
                            item.hue = BTWFunc.random;
                        }
                        else if (this.objectList[j].intData == 4)
                        {
                            item.poison = 1 + BTWFunc.random;
                            item.poisonHue = BTWFunc.random;
                        }

                        this.room.abstractRoom.AddEntity(item);
                        item.RealizeInRoom();
                    }
                    else if (this.objectList[j].objectType == ObjectType.ScavengerBomb)
                    {
                        AbstractPhysicalObject item = new(
                            world, ObjectType.ScavengerBomb, null, coords, newID
                        );

                        this.room.abstractRoom.AddEntity(item);
                        item.RealizeInRoom();
                    }
                    else if (this.objectList[j].objectType == ObjectType.SporePlant)
                    {
                        SporePlant.AbstractSporePlant item = new(
                            world, null, coords, newID, -2, -2, null, false, true
                        );

                        this.room.abstractRoom.AddEntity(item);
                        item.RealizeInRoom();
                    }
                    else if (ModManager.MSC && this.objectList[j].objectType == MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.FireEgg)
                    {
                        MoreSlugcats.FireEgg.AbstractBugEgg item = new(
                            world, null, coords, newID, BTWFunc.random
                        );

                        this.room.abstractRoom.AddEntity(item);
                        item.RealizeInRoom();
                    }
                    else if (ModManager.DLCShared && this.objectList[j].objectType == DLCSharedEnums.AbstractObjectType.LillyPuck)
                    {
                        MoreSlugcats.LillyPuck.AbstractLillyPuck item = new(
                            world, null, coords, newID, 3, -1, -1, null
                        );

                        this.room.abstractRoom.AddEntity(item);
                        item.RealizeInRoom();
                    }
                    else 
                    {
                        if (AbstractConsumable.IsTypeConsumable(this.objectList[j].objectType))
                        {
                            AbstractConsumable item = new(
                                world, this.objectList[j].objectType, null, coords, newID, -1, -1, null
                            );

                            this.room.abstractRoom.AddEntity(item);
                            item.RealizeInRoom();
                        }
                        else
                        {
                            AbstractPhysicalObject item = new(
                                this.room.world, this.objectList[j].objectType, null, room.GetWorldCoordinate(this.pos), this.room.world.game.GetNewID()
                            );
                            this.room.abstractRoom.AddEntity(item);
                            item.RealizeInRoom();
                        }
                    }
                    Plugin.Log($"   > Spawned [{this.objectList[j].objectType}]<{this.objectList[j].intData}> at [{this.pos}] !");
                }
                catch (Exception ex)
                {
                    Plugin.logger.LogError($"   > Countn't spawn [{this.objectList[j].objectType}]<{this.objectList[j].intData}> at [{this.pos}] ! Maybe item not supported ? \n >Error : {ex}");
                }
            }
        }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.room != null && !this.slatedForDeletetion)
        {
            if (this.spawnCount < this.spawnTime)
            {
                this.spawnCount++;
                if (this.spawnCount == this.spawnTime)
                {
                    this.room.PlaySound(SoundID.HUD_Pause_Game, this.pos, 0.35f, 0.65f + BTWFunc.random * 0.25f);
                    if (!this.isFake)
                    {
                        SpawnItems();
                    }
                }
            }
            else if (this.destructionCount < destructionTime)
            {
                this.destructionCount++;
            }
            else
            {
                this.Destroy();
            }
        }
        else
        {
            this.Destroy();
        }
    }
    public override void Destroy()
    {
        base.Destroy();
        if (itemSpawnManager != null)
        {
            itemSpawnManager.itemSpawns.Remove(this);
        }
    }

    public float GetCircleFraction(int circle)
    {
        float timePerCircle = (float)this.spawnTime / this.circlesAmount;
        return 1 - Mathf.Clamp01((this.spawnCount - timePerCircle * (circle - 1)) / timePerCircle);
    }
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        this.circlesAmount = (int)Mathf.Clamp(this.spawnTime / (BTWFunc.FrameRate * 1.5f), 5, 12);
        sLeaser.sprites = new FSprite[this.circlesAmount + 1 + this.objectList.Count];

        FSprite SpawnAura = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
            color = this.baseColor,
            alpha = 0f,
            scale = 0.8f
        };
        sLeaser.sprites[0] = SpawnAura;

        for (int i = 1; i <= this.circlesAmount; i++)
        {
            sLeaser.sprites[i] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
                color = this.baseColor,
                alpha = 0f,
                scale = 0.25f
            };
        }
        for (int j = 0; j < this.objectList.Count; j++)
        {
            sLeaser.sprites[j + 1 + this.circlesAmount] = new FSprite(
                ItemSymbol.SpriteNameForItem(this.objectList[j].objectType, this.objectList[j].intData), true)
            {
                color = ItemSymbol.ColorForItem(this.objectList[j].objectType, this.objectList[j].intData),
                alpha = 0.5f,
                scale = 0.75f
            };
            if (ModManager.Watcher && this.objectList[j].objectType == ObjectType.Spear && this.objectList[j].intData == 4)
            {
                sLeaser.sprites[j + 1 + this.circlesAmount].color = new Color(0.35f, 0.15f, 0.85f);
            }
            Plugin.Log($"Initiating item sprite [{this.objectList[j].objectType}] with data <{this.objectList[j].intData}> : sprite name is <{sLeaser.sprites[j + 1 + this.circlesAmount].element.name}> and color is [{sLeaser.sprites[j + 1 + this.circlesAmount].color}]");
        }

        this.AddToContainer(sLeaser, rCam, null);
    }
    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
        {
            sLeaser.CleanSpritesAndRemove();
            return;
        }
        float easedDesc = BTWFunc.EaseIn(1 - this.FractionDestruct, 2);
        float easedSpawn = BTWFunc.EaseOut(1 - this.FractionSpawn, 4);

        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.x = pos.x - camPos.x;
            sprite.y = pos.y - camPos.y;
            sprite.alpha = 0f;
        }

        sLeaser.sprites[0].scale = 0.2f * easedSpawn + 0.6f * easedDesc;
        sLeaser.sprites[0].alpha = 0.9f - (0.4f * easedSpawn + 0.5f * easedDesc);

        for (int i = 1; i <= this.circlesAmount; i++)
        {
            sLeaser.sprites[i].x += Mathf.Sin(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 10f;
            sLeaser.sprites[i].y += Mathf.Cos(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 10f;
            sLeaser.sprites[i].alpha = 0.4f + 0.6f * (1 - GetCircleFraction(i));
            sLeaser.sprites[i].scale = 0.3f * BTWFunc.EaseOut(GetCircleFraction(i), 3);
        }
        for (int j = 0; j < this.objectList.Count; j++)
        {
            sLeaser.sprites[j + 1 + this.circlesAmount].y += 25f;
            sLeaser.sprites[j + 1 + this.circlesAmount].x += -(this.objectList.Count - 1) * 6f + j * 12f;
            sLeaser.sprites[j + 1 + this.circlesAmount].alpha = Mathf.Clamp01(easedDesc - 0.5f * easedSpawn);
        }
    }
    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) 
    { 
        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.color = Color.Lerp(sprite.color, palette.shortCutSymbol, 0.2f);
        }
    }
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        foreach (FSprite sprite in sLeaser.sprites)
        {
            rCam.ReturnFContainer("HUD").AddChild(sprite);
        }
    }

    public List<ObjectData> objectList = new();
    public Vector2 pos;

    public Color baseColor = Color.gray;
    public int spawnCount = 0;
    private int destructionCount = 0;
    private int circlesAmount = 0;
    
    public int spawnTime = BTWFunc.FrameRate * 5;
    private const int destructionTime = (int)(BTWFunc.FrameRate * 0.5);

    public bool isFake = false;
    public ArenaItemSpawnManager itemSpawnManager;

    public float FractionSpawn
    {
        get
        {
            return Mathf.Clamp01((float)spawnCount / spawnTime);
        }
    }
    public float FractionDestruct
    {
        get
        {
            return Mathf.Clamp01((float)destructionCount / destructionTime);
        }
    }
}

public static class ArenaItemSpawnHooks
{
    public static void ApplyHooks()
    {
        InitPools();
        IL.ArenaGameSession.SpawnItem += ArenaGameSession_NewSpawnSystem;
        Plugin.Log("ArenaItemSpawnHooks ApplyHooks Done !");
    }

    private static void InitPools()
    {
        ArenaItemSpawn.rockPool.AddToPool(ObjectType.Rock, 20);
        ArenaItemSpawn.rockPool.AddToPool(ObjectType.ScavengerBomb, 7);

        ArenaItemSpawn.spearPool.AddToPool(ObjectType.Spear, 35);
        ArenaItemSpawn.spearPool.AddToPool(ObjectType.Spear, 1, 15);

        ArenaItemSpawn.othersPool.AddToPool(ObjectType.SporePlant, 12);
        ArenaItemSpawn.othersPool.AddToPool(ObjectType.KarmaFlower, 1);
        ArenaItemSpawn.othersPool.AddToPool(ObjectType.FirecrackerPlant, 5);
        ArenaItemSpawn.othersPool.AddToPool(ObjectType.JellyFish, 7);
        ArenaItemSpawn.othersPool.AddToPool(ObjectType.EggBugEgg, 5);

        foreach (var poolitem in ArenaItemSpawn.rockPool.pool)
        {
            ArenaItemSpawn.allPool.AddToPool(poolitem);
        }
        foreach (var poolitem in ArenaItemSpawn.spearPool.pool)
        {
            ArenaItemSpawn.allPool.AddToPool(poolitem);
        }
        foreach (var poolitem in ArenaItemSpawn.othersPool.pool)
        {
            ArenaItemSpawn.allPool.AddToPool(poolitem);
        }
    }

    private static int GenerateItemCount(float chance, ArenaGameSession arena)
    {
        ArenaItemSpawn.ArenaItemSpawnSetting settings = new();
        if (arena is CompetitiveGameSession && settings.newSystem)
        {
            float multiplier = settings.multiplier;
            int playersCount = arena.arenaSitting.players.Count;
            int spawnCount = 0;

            if (Plugin.meadowEnabled && MeadowFunc.IsMeadowArena())
            {
                if (!MeadowFunc.IsMeadowHost()) { return 0; }
                playersCount = MeadowFunc.GetPlayersReadyForArena();
            }

            if (settings.doScalePerPlayer)
            {
                multiplier += settings.multiplierPerPlayer 
                    * (playersCount - 1);
            }
            while (multiplier > 0)
            {
                if ((multiplier > 1 || multiplier > BTWFunc.random) && BTWFunc.random < chance)
                {
                    spawnCount++;
                }
                multiplier--;
            }
            return spawnCount;
        }
        return BTWFunc.random > chance ? 0 : 1;
    }
    private static bool AddItemSpawnerIL(ArenaGameSession arena, Room room, ObjectType objectType, PlacedObject placedObject, int count)
    {
        ArenaItemSpawn.ArenaItemSpawnSetting settings = new();
        if (arena is CompetitiveGameSession && settings.newSystem)
        {
            ArenaItemSpawnManager.AddItemSpawnerFromPlacedObject(arena, room, objectType, placedObject, count);
            return true;
        }
        return false;
    }
    private static void ArenaGameSession_NewSpawnSystem(ILContext il)
    {
        Plugin.Log("ArenaItemSpawnHooks IL 1 starts");
        try
        {
            ILCursor cursor = new(il);
            MethodBody body = il.Body;

            VariableDefinition spawnCount = new VariableDefinition(
                il.Module.ImportReference(typeof(int))
            );
            body.Variables.Add(spawnCount);
            body.InitLocals = true;

            Plugin.Log($"Added new variable [{spawnCount}] to IL");
            
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchCall(typeof(UnityEngine.Random).GetProperty(nameof(UnityEngine.Random.value)).GetGetMethod()),
                x => x.MatchLdarg(2),
                x => x.MatchLdfld<PlacedObject>(nameof(PlacedObject.data)),
                x => x.MatchIsinst<MItemData>(),
                x => x.MatchLdfld<MItemData>(nameof(MItemData.chance))
            ))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(GenerateItemCount);
                cursor.Emit(OpCodes.Stloc, spawnCount);
                cursor.Emit(OpCodes.Ldloc, spawnCount);
                cursor.Emit(OpCodes.Conv_R4);
            }
            else
            {
                Plugin.logger.LogError("IL hook 1 not found :<");
                Plugin.Log(il);
            }
            
            if (cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(0),
                x => x.MatchLdsfld<ObjectType>(nameof(ObjectType.Spear)),
                x => x.MatchCall(out _),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<Room>(nameof(Room.world)),
                x => x.MatchLdnull(),
                x => x.MatchLdarg(1),
                x => x.MatchLdarg(2),
                x => x.MatchLdfld<PlacedObject>(nameof(PlacedObject.pos)),
                x => x.MatchCallvirt<Room>(nameof(Room.GetWorldCoordinate)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<GameSession>(nameof(GameSession.game)),
                x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GetNewID)),
                x => x.MatchLdloc(1)
            ))
            {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldloc_0);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.Emit(OpCodes.Ldloc, spawnCount);
                cursor.EmitDelegate(AddItemSpawnerIL);
                cursor.Emit(OpCodes.Brfalse, cursor.Next);
                cursor.Emit(OpCodes.Ret);
            }
            else
            {
                Plugin.logger.LogError("IL hook 2 not found :<");
                Plugin.Log(il);
            }
            
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
            Plugin.Log(il);
        }
        Plugin.Log("ArenaItemSpawnHooks IL 1 ended !");
    }
}