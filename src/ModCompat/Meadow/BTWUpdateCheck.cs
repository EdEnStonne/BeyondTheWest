using System;
using RainMeadow;
using UnityEngine;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using BeyondTheWest.MSCCompat;
using Menu;
using RainMeadow.UI.Pages;
using BeyondTheWest.Items;
using static RainMeadow.Serializer;
using System.Linq;

namespace BeyondTheWest.MeadowCompat;

public static class BTWVersionChecker
{
    public static bool BTWVersionChecked = false;
    public static string errorMessageText = null;
    public static bool leaveOnError = true;
    public static LobbyBTWVersionData lobbyBTWVersionData;
    public static LobbyBTWVersionData myBTWVersionData;
    public static void ApplyHooks()
    {
        MatchmakingManager.OnLobbyJoined += MatchmakingManager_BTWVersionChecker_OnLobbyJoined;
        new Hook(typeof(ArenaMainLobbyPage).GetMethod(nameof(ArenaMainLobbyPage.GrafUpdate)), ArenaMainMenu_ShowError);
        new Hook(typeof(StoryOnlineMenu).GetMethod(nameof(StoryOnlineMenu.Update)), StoryOnlineMenu_ShowError);
    }
    private static void ArenaMainMenu_ShowError(Action<ArenaMainLobbyPage, float> orig, ArenaMainLobbyPage self, float timeStacker)
    {
        if (OnlineManager.lobby != null && errorMessageText is not null)
        {
            ShowVersionErrorMessage();
        }
        orig(self, timeStacker);
    }
    private static void StoryOnlineMenu_ShowError(Action<StoryOnlineMenu> orig, StoryOnlineMenu self)
    {
        if (OnlineManager.lobby != null && errorMessageText is not null)
        {
            ShowVersionErrorMessage();
        }
        orig(self);
    }
    public static void ShowVersionErrorMessage()
    {
        DialogNotify errorMessage = new(errorMessageText, new Vector2(480f, 320f), RWCustom.Custom.rainWorld.processManager, Cancel);
        errorMessageText = null;
        errorMessage.okButton.menuLabel.myText = leaveOnError ? "Leave lobby" : "Ok";
        RWCustom.Custom.rainWorld.processManager.ShowDialog(errorMessage);
    }
    private static void Cancel() 
    { 
        (RWCustom.Custom.rainWorld.processManager.currentMainLoop as Menu.Menu)?.PlaySound(SoundID.MENU_Switch_Page_Out); 
        if (leaveOnError)
        {
            OnlineManager.LeaveLobby();
            RWCustom.Custom.rainWorld.processManager.RequestMainProcessSwitch(RainMeadow.RainMeadow.Ext_ProcessID.LobbySelectMenu);
        }
    }
    public static void CompareVersion(LobbyBTWVersionData hostBTWVersionData)
    {
        BTWPlugin.Log($"Owner responded ! Here is their lobby version : ");
        hostBTWVersionData.Log();
        lobbyBTWVersionData = hostBTWVersionData;

        try
        {
            for (int i = 0; i < hostBTWVersionData.BTWVersion.Length; i++)
            {
                if (hostBTWVersionData.BTWVersion[i] != myBTWVersionData.BTWVersion[i])
                {
                    BTWPlugin.logger.LogWarning($"version of BTW doesn't match ! <{myBTWVersionData.BTWVersionString}> instead of <{hostBTWVersionData.BTWVersionString}>");
                    errorMessageText = "Version mismatch for Beyond the West !" 
                    + Environment.NewLine + $"Your version is {myBTWVersionData.BTWVersionString} while the host is {hostBTWVersionData.BTWVersionString}"
                    + Environment.NewLine + "To avoid desync issues, you are being send back to the main menu.";
                    leaveOnError = true;
                    return;
                }
            }

            if (!hostBTWVersionData.IsEnumMatching())
            {
                hostBTWVersionData.ReorganizeEnum();
                myBTWVersionData.SaveCurrentEnumOrder();
                myBTWVersionData.Log();
                errorMessageText = "Some enum have been found misplaced (Oh no !)" 
                    + Environment.NewLine + "That means that you might not interpret data sends by others correctly."
                    + Environment.NewLine + "To avoid desync issues, your enum order has been modified to the host's (WIP)."
                    + Environment.NewLine + "You should be safe from enum desync now (hooray!)."
                    + Environment.NewLine + "(Please restart your game if you encounter any issues)";
                leaveOnError = false;
                return;
            }
            BTWPlugin.Log($"Version verified ! Everything matches !");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError("Error while checking version of lobby ! " + ex);
            errorMessageText = "Beyond the West failed to check the version of the host";
            leaveOnError = false;
        }
    }
    private static void MatchmakingManager_BTWVersionChecker_OnLobbyJoined(bool ok, string error)
    {
        try
        {
            if (ok)
            {
                BTWPlugin.Log($"Is there a lobby there ? [{OnlineManager.lobby != null}], [{OnlineManager.lobby?.owner}], [{OnlineManager.lobby?.isOwner}], [{OnlineManager.lobby?.configurableInts.Count}]");
                if (OnlineManager.lobby != null)
                {
                    myBTWVersionData = new();
                    errorMessageText = null;
                    if (OnlineManager.lobby.isOwner)
                    {
                        lobbyBTWVersionData = myBTWVersionData;
                        BTWPlugin.Log($"I'm the owner ! Here's the lobby version : ");
                        lobbyBTWVersionData.Log();
                    }
                    else
                    {
                        // myBTWVersionData.Swap2EnumForTestingPurposes(50);
                        BTWPlugin.Log($"Lobby BTW version has to be checked ! Current version is :");
                        myBTWVersionData.Log();
                        OnlineManager.lobby.owner.InvokeRPC(MeadowRPCs.BTWVersionChecker_RequestVersionInfo);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError("Error while creating version struct of lobby ! " + ex);
        }
    }

    public class LobbyBTWVersionData : ICustomSerializable
    {
        public LobbyBTWVersionData()
        {
            this.BTWVersion = BTWPlugin.GetVersionIntArray();
            this.RainWorldEnums = new string[SyncedEnumTypes.Count][];
            SaveCurrentEnumOrder();
        }
        public void SaveCurrentEnumOrder()
        {
            for (int i = 0; i < this.RainWorldEnums.Length; i++)
            {
                this.RainWorldEnums[i] = GetArrayIndexOfEnumList(SyncedEnumTypes[i]);
            }
        }
        private string[] GetArrayIndexOfEnumList(Type EnumType)
        {
            if (ExtEnumBase.GetExtEnumType(EnumType) is ExtEnumType enumType)
            {
                return enumType.entries.ToArray();
            }
            throw new ArgumentException($"enumType [{EnumType}] is not an ExtEnum type.");
        }

        public void Log()
        {
            BTWPlugin.Log($"Logging BTW lobby data :");
            BTWPlugin.Log($"    > Version of BTW is {BTWVersionString}");
            for (int i = 0; i < this.RainWorldEnums.Length; i++)
            {
                BTWPlugin.Log($"    > Order of [{SyncedEnumTypes[i].Name}] enum is :");
                for (int j = 0; j < this.RainWorldEnums[i].Length; j++)
                {
                    BTWPlugin.Log($"        >> <{j}>[{this.RainWorldEnums[i][j]}]");
                }
            }
        }
        public void ReorganizeEnum()
        {
            BTWPlugin.Log($"Changing the order of the enum according to the lobby data...");
            try
            {
                for (int en = 0; en < this.RainWorldEnums.Length; en++)
                {
                    if (ExtEnumBase.GetExtEnumType(SyncedEnumTypes[en]) is ExtEnumType enumType)
                    {
                        List<string> newEnumOrder = this.RainWorldEnums[en].ToList();
                        for (int i = 0; i < enumType.entries.Count; i++)
                        {
                            if (this.RainWorldEnums[en].FirstOrDefault(x => x == enumType.entries[i]) == null)
                            {
                                newEnumOrder.Add(enumType.entries[i]);
                            }
                        }
                        enumType.entries = newEnumOrder;
                        enumType.version++;
                    }
                    else
                    {
                        throw new ArgumentException($"enumType [{SyncedEnumTypes[en]}] is not an ExtEnum type.");
                    }
                }
            }
            catch (Exception ex)
            {
                BTWPlugin.logger.LogError("Error while changing the order of the enums ! " + ex);
            }
            BTWPlugin.Log($"Done without issues ! For now...");
        }
        public bool IsEnumMatching()
        {
            BTWPlugin.Log($"Checking if enum matches");
            bool match = true;
            try
            {
                for (int en = 0; en < this.RainWorldEnums.Length; en++)
                {
                    if (ExtEnumBase.GetExtEnumType(SyncedEnumTypes[en]) is ExtEnumType enumType)
                    {
                        if (this.RainWorldEnums[en].Length != enumType.entries.Count)
                        {
                            BTWPlugin.logger.LogWarning($"Lenght of enum [{SyncedEnumTypes[en].Name}] doesn't match ! [{enumType.entries.Count}] instead of [{this.RainWorldEnums[en].Length}]");
                            return false;
                        }
                        for (int i = 0; i < enumType.entries.Count; i++)
                        {
                            if (this.RainWorldEnums[en][i] != enumType.entries[i])
                            {
                                BTWPlugin.logger.LogWarning($"id <{i}> of enum [{SyncedEnumTypes[en].Name}] doesn't match ! [{enumType.entries[i]}] instead of [{this.RainWorldEnums[en][i]}]");
                                match = false;
                            }
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"enumType [{SyncedEnumTypes[en]}] is not an ExtEnum type.");
                    }
                }
            }
            catch (Exception ex)
            {
                BTWPlugin.logger.LogError("Error while changing the order of the enums ! " + ex);
            }
            return match;
        }
        internal void Swap2EnumForTestingPurposes(int swaps = 1)
        {
            BTWPlugin.Log($"Swapping two enum order (x{swaps}) for testing purposes...");
            for (int s = 1; s <= swaps; s++)
            {
                try
                {
                    int en = BTWFunc.RandInt(this.RainWorldEnums.Length - 1);
                    int i = BTWFunc.RandInt(this.RainWorldEnums[en].Length - 1);
                    int j = BTWFunc.RandInt(this.RainWorldEnums[en].Length - 1);
                    if (ExtEnumBase.GetExtEnumType(SyncedEnumTypes[en]) is ExtEnumType enumType)
                    {
                        (enumType.entries[j], enumType.entries[i]) = (enumType.entries[i], enumType.entries[j]);
                        (this.RainWorldEnums[en][j], this.RainWorldEnums[en][i]) = (this.RainWorldEnums[en][i], this.RainWorldEnums[en][j]);
                        enumType.version++;
                        BTWPlugin.Log($"Swapped <{i}>[{enumType.entries[j]}] and <{j}>[{enumType.entries[i]}] of enum [{SyncedEnumTypes[en].Name}]");
                    }
                    else
                    {
                        throw new ArgumentException($"enumType [{SyncedEnumTypes[en]}] is not an ExtEnum type.");
                    }
                }
                catch (Exception ex)
                {
                    BTWPlugin.logger.LogError("Error while changing the order of the enums ! " + ex);
                }
            }
            BTWPlugin.Log($"Done without issues ! For now...");
        }
        public int[] BTWVersion;
        public string[][] RainWorldEnums;
        public static List<Type> SyncedEnumTypes = new()
        {
            typeof(SlugcatStats.Name),
            typeof(AbstractPhysicalObject.AbstractObjectType),
            typeof(CreatureTemplate.Type),
            typeof(OnlineState.StateType)
        };

        public string BTWVersionString => string.Join(".", BTWVersion);

        public void CustomSerialize(Serializer serializer)
        {
            if (serializer.IsWriting)
            {  
                serializer.Serialize(ref this.BTWVersion);
                for (int i = 0; i < RainWorldEnums.Length; i++)
                {
                    serializer.Serialize(ref this.RainWorldEnums[i]);
                }
            }
            else if (serializer.IsReading)
            {
                serializer.Serialize(ref this.BTWVersion);
                for (int i = 0; i < RainWorldEnums.Length; i++)
                {
                    serializer.Serialize(ref this.RainWorldEnums[i]);
                }
            }
        }
    }
}