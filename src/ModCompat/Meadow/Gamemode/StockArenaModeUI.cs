using Menu;
using Menu.Remix;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using RainMeadow.UI;
using RainMeadow.UI.Pages;
using System.Runtime.CompilerServices;
using RainMeadow;
using HUD;
using RWCustom;


namespace BeyondTheWest.MeadowCompat.Gamemodes;
public partial class StockArenaMode : ExternalArenaGameMode
{
    public TabContainer.Tab myTab;
    public OnlineStockArenaModeSettingsInterface myStockSettingInterface;

    private int _livesDefaultAmount = BTWRemix.MeadowArenaLivesAmount.Value;
    public int LivesDefaultAmount
    {
        get
        {
            return _livesDefaultAmount;
        }
        set
        {
            if (_livesDefaultAmount != value)
            {
                _livesDefaultAmount = value;
                ArenaStockClientSettings stockSettings = ArenaHelpers.GetDataSettings<ArenaStockClientSettings>(OnlineManager.mePlayer);
                if (stockSettings != null)
                {
                    stockSettings.lives = value;
                }
            }
        }
    }
    public bool everyoneCanModifyTheirLifeAmount = BTWRemix.MeadowArenaLivesEveryoneCanSet.Value;
    public bool blockWin = BTWRemix.MeadowArenaLivesBlockWin.Value;
    public bool strictEnforceAfter0Lives = BTWRemix.MeadowArenaLivesStrict.Value;
    public int reviveTime = BTWRemix.MeadowArenaLivesReviveTime.Value;
    public int additionalReviveTime = BTWRemix.MeadowArenaLivesAdditionalReviveTime.Value;
    public bool respawnShieldToggle = BTWRemix.MeadowArenaLivesRespawnShield.Value;
    public int respawnShieldDuration = BTWRemix.MeadowArenaLivesRespawnShieldDuration.Value;
    public bool karmaFlowerGiveProtection = BTWRemix.MeadowArenaLivesKarmaFlowerProtection.Value;
    public bool karmaFlowerGiveLife = BTWRemix.MeadowArenaLivesKarmaFlower1UP.Value;
    public bool killGiveProtection = BTWRemix.MeadowArenaLivesKillKarmaProtection.Value;
    public int killAmountForProtection = BTWRemix.MeadowArenaLivesKillKarmaProtectionAmount.Value;
    public bool killGiveLife = BTWRemix.MeadowArenaLivesKill1UP.Value;
    public int killAmountForLife = BTWRemix.MeadowArenaLivesKill1UPAmount.Value;
    public int rainTimerToSuddentDeath = BTWRemix.MeadowArenaLivesRainTimerToSuddentDeath.Value;

    private OnlinePlayer _selectedPlayer;
    public OnlinePlayer SelectedPlayer
    {
        get
        {
            return _selectedPlayer;
        }
        set
        {
            if (_selectedPlayer != value)
            {
                _selectedPlayer = value;
                if (value != null 
                    && myStockSettingInterface?.LifeAmountOfPlayer_TextBox != null)
                {
                    int lives = this.LivesDefaultAmount;
                    ArenaStockClientSettings stockSettings = ArenaHelpers.GetDataSettings<ArenaStockClientSettings>(value);
                    if (stockSettings != null)
                    {
                        lives = stockSettings.lives;
                    }
                    myStockSettingInterface.LifeAmountOfPlayer_TextBox.cfgEntry = 
                        myStockSettingInterface.CreateDummyConfigurableForPlayerLifeSet(value, lives);
                    myStockSettingInterface.LifeAmountOfPlayer_TextBox.valueInt = lives;
                }
            }
        }
    }

    public ConditionalWeakTable<ArenaPlayerBox, StockPlayerBox> playerBoxes = new();
    
