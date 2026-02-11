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
    public static void CompareVersion(ref LobbyBTWVersionData hostBTWVersionData)
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

            int[] enumMisplaced = new int[3]{0, 0, 0};
            for (int i = 0; i < hostBTWVersionData.SlugcatNameEnum.Length; i++)
            {
                if (hostBTWVersionData.SlugcatNameEnum[i] != myBTWVersionData.SlugcatNameEnum[i])
                {
                    BTWPlugin.logger.LogWarning($"id <{i}> of slugcat names doesn't match ! [{myBTWVersionData.SlugcatNameEnum[i]}] instead of [{hostBTWVersionData.SlugcatNameEnum[i]}]");
                    enumMisplaced[0]++;
                }
            }
            for (int i = 0; i < hostBTWVersionData.ItemsTypeEnum.Length; i++)
            {
                if (hostBTWVersionData.ItemsTypeEnum[i] != myBTWVersionData.ItemsTypeEnum[i])
                {
                    BTWPlugin.logger.LogWarning($"id <{i}> of object types doesn't match ! [{myBTWVersionData.ItemsTypeEnum[i]}] instead of [{hostBTWVersionData.ItemsTypeEnum[i]}]");
                    enumMisplaced[1]++;
                }
            }
            for (int i = 0; i < hostBTWVersionData.CreatureTypeEnum.Length; i++)
            {
                if (hostBTWVersionData.CreatureTypeEnum[i] != myBTWVersionData.CreatureTypeEnum[i])
                {
                    BTWPlugin.logger.LogWarning($"id <{i}> of creature types doesn't match ! [{myBTWVersionData.CreatureTypeEnum[i]}] instead of [{hostBTWVersionData.CreatureTypeEnum[i]}]");
                    enumMisplaced[2]++;
                }
            }

            if (!(enumMisplaced[0] == 0 && enumMisplaced[1] == 0 && enumMisplaced[2] == 0))
            {
                hostBTWVersionData.ReorganizeEnum();
                errorMessageText = "Some enum have ben found misplaced !" 
                    + Environment.NewLine + $"Slugcat enum misplaced : {enumMisplaced[0]}"
                    + Environment.NewLine + $"Items enum misplaced : {enumMisplaced[1]}"
                    + Environment.NewLine + $"Creature enum misplaced : {enumMisplaced[2]}"
                    + Environment.NewLine + "To avoid desync issues, your enum order has been modified to the host's (WIP)."
                    + Environment.NewLine + "Please restart your game if you encounter any issues.";
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

    public struct LobbyBTWVersionData : ICustomSerializable
    {
        public LobbyBTWVersionData()
        {
            this.BTWVersion = BTWPlugin.GetVersionIntArray();
            this.SlugcatNameEnum = SlugcatStats.Name.values.entries.ToArray();
            this.ItemsTypeEnum = AbstractPhysicalObject.AbstractObjectType.values.entries.ToArray();
            this.CreatureTypeEnum = CreatureTemplate.Type.values.entries.ToArray();
        }
        public readonly void Log()
        {
            BTWPlugin.Log($"Logging BTW lobby data :");
            BTWPlugin.Log($"    > Version of BTW is {BTWVersionString}");
            BTWPlugin.Log($"    > Order of SlugcatStats.Name enum is :");
            for (int i = 0; i < this.SlugcatNameEnum.Length; i++)
            {
                BTWPlugin.Log($"        >> <{i}>[{this.SlugcatNameEnum[i]}]");
            }
            BTWPlugin.Log($"    > Order of AbstractPhysicalObject.AbstractObjectType enum is :");
            for (int i = 0; i < this.ItemsTypeEnum.Length; i++)
            {
                BTWPlugin.Log($"        >> <{i}>[{this.ItemsTypeEnum[i]}]");
            }
            BTWPlugin.Log($"    > Order of CreatureTemplate.Type enum is :");
            for (int i = 0; i < this.CreatureTypeEnum.Length; i++)
            {
                BTWPlugin.Log($"        >> <{i}>[{this.CreatureTypeEnum[i]}]");
            }
        }
        public readonly void ReorganizeEnum()
        {
            BTWPlugin.Log($"Changing the order of the enum according to the lobby data...");
            try
            {
                List<string> names = this.SlugcatNameEnum.ToList();
                for (int i = 0; i < SlugcatStats.Name.values.entries.Count; i++)
                {
                    if (this.SlugcatNameEnum.FirstOrDefault(x => x == SlugcatStats.Name.values.entries[i]) == null)
                    {
                        names.Add(SlugcatStats.Name.values.entries[i]);
                    }
                }
                SlugcatStats.Name.values.entries = names;

                List<string> items = this.ItemsTypeEnum.ToList();
                for (int i = 0; i < AbstractPhysicalObject.AbstractObjectType.values.entries.Count; i++)
                {
                    if (this.ItemsTypeEnum.FirstOrDefault(x => x == AbstractPhysicalObject.AbstractObjectType.values.entries[i]) == null)
                    {
                        items.Add(AbstractPhysicalObject.AbstractObjectType.values.entries[i]);
                    }
                }
                AbstractPhysicalObject.AbstractObjectType.values.entries = items;

                List<string> creatures = this.CreatureTypeEnum.ToList();
                for (int i = 0; i < CreatureTemplate.Type.values.entries.Count; i++)
                {
                    if (this.CreatureTypeEnum.FirstOrDefault(x => x == CreatureTemplate.Type.values.entries[i]) == null)
                    {
                        creatures.Add(CreatureTemplate.Type.values.entries[i]);
                    }
                }
                CreatureTemplate.Type.values.entries = creatures;
            }
            catch (Exception ex)
            {
                BTWPlugin.logger.LogError("Error while changing the order of the enums ! " + ex);
            }
            BTWPlugin.Log($"Done without issues ! For now...");
        }
        public int[] BTWVersion;
        public string[] SlugcatNameEnum;
        public string[] ItemsTypeEnum;
        public string[] CreatureTypeEnum;

        public readonly string BTWVersionString => string.Join(".", BTWVersion);

        public void CustomSerialize(Serializer serializer)
        {
            if (serializer.IsWriting)
            {  
                serializer.Serialize(ref this.BTWVersion);
                serializer.Serialize(ref this.SlugcatNameEnum);
                serializer.Serialize(ref this.ItemsTypeEnum);
                serializer.Serialize(ref this.CreatureTypeEnum);
            }
            else if (serializer.IsReading)
            {
                serializer.Serialize(ref this.BTWVersion);
                serializer.Serialize(ref this.SlugcatNameEnum);
                serializer.Serialize(ref this.ItemsTypeEnum);
                serializer.Serialize(ref this.CreatureTypeEnum);
            }
        }
    }
}