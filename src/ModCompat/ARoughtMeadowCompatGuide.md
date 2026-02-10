# A rough and probably innaccurate guide for Meadow Compatibilty
Hey there ! Looking to make your mod Meadow compatible but don't know where to start ? 
Well I was in the same possition not so long ago, and decided to record here everything I've learned so far so we can hopefully see more silly slugcats (and others) in the Arena !
Anyway, let's get started right away.

## Soft Dependancy
If you want, like me, for you mod to work with meadow but still work without, you need to make *sure* that you don't call libraries that don't exist ! 
So we first of all need a way to detect if meadow is loaded.
```cs
public static bool meadowEnabled = false;
public static void CheckMods()
{
    foreach (ModManager.Mod mod in ModManager.ActiveMods)
    {
        if (mod.id == "henpemaz_rainmeadow" && mod.enabled)
        {
            meadowEnabled = true; // We found Rain Meadow !
        }
    }
}
```

You'll then need a dedicated .cs file that loads all the Meadow libraries that you need. 
Be sure it's in a **separated place** than the rest of your code to avoid accidentally running it when Meadow is not enabled.
> [!NOTE]
> You can find the reference of Meadow somewhere around `Steam\steamapps\workshop\content\312520\3388224007\plugins`. To check the functions, I'd recommend checking their [GitHub Page](https://github.com/henpemaz/Rain-Meadow).
```cs

using RainMeadow;
using MyPlugin;

public class MeadowCompat
{
    // All your code that has to do with Rain Meadow

    public static void ApplyHooks()
    {
        // All the hooks you'll need to add if Rain Meadow is enabled
    }
}
```
> [!CAUTION]
> Be sure to call `MeadowCompat` ONLY, and ONLY if you are SURE that Meadow is loaded (aka `Plugin.meadowEnabled` is true). ONLY use Meadow librairy here, or the game WILL softlock when Rain Meadow is disabled.

You can then hook all the functions your want from MeadowCompat on `PostModsInit`
```cs
public void OnEnable()
{
    // Your code
    On.RainWorld.PostModsInit += PostModsLoad;
}
public static void ApplyMeadowHooks()
{
    if (meadowEnabled)
    {
        MeadowCompat.ApplyHooks();
    }
}
```

> [!TIP]
> Hook on `On.RainWorld.PostModsInit` will guarantee that Rain Meadow was indeed loaded, skipping the problematic of mod load order (probably with some consequences that I haven't met yet).

Everything below there is assumed to be put in 

## What do you need to sync exactly ?
Meadow will attempt to automatically sync :
- Any `PhysicalObject` present
- The position of the slugcat
- Some states of the creatures (like alive/dead)
- `Creature.Violence()`
- And many more that I haven't explored yet !

The rest (custom forces, UIs, modification of oxygen level, most of `UpdatableAndDeletable`s, etc...) doesn't seem to be handled by Rain Meadow, *which is fair*.
The best way to check for ambiguity is to test it, so hell I'll expand that list if I find more.

## Stop a `PhysicalObject` from replicating
To stop a `PhysicalObject` from replicating (without Rain Meadow screaming hell at you in the process), you'll need multiple hooks on Meadow's Session handlers to ignore the said objects.
Hooking to another mod can be quite tricky. Luckily, I already did all the work. 
> [!NOTE]
> This isn't recommended, but hell if you really need to do so, here's how :


1. **Set up a list of the objects you don't want synced**
```cs
public static HashSet<AbstractPhysicalObject.AbstractObjectType> deniedSyncedObjects = new()
{
    ObjectType1,
    ObjectType2,
    ObjectType3 // and so on
};
// It's one way to do it, you can as well make a function that takes a PhysicalObject and adapt that rest accordingly
```


2. **Create a hook function for each Rain Meadow functions that checks for `OnlinePhysicalObject` (there's alot)**
```cs
private static bool OnlineGameMode_DoNotRegister(Func<OnlineGameMode, OnlineResource, AbstractPhysicalObject, bool> orig, OnlineGameMode self, OnlineResource resource, AbstractPhysicalObject apo)
{
    if (deniedSyncedObjects.Contains(apo.type)) { 
        return false; 
    }
    return orig(self, resource, apo);
}
private static bool OnlineGameMode_DoNotSyncInWorld(Func<OnlineGameMode, WorldSession, AbstractPhysicalObject, bool> orig, OnlineGameMode self, WorldSession ws, AbstractPhysicalObject apo)
{
    if (deniedSyncedObjects.Contains(apo.type)) { 
        return false; 
    }
    return orig(self, ws, apo);
}
private static bool OnlineGameMode_DoNotSyncInRoom(Func<OnlineGameMode, RoomSession, AbstractPhysicalObject, bool> orig, OnlineGameMode self, RoomSession rs, AbstractPhysicalObject apo)
{
    if (deniedSyncedObjects.Contains(apo.type)) { 
        return false; 
    }
    return orig(self, rs, apo);
}
private static void WorldSession_DoNotRegisterExit(Action<WorldSession, AbstractPhysicalObject> orig, WorldSession self, AbstractPhysicalObject apo)
{
    if (deniedSyncedObjects.Contains(apo.type)) { 
        return; 
    }
    orig(self, apo);
}
private static void RoomSession_DoNotRegisterExit(Action<RoomSession, AbstractPhysicalObject> orig, RoomSession self, AbstractPhysicalObject apo)
{
    if (deniedSyncedObjects.Contains(apo.type)) { 
        return; 
    }
    orig(self, apo);
}
```


3. **Hook the says functions**
```cs
public static void ApplyHooks()
{
    foreach (var gamemodeDict in OnlineGameMode.gamemodes) // For each gamemode that currently exist.
    {
        Type gameModeType = gamemodeDict.Value;
        new Hook(gameModeType.GetMethod("ShouldRegisterAPO"), OnlineGameMode_DoNotRegister);
        new Hook(gameModeType.GetMethod("ShouldSyncAPOInWorld"), OnlineGameMode_DoNotSyncInWorld);
        new Hook(gameModeType.GetMethod("ShouldSyncAPOInRoom"), OnlineGameMode_DoNotSyncInRoom);
    }
    new Hook(typeof(WorldSession).GetMethod(nameof(WorldSession.ApoLeavingWorld)), WorldSession_DoNotRegisterExit);
    new Hook(typeof(RoomSession).GetMethod(nameof(RoomSession.ApoLeavingRoom)), RoomSession_DoNotRegisterExit);
}
```
> [!IMPORTANT]
> I couldn't put a nameof for `.GetMethod("ShouldRegisterAPO"),`, `.GetMethod("ShouldSyncAPOInWorld")` and `.GetMethod("ShouldSyncAPOInRoom")`, so I typed the string manually. If that happened to break, it's probably because the function changed names and the compilator couldn't detect it.

From there, Meadow should completely ignore all items in that list and not throw any errors !
Just know that the sync of those said objects becomes entirely YOUR job at that point.

> [!TIP]
> You can use states to sync those object, althrough that'd mean that you link it to another `OnlineEntity`... which in most case is probably a waste of energy + some new bugs waiting to happend.

## Set up a State or RPC
Moved to here : https://rainworldmodding.miraheze.org/wiki/Rain_Meadow%27s_RPC_and_States   
