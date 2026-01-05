using System;
using RainMeadow;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using BeyondTheWest.MSCCompat;
using Menu;

namespace BeyondTheWest.MeadowCompat;

public static class BTWVersionChecker
{
    public static bool BTWVersionChecked = false;
    // public static bool ShowVersionError = false;
    public static void ApplyHooks()
    {
        MatchmakingManager.OnLobbyJoined += MatchmakingManager_BTWVersionChecker_OnLobbyJoined;
        new Hook(typeof(OnlineResource).GetMethod(nameof(OnlineResource.UpdateParticipants)), Lobby_CheckBTWVersion);
        new Hook(typeof(LobbySelectMenu).GetMethod(nameof(LobbySelectMenu.Update)), LobbyMenu_ShowErrorMessage);
    }
    private static void LeaveLobbyBecauseUnmatchingVersions(string hostVersion)
    {
        if (OnlineManager.lobby != null)
        {
            BTWMenu.BTWUpdateDialog.hostVersion = hostVersion;
            // There lies a bad idea :
            // RPCEvent rpc = OnlineManager.lobby.owner.InvokeRPC(
            //     typeof(MeadowRPCs).GetMethod(nameof(MeadowRPCs.BTWVersionChecker_VersionMismatch)).CreateDelegate(
            //         typeof(Action<RPCEvent, string>)), 
            //     hostVersion);
            // rpc.Then(Leave);
            Leave();
        }
    }
    private static void Leave(GenericResult result = null)
    {
        // Taken from the ban hammer
        OnlineManager.LeaveLobby();
        if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is not null)
        {
            RWCustom.Custom.rainWorld.processManager.musicPlayer?.DeathEvent();
            game.ExitGame(asDeath: true, asQuit: true);
        }
        RWCustom.Custom.rainWorld.processManager.RequestMainProcessSwitch(RainMeadow.RainMeadow.Ext_ProcessID.LobbySelectMenu);
        RWCustom.Custom.rainWorld.processManager.ShowDialog(
            new DialogNotify(BTWMenu.BTWUpdateDialog.Text, new Vector2(480f, 320f), RWCustom.Custom.rainWorld.processManager, Cancel));
    }
    private static void Cancel() 
    { 
        (RWCustom.Custom.rainWorld.processManager.currentMainLoop as Menu.Menu)?.PlaySound(SoundID.MENU_Switch_Page_Out); 
    }
    private static void LobbyMenu_ShowErrorMessage(Action<LobbySelectMenu> orig, LobbySelectMenu self)
    {
        orig(self);
        // if (ShowVersionError)
        // {
        //     self.ShowErrorDialog(Menu.BTWUpdateDialog.Text);
        //     ShowVersionError = false;
        // }
    }
    private static void Lobby_CheckBTWVersion(Action<OnlineResource, List<OnlinePlayer>> orig, OnlineResource self, List<OnlinePlayer> newParticipants)
    {
        orig(self, newParticipants);
        if (self is Lobby lobby)
        {
            if (!lobby.isOwner && !BTWVersionChecked && lobby.modsChecked)
            {
                int[] currentVersionInt = BTWFunc.GetVersionIntArray();
                int[] hostVersionInt = new int[3];
                bool match = true;
                BTWPlugin.Log("Trying to check lobby BTW version !");
                if (OnlineManager.lobby.configurableInts.TryGetValue("BTWVersionPROUD", out int v0))
                {
                    hostVersionInt[0] = v0;
                }
                else
                {
                    match = false;
                }
                if (OnlineManager.lobby.configurableInts.TryGetValue("BTWVersionBIG", out int v1))
                {
                    hostVersionInt[1] = v1;
                }
                else
                {
                    match = false;
                }
                if (OnlineManager.lobby.configurableInts.TryGetValue("BTWVersionPATCH", out int v2))
                {
                    hostVersionInt[2] = v2;
                }
                else
                {
                    match = false;
                }
                if (!match)
                {
                    BTWPlugin.Log("Host doesn't have a version to match ! Exiting the lobby immiediatly.");
                    LeaveLobbyBecauseUnmatchingVersions("[Not found]");
                }
                else
                {
                    BTWPlugin.Log($"Host Version is {hostVersionInt[0]}.{hostVersionInt[1]}.{hostVersionInt[2]}, current is {currentVersionInt[0]}.{currentVersionInt[1]}.{currentVersionInt[2]}");
                    string hostVersion = "";
                    for (int i = 0; i < 3; i++)
                    {
                        if (hostVersionInt[i] != currentVersionInt[i])
                        {
                            match = false;
                        }
                        hostVersion += hostVersionInt[i];
                        if (i < 2)
                        {
                            hostVersion += ".";
                        }
                    }
                    if (!match)
                    {
                        BTWPlugin.Log("Host doesn't have the same version ! Exiting the lobby immiediatly.");
                        LeaveLobbyBecauseUnmatchingVersions(hostVersion);
                    }
                }
                BTWVersionChecked = true;
            }
        }
    }
    private static void MatchmakingManager_BTWVersionChecker_OnLobbyJoined(bool ok, string error)
    {
        if (ok)
        {
            BTWPlugin.Log($"Is there a lobby there ? [{OnlineManager.lobby != null}], [{OnlineManager.lobby?.owner}], [{OnlineManager.lobby?.isOwner}], [{OnlineManager.lobby?.configurableInts.Count}]");
            if (OnlineManager.lobby != null)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    int[] versionInt = BTWFunc.GetVersionIntArray();
                    OnlineManager.lobby.configurableInts.Add("BTWVersionPROUD", versionInt[0]);
                    OnlineManager.lobby.configurableInts.Add("BTWVersionBIG", versionInt[1]);
                    OnlineManager.lobby.configurableInts.Add("BTWVersionPATCH", versionInt[2]);
                    BTWVersionChecked = true;
                    BTWPlugin.Log($"Lobby BTW version set to : {versionInt[0]}.{versionInt[1]}.{versionInt[2]}");
                }
                else
                {
                    BTWVersionChecked = false;
                    int[] versionInt = BTWFunc.GetVersionIntArray();
                    BTWPlugin.Log($"Lobby BTW version has to be checked ! Current version is : {versionInt[0]}.{versionInt[1]}.{versionInt[2]}");
                }
            }
        }
    }
}