    public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
    {
        base.OnUIEnabled(menu);
        myTab = new(menu, menu.arenaMainLobbyPage.tabContainer);
        myTab.AddObjects(myStockSettingInterface = new OnlineStockArenaModeSettingsInterface((ArenaMode)OnlineManager.lobby.gameMode, this, myTab.menu, myTab, new(0, 0), menu.arenaMainLobbyPage.tabContainer.size));
        menu.arenaMainLobbyPage.tabContainer.AddTab(myTab, menu.Translate("Stock Settings"));
    }
    public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
    {
        base.OnUIDisabled(menu);
        myStockSettingInterface?.OnShutdown();
        if (myTab != null) menu.arenaMainLobbyPage.tabContainer.RemoveTab(myTab);
        myTab = null;
        foreach (ArenaPlayerBox playerBox in menu.arenaMainLobbyPage.playerDisplayer?.GetSpecificButtons<ArenaPlayerBox>())
        {
            if (playerBoxes.TryGetValue(playerBox, out StockPlayerBox stockBox))
            {
                playerBox.ClearMenuObject(stockBox);
                playerBoxes.Remove(playerBox);
            }
        }
    }
    public override void OnUIUpdate(ArenaOnlineLobbyMenu menu)
    {
        base.OnUIUpdate(menu);
        if (menu?.arenaMainLobbyPage?.playerDisplayer?.buttons != null)
        {
            foreach (ButtonScroller.IPartOfButtonScroller button in menu?.arenaMainLobbyPage?.playerDisplayer?.buttons)
            {
                if (button is ArenaPlayerBox playerBox)
                {
                    ArenaStockClientSettings stockSettings = ArenaHelpers.GetDataSettings<ArenaStockClientSettings>(playerBox.profileIdentifier);
                    if (stockSettings != null)
                    {
                        if (stockSettings.lives <= 0)
                        {
                            stockSettings.lives = this.LivesDefaultAmount;
                        }
                        if (playerBoxes.TryGetValue(playerBox, out StockPlayerBox stockBox))
                        {
                            stockBox.ChangeLifeCount(stockSettings.lives);
                            if (stockSettings.lives > this.LivesDefaultAmount)
                            {
                                stockBox.color = Color.cyan;
                            }
                            else if (stockSettings.lives < this.LivesDefaultAmount)
                            {
                                stockBox.color = Color.yellow;
                            }
                            else
                            {
                                stockBox.color = Color.white;
                            }
                        }
                        else 
                        {
                            Vector2 pos;
                            if (playerBox.profileIdentifier.isMe)
                            {
                                pos = new Vector2 (playerBox.infoKickButton.pos.x - 2, playerBox.infoKickButton.pos.y - 30);
                            }
                            else
                            {
                                pos = new Vector2 (playerBox.colorInfoButton.pos.x - 2, playerBox.colorInfoButton.pos.y - 30);
                            }
                            stockBox = new(playerBox.menu, playerBox, 
                                pos, 
                                stockSettings.lives, Vector2.one * 28f);
                            playerBox.subObjects.Add(stockBox);
                            playerBoxes.Add(playerBox, stockBox);
                        }
                    }
                }
                else if (button is ArenaPlayerSmallBox smallBox)
                {
                    // Uh will figure out later
                }
            }
        }
    }
    public override void OnUIShutDown(ArenaOnlineLobbyMenu menu)
    {
        base.OnUIShutDown(menu);
        myStockSettingInterface?.OnShutdown();
    }
}

public class StockPlayerBox : SimplerSymbolButton, ButtonScroller.IPartOfButtonScroller
{
    public Color color = Color.white;
    public int lives = -1;
    public ArenaPlayerBox PlayerBox => owner as ArenaPlayerBox;

    private float _alpha = 0f;
    public float Alpha { get => _alpha; set => _alpha = value; }
    public Vector2 Pos { get => pos; set => pos = value; }
    public Vector2 Size { get => size; set => size = value; }

    public StockPlayerBox(Menu.Menu menu, MenuObject owner, Vector2 pos, int lives, Vector2 size = default) : base(menu, owner, GetSymbol(lives), "STOCK_INFO", pos, "The amount of life this player will have in arena.")
    {
        this.size = size == default ? this.size : size;
        ChangeLifeCount(lives);
    }
    
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        this.symbolSprite.color = color;
        this.symbolSprite.scale = Mathf.Min(this.size.x, this.size.y) / 82f;
        roundedRect.size = size;
        
        if (PlayerBox != null)
        {
            if (OnlineManager.lobby.isOwner)
            {
                this.description = $"Edit the amount of life this player has (currently {this.lives}).";
            }
            else
            {
                this.description = $"The amount of life (currently {this.lives}) this player will have in arena.";
            }
        }
        // BTWPlugin.Log($"Button [{this}] has size [{this.Size}], pos [{this.Pos}], alpha [{this.Alpha}], symbol [{this.symbolSprite.element.name}]");
    }

    public override void Clicked()
    {
        base.Clicked();
        if (PlayerBox != null)
        {
            if (OnlineManager.lobby.isOwner
                && MeadowFunc.IsMeadowArena(out var arenaOnline) 
                && arenaOnline.IsStockArenaMode(out var stockArenaMode))
            {
                this.menu.PlaySound(SoundID.MENU_MultipleChoice_Clicked);
                if (stockArenaMode.SelectedPlayer == PlayerBox.profileIdentifier)
                {
                    stockArenaMode.SelectedPlayer = null;
                }
                else
                {
                   stockArenaMode.SelectedPlayer = PlayerBox.profileIdentifier;
                }
            }
            else
            {
                this.menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }
        }
    }

    public static string GetSymbol(int lives)
    {
        return KarmaMeter.KarmaSymbolSprite(false, new IntVector2(Mathf.Clamp(lives - 1, 0, 9), 9));
    }
    public void ChangeLifeCount(int lives)
    {
        if (lives != this.lives)
        {
            if (lives > 0)
            {
                this.Alpha = 1f;
                this.UpdateSymbol(GetSymbol(lives));
            }
            else
            {
                this.Alpha = 0f;
            }
            this.lives = lives;
        }
    }
}