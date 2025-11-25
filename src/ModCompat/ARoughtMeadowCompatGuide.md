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


2. **Create a hook funcitno for each Rain Meadow functions that checks for `OnlinePhysicalObject` (there's alot)**
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

## Set up a State
Ok, now is the REAL part where you actually sync some objects ! 
This is the first method to sync some data.

### What's a state ?
A state is a bunch of data that'll get synced by the subscribing system of Meadow. 
To make it short : the client wanna see the object, enter a rooms, etc, but the object/place is not "owned" by the client, so it'll ask the owner for update on that object.
THIS is where you want to send values that frequently (and unpredictably) update over time. 
You can link those values to any `PhysicalObject` that has an `OnlinePhysicalObject` (actually, anything that has a `OnlineEntity`), or any Ressource (world, room, hell the lobby even) that has an `OnlineResource`.

Anyway, here's the way to make a state for an entity :

### Creating some `EntityData` 


1. **Create a new `OnlineEntity.EntityData`**
```cs
public class MyNewEntityData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public MyNewEntityData() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        // We'll return to that later
    }
}
```
There you go, some data that are just waiting to be send ! But we're just started, so let's not get too ahead of ourselves.
Your `EntityData` needs a way to make `State`s, a one time package that'll be send and opened regularly. So let's create just that.
> [!TIP]
> You can create it inside your `EntityData` for convenience. I'll be doing just that for the demo.


2. **Make the State**
```cs
    // Inside the EntityData
    public class State : EntityDataState
    {
        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            // Setting the values
        }
        //--------- Functions
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            // Checking and reading the values
        }
        public override Type GetDataType()
        {
            return typeof(OnlineStaticChargeManagerData); // Get what type of Data to expect
        }
    }
```
> [!NOTE]
> `GetDataType` will be used by Meadow to know what kind of State its dealing with.


3. **Set the values to be synced**
THERE you'll be putting the values you need to sync ! SO let's create them.
Every variable needs an `[OnlineField]` (for int and bool) or an `[OnlineFieldHalf]` (for float) to say how to encode the sent data.
```cs
        // Inside the State
        [OnlineFieldHalf]
        public float MyFloat;
        [OnlineField]
        public int MyInt;
        [OnlineField]
        public bool MyBool;
        [OnlineField]
        public byte MyByte;
```

Now let's check the other functions of the states :
The constructor is where the owner set the current data, and `ReadTo` is where the other client reads the data and sets its own values accordingly. 
> [!WARNING]
> __A WARNING I WAS GIVEN :__ Set your `ReadTo()` defensively. Basically anything could be send there.


4. **Write and Read the values**
So let's also set the function :
```cs
        // Inside the State
        public State(OnlineEntity onlineEntity)
        {
            // Optional checks

            this.MyFloat = GetMyFloatForEntityData(onlineEntity); // I've put some placeholder function, but this is where you place the values that should be send.
            this.MyInt = GetMyIntForEntityData(onlineEntity);
            this.MyBool = GetMyBoolForEntityData(onlineEntity);
            this.MyByte = GetMyByteForEntityData(onlineEntity);
        }
        
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if (ThisDataIsInvalid(onlineEntity)) { return; } // Check if the data should be read, REALLY important to avoid issues

            SetMyFloatForEntityData(onlineEntity, this.MyFloat); // There we do the opposite, instead of taking the values, we set them
            SetMyIntForEntityData(onlineEntity, this.MyInt);
            AnotherClass.ImportantBoolToBeSynced = this.MyBool; // You can change anything from those values really
            SetMyByteForEntityData(onlineEntity, this.MyByte);
        }
```
> [!TIP]
> If you wanna get back the Object you got it from, you can use the `OnlineEntity` in parameters and check for `(onlineEntity as OnlinePhysicalObject)?.apo` for the `AbstractPhysicalObject`.


5. **Connect everything together**
Finally, you just have to pass your newly made state in the `EntityData` 
```cs
    // Inside the EntityData
    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
         return new State(entity);
    }
```

Your data is ready ! It should look like this :
```cs
public class MyNewEntityData : OnlineEntity.EntityData
{
    [UsedImplicitly]
    public MyNewEntityData() { }

    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(entity);
    }
    //-------- State

    public class State : EntityDataState
    {
        //--------- Variables
        [OnlineFieldHalf]
        public float MyFloat;
        [OnlineField]
        public int MyInt;
        [OnlineField]
        public bool MyBool;
        [OnlineField]
        public byte MyByte;

        //--------- ctor

        [UsedImplicitly]
        public State() { }
        public State(OnlineEntity onlineEntity)
        {
            // Optional checks

            this.MyFloat = GetMyFloatForEntityData(onlineEntity); // I've put some placeholder function, but this is where you place the values that should be send.
            this.MyInt = GetMyIntForEntityData(onlineEntity);
            this.MyBool = GetMyBoolForEntityData(onlineEntity);
            this.MyByte = GetMyByteForEntityData(onlineEntity);
        }
      
        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if (ThisDataIsInvalid(onlineEntity)) { return; } // Check if the data should be read, REALLY important to avoid issues

            SetMyFloatForEntityData(onlineEntity, this.MyFloat); // There we do the opposite, instead of taking the values, we set them
            SetMyIntForEntityData(onlineEntity, this.MyInt);
            AnotherClass.ImportantBoolToBeSynced = this.MyBool; // You can change anything from those values really
            SetMyByteForEntityData(onlineEntity, this.MyByte);
        }

        public override Type GetDataType()
        {
            return typeof(MyNewEntityData);
        }

    }
}
```

Finally, you need to attach that state to some online object ! For that you simply need to write.
```cs
// blah blah your code about the entity in the MeadowCompat class
onlineEntity.AddData(new MyNewEntityData());
```
Inside whatever function you run when you wanna sync those data (be sure that Meadow is enabled !), and you should be good to go !
> [!TIP]
> You can get the `OnlineEntity` of any `Creature` with `creature.abstractCreature.GetOnlineCreature()`

## Set up a RPC
Yep, there's actually 2 method to sync. This one is quite straightforward too, doesn't mean you should spam it !

### What's a RPC ?
An RPC is a one time event sent to the designated `OnlinePlayer`(s). It send data in repeat until the client confirm reception, so it's kind of a bruteforce method to get data across clients.
The other client will receive it as fast as it is possible with both networks.

### Creating an RPC


1. **Creating the RPC function**
First off, you need a function that'll handle the reception of the RPC. This will need to have `[RPCMethod]` right above it. 
> [!NOTE]
> There's no particular parameters to set, except for the first one, but do note that there's no fancy compression methods to send the data, so be careful of what you actually send over.
> Also, send the `OnlineEntity` of a creature across if you need to, not the `Creature` itself, since it's not technically the same across all clients !

```cs
[RPCMethod]
public static void RPCEvent_ChangeThatParticularBool(RPCEvent _, OnlinePhysicalObject playerOpo, bool thisParticularBool)
{
    // Eventual Check(s)
    if (!ThisOnlinePlayerHasThisParticularBool(playerOpo)) { return; }

    // The action you want to do when receiving the RPC
    ChangeThisParticularBoolOfOnlinePlayer(thisParticularBool);
}
```


2. **Sending the RPC**
You then need to send that RPC to the designated `OnlinePlayer` !
Usually, I find myself wanting to send the RPC to all other players. To make it simpled, I did 2 functions that does just that for me :
```cs
public static void InvokeAllOtherPlayerWithRPC(Delegate del, params object[] args)
{
    foreach (var player in OnlineManager.players)
    {
        if (!player.isMe)
        {
            player.InvokeRPC(del, args);
        }
    }
}

public static void InvokeAllOtherPlayerWithRPCOnce(Delegate del, params object[] args)
{
    foreach (var player in OnlineManager.players)
    {
        if (!player.isMe)
        {
            player.InvokeOnceRPC(del, args);
        }
    }
}
```
> [!CAUTION]
> The only real way I found to find the `OnlinePlayer` from a specific slugcat (that is not yours) has been to find the owner of that slugcat (`OnlinePlayer onlinePlayer = slugcat.owner`). I was warned it may be wrong on piggyback, but I still have to check that exception. 

> [!TIP]
> `InvokeOnceRPC` sends only the RPC once. This does not guarantee that the other clien twill receive it, but avoids spam for RPCs that aren't this important to be always received.

Then, you just need to send the RPC ! 
I usually make a funciton my other files in my project can use after checking that Rain Meadow is active and this event was trigered.
A function with this example would look like this :

```cs
public static void ChangeThatOneBool_RPC(Player player)
{
    // eventual check
    OnlineCreature onlinePlayer = player.abstractPlayer.GetOnlineCreature();
    if (!ThisOnlinePlayerHasThisParticularBool(playerOpo) || onlinePlayer == null) { return; }
    bool thisBool = GetThisParticularBoolOfPlayer(player);
    
    InvokeAllOtherPlayerWithRPC(
        typeof(MeadowCompat).GetMethod(nameof(RPCEvent_ChangeThatParticularBool)).CreateDelegate(
            typeof(Action<RPCEvent, OnlinePhysicalObject, bool>)),
            onlinePlayer, thisBool
    );
}
```

The `InvokeRPC` has two arguments :
- `Delegate`, which is a description of the arguments of your function. It should look like this : `typeof(Action<RPCEvent, argument1, argument2, argument3,...>))`
- `params object[]`, which are all the arguements that should be passed

