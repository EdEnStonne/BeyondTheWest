using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;
using ObjectType = AbstractPhysicalObject.AbstractObjectType;

namespace BeyondTheWest.Items;

public struct IconLayerCount
{
    public string name;
    public int count;
    public int fragment;
}

public static class NewObjectsHooks
{
    public static IconSymbol.IconSymbolData PoisonSpearIconData;
    public static string PoisonSpearIconName = "SpearPoisonIcon";
    public static Color PoisonSpearIconColor = new Color(0.35f, 0.15f, 0.85f);
    public static void LoadIcons()
    {
        TristorHooks.Register();
        VoidCrystalHooks.Register();
        CrystalSpearHooks.Register();
        Futile.atlasManager.ActuallyLoadAtlasOrImage("SpearPoisonIcon", "icons/icon_SpearPoison", "");
        PoisonSpearIconData = new(CreatureTemplate.Type.StandardGroundCreature, ObjectType.Spear, 4);
        BTWPlugin.Log("NewObjectsHooks LoadIcons Done !");
    }
    
    public static void ApplyHooks()
    {
        TristorHooks.ApplyHooks();
        VoidCrystalHooks.ApplyHooks();
        CrystalSpearHooks.ApplyHooks();

        // On.Menu.SandboxEditorSelector.ctor += SandboxEditorSelector_LogAllUnlocks;

        On.MultiplayerUnlocks.SymbolDataForSandboxUnlock += MultiplayerUnlocks_PutNewUnlockData;
        On.MultiplayerUnlocks.SandboxItemUnlocked += MultiplayerUnlocks_SetLockOnNewItems;
        On.ItemSymbol.ColorForItem += ItemSymbol_GetItemIconColor;
        On.ItemSymbol.SpriteNameForItem += ItemSymbol_GetItemIconName;
        On.ItemSymbol.SymbolDataFromItem += ItemSymbol_GetItemData;
        On.SandboxGameSession.SpawnItems += SandboxGameSession_SpawnCustomItem;
        On.SaveState.AbstractPhysicalObjectFromString += SaveState_GetObjectFromSave;

        On.Player.CanBeSwallowed += Player_CanSwallowItem;
        On.Player.Grabability += Player_CanGrabItem;

        On.ScavengerAI.WeaponScore += ScavengerAI_ScoreWeaponOfCustomItems;
        IL.ScavengerAI.CollectScore_PhysicalObject_bool += ScavengerAI_ScoreOfCustomItems;
        IL.ScavengerAI.SeeThrownWeapon += ScavengerAI_GetScaredOfWeapon;

        BTWPlugin.Log("NewObjectsHooks ApplyHooks Done !");
    }

