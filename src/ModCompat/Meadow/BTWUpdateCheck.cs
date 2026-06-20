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
        }

        public void Log()
        {
            BTWPlugin.Log($"Logging BTW lobby data :");
            BTWPlugin.Log($"    > Version of BTW is {BTWVersionString}");
        }
        public int[] BTWVersion;

        public string BTWVersionString => string.Join(".", BTWVersion);

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref this.BTWVersion);
        }
    }
}