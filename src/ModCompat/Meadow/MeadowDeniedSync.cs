using System;
using RainMeadow;
using JetBrains.Annotations;
using System.Linq;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;

namespace BeyondTheWest.MeadowCompat;
public static class MeadowDeniedSync
{
    public static void ApplyHooks()
    {
        foreach (var gamemodeDict in OnlineGameMode.gamemodes)
        {
            Type gameModeType = gamemodeDict.Value;
            new Hook(gameModeType.GetMethod(nameof(OnlineGameMode.ShouldRegisterAPO)), OnlineGameMode_DoNotRegister);
            new Hook(gameModeType.GetMethod(nameof(OnlineGameMode.ShouldSyncAPOInWorld)), OnlineGameMode_DoNotSyncInWorld);
            new Hook(gameModeType.GetMethod(nameof(OnlineGameMode.ShouldSyncAPOInRoom)), OnlineGameMode_DoNotSyncInRoom);
        }
        new Hook(typeof(WorldSession).GetMethod(nameof(WorldSession.ApoLeavingWorld)), WorldSession_DoNotRegisterExit);
        new Hook(typeof(RoomSession).GetMethod(nameof(RoomSession.ApoLeavingRoom)), RoomSession_DoNotRegisterExit);
    
        BTWPlugin.Log("MeadowDeniedSync ApplyHooks Done !");
    }
    
    public static HashSet<AbstractPhysicalObject.AbstractObjectType> deniedSyncedObjects = new()
    {
        AbstractEnergyCore.EnergyCoreType
    };

    private static bool OnlineGameMode_DoNotRegister(Func<OnlineGameMode, OnlineResource, AbstractPhysicalObject, bool> orig, OnlineGameMode self, OnlineResource resource, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            BTWPlugin.Log(apo.ToString() + " shall not be replicated !");
            return false; 
        }
        return orig(self, resource, apo);
    }
    private static bool OnlineGameMode_DoNotSyncInWorld(Func<OnlineGameMode, WorldSession, AbstractPhysicalObject, bool> orig, OnlineGameMode self, WorldSession ws, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            BTWPlugin.Log(apo.ToString() + " shall not be sync (world) !");
            return false; 
        }
        return orig(self, ws, apo);
    }
    private static bool OnlineGameMode_DoNotSyncInRoom(Func<OnlineGameMode, RoomSession, AbstractPhysicalObject, bool> orig, OnlineGameMode self, RoomSession rs, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            BTWPlugin.Log(apo.ToString() + " shall not be sync (room) !");
            return false; 
        }
        return orig(self, rs, apo);
    }
    private static void WorldSession_DoNotRegisterExit(Action<WorldSession, AbstractPhysicalObject> orig, WorldSession self, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            BTWPlugin.Log(apo.ToString() + " shall not be accounted in deletion (world) !");
            return; 
        }
        orig(self, apo);
    }
    private static void RoomSession_DoNotRegisterExit(Action<RoomSession, AbstractPhysicalObject> orig, RoomSession self, AbstractPhysicalObject apo)
    {
        if (deniedSyncedObjects.Contains(apo.type)) { 
            BTWPlugin.Log(apo.ToString() + " shall not be accounted in deletion (room) !");
            return; 
        }
        orig(self, apo);
    }
}