    public static bool ShouldBeScared(PhysicalObject obj)
    {
        if (obj is Tristor
            || obj is VoidCrystal)
        {
            return true;
        }
        return false;
    }
    private static void ScavengerAI_GetScaredOfWeapon(ILContext il)
    {
        BTWPlugin.Log("NewObjectsHooks IL 2 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            ILLabel label = cursor.DefineLabel();
            if (cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(1),
                x => x.MatchIsinst<Spear>(),
                x => x.MatchBrtrue(out label),
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MMF)),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(1),
                x => x.MatchIsinst<ScavengerBomb>(),
                x => x.MatchBrtrue(out _),
                x => x.MatchCall(typeof(ModManager).GetProperty(nameof(ModManager.DLCShared)).GetGetMethod()),
                x => x.MatchBrfalse(out _)
            ))
            {
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(ShouldBeScared);
                cursor.Emit(OpCodes.Brtrue, label);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("NewObjectsHooks IL 2 ends");
    }

    public static bool DoesGiveCustomScore(PhysicalObject obj)
    {
        return obj is Tristor
            || obj is VoidCrystal;
    }
    public static int CustomScore(PhysicalObject obj)
    {
        if (obj is Tristor)
        {
            return 4;
        }
        if (obj is VoidCrystal)
        {
            return 1;
        }
        return 0;
    }
    private static void ScavengerAI_ScoreOfCustomItems(ILContext il)
    {
        BTWPlugin.Log("NewObjectsHooks IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(1),
                x => x.MatchLdcI4(0),
                x => x.MatchCall<ScavengerAI>(nameof(ScavengerAI.WeaponScore)),
                x => x.MatchRet()
            ))
            {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(DoesGiveCustomScore);
                cursor.Emit(OpCodes.Brfalse, cursor.Next);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(CustomScore);
                cursor.Emit(OpCodes.Ret);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("NewObjectsHooks IL 1 ends");
    }

    private static int ScavengerAI_ScoreWeaponOfCustomItems(On.ScavengerAI.orig_WeaponScore orig, ScavengerAI self, PhysicalObject obj, bool pickupDropInsteadOfWeaponSelection, bool reallyWantsSpear)
    {
        if (obj is Tristor) // taken from Scav Bomb score
        {
            if (pickupDropInsteadOfWeaponSelection)
            {
                return 4;
            }
            if (self.currentViolenceType != ScavengerAI.ViolenceType.Lethal)
            {
                return 0;
            }
            if (self.focusCreature != null 
                && !Custom.DistLess(self.scavenger.mainBodyChunk.pos, self.scavenger.room.MiddleOfTile(self.focusCreature.BestGuessForPosition()), 300f))
            {
                for (int j = 0; j < self.tracker.CreaturesCount; j++)
                {
                    if (self.tracker.GetRep(j).dynamicRelationship.currentRelationship.type == CreatureTemplate.Relationship.Type.Pack 
                        && (float)Custom.ManhattanDistance(self.tracker.GetRep(j).BestGuessForPosition(), self.focusCreature.BestGuessForPosition()) < 7f)
                    {
                        return 0;
                    }
                }
                return 4;
            }
            if (self.scared <= 0.9f)
            {
                return 0;
            }
            return 1;
        }
        else if (obj is Tristor) // taken from Scav Bomb score
        {
            if (self.scared <= 0.9f)
            {
                return 0;
            }
            return 1;
        }
        return orig(self, obj, pickupDropInsteadOfWeaponSelection, reallyWantsSpear);
    }

    private static Player.ObjectGrabability Player_CanGrabItem(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (obj is Tristor
            || obj is VoidCrystal)
        {
            return Player.ObjectGrabability.OneHand;
        }
        return orig(self, obj);
    }
    private static bool Player_CanSwallowItem(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        if (testObj is Tristor)
        {
            return false;
        }
        else if (testObj is VoidCrystal)
        {
            return true;
        }
        return orig(self, testObj);
    }

    private static AbstractPhysicalObject SaveState_GetObjectFromSave(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
    {
        try
        {
            string[] data = objString.Split(new[] { "<oA>" }, StringSplitOptions.None);
            string type = data[1];
            if (SaveHelper.Supported(type))
            {
                BTWPlugin.Log($"Found custom object to load : {type}");
                AbstractPhysicalObject result = SaveHelper.GetCustomObject(world, objString);
                if (result is not null)
                {
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError("Error while trying to get type of object : "+ ex);
        }
        return orig(world, objString);
    }

    private static void SandboxGameSession_SpawnCustomItem(On.SandboxGameSession.orig_SpawnItems orig, SandboxGameSession self, IconSymbol.IconSymbolData data, WorldCoordinate pos, EntityID entityID)
    {
        if (data == AbstractTristor.TristorIconData)
        {
            self.game.world.GetAbstractRoom(0).AddEntity(new AbstractTristor(self.game.world, null, pos, entityID));
            return;
        }
        if (data == AbstractVoidCrystal.VoidCrystalIconData)
        {
            self.game.world.GetAbstractRoom(0).AddEntity(new AbstractVoidCrystal(self.game.world, null, pos, entityID));
            return;
        }
        if (data == AbstractCrystalSpear.CrystalSpearIconData)
        {
            self.game.world.GetAbstractRoom(0).AddEntity(new AbstractCrystalSpear(self.game.world, null, pos, entityID));
            return;
        }
        orig(self, data, pos, entityID);
    }
    private static IconSymbol.IconSymbolData? ItemSymbol_GetItemData(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
    {
        if (item is AbstractTristor)
        {
            return AbstractTristor.TristorIconData;
        }
        if (item is AbstractVoidCrystal)
        {
            return AbstractVoidCrystal.VoidCrystalIconData;
        }
        if (ModManager.Watcher && item is AbstractSpear spear && spear.poison > 0f && !spear.explosive)
        {
            return PoisonSpearIconData;
        }
        if (item is AbstractCrystalSpear)
        {
            return AbstractCrystalSpear.CrystalSpearIconData;
        }
        return orig(item);
    }
    private static string ItemSymbol_GetItemIconName(On.ItemSymbol.orig_SpriteNameForItem orig, ObjectType itemType, int intData)
    {
        if (itemType == AbstractTristor.TristorType)
        {
            return AbstractTristor.TristorIconName;
        }
        if (itemType == AbstractVoidCrystal.VoidCrystalType)
        {
            return AbstractVoidCrystal.VoidCrystalIconName;
        }
        if (ModManager.Watcher && itemType == ObjectType.Spear && intData == 4)
        {
            return PoisonSpearIconName;
        }
        if (itemType == ObjectType.Spear && intData == 5)
        {
            return AbstractCrystalSpear.CrystalSpearIconName;
        }
        return orig(itemType, intData);
    }
    private static Color ItemSymbol_GetItemIconColor(On.ItemSymbol.orig_ColorForItem orig, ObjectType itemType, int intData)
    {
        if (itemType == AbstractTristor.TristorType)
        {
            return AbstractTristor.TristorIconColor;
        }
        if (itemType == AbstractVoidCrystal.VoidCrystalType)
        {
            return AbstractVoidCrystal.VoidCrystalIconColor;
        }
        if (ModManager.Watcher && itemType == ObjectType.Spear && intData == 4)
        {
            return PoisonSpearIconColor;
        }
        if (itemType == ObjectType.Spear && intData == 5)
        {
            return AbstractCrystalSpear.CrystalSpearIconColor;
        }
        return orig(itemType, intData);
    }

    private static bool MultiplayerUnlocks_SetLockOnNewItems(On.MultiplayerUnlocks.orig_SandboxItemUnlocked orig, MultiplayerUnlocks self, MultiplayerUnlocks.SandboxUnlockID unlockID)
    {
        if (unlockID == AbstractTristor.TristorUnlock)
        {
            return true;
        }
        if (unlockID == AbstractVoidCrystal.VoidCrystalUnlock)
        {
            return true;
        }
        if (unlockID == AbstractCrystalSpear.CrystalSpearUnlock)
        {
            return true;
        }
        return orig(self, unlockID);
    }
    private static IconSymbol.IconSymbolData MultiplayerUnlocks_PutNewUnlockData(On.MultiplayerUnlocks.orig_SymbolDataForSandboxUnlock orig, MultiplayerUnlocks.SandboxUnlockID unlockID)
    {
        if (unlockID == AbstractTristor.TristorUnlock)
        {
            return AbstractTristor.TristorIconData;
        }
        if (unlockID == AbstractVoidCrystal.VoidCrystalUnlock)
        {
            return AbstractVoidCrystal.VoidCrystalIconData;
        }
        if (unlockID == AbstractCrystalSpear.CrystalSpearUnlock)
        {
            return AbstractCrystalSpear.CrystalSpearIconData;
        }
        return orig(unlockID);
    }

    private static void SandboxEditorSelector_LogAllUnlocks(On.Menu.SandboxEditorSelector.orig_ctor orig, Menu.SandboxEditorSelector self, Menu.Menu menu, Menu.MenuObject owner, SandboxOverlayOwner overlayOwner)
    {
        orig(self, menu, owner, overlayOwner);
        
        BTWPlugin.Log("Logging all Unlocks :");
        foreach (MultiplayerUnlocks.SandboxUnlockID unlockID in MultiplayerUnlocks.ItemUnlockList)
        {
            BTWPlugin.Log($"    > <{unlockID.index}>[{unlockID}] : {self.unlocks.SandboxItemUnlocked(unlockID)}");
        }
    }

}