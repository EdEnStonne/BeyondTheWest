using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using System.Linq;
using RainMeadow.UI.Components.Patched;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow;
using System.Globalization;
using System;
using RainMeadow.UI.Components;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using HUD;
using RWCustom;

namespace BeyondTheWest.MeadowCompat.Gamemodes;
public class OnlineStockArenaModeSettingsInterface : RectangularMenuObject, CheckBox.IOwnCheckBox, MultipleChoiceArray.IOwnMultipleChoiceArray
{
    public const string BLOCKWIN = "BTW_AL_BLW";
    public const string REVIVEFROMABYSS = "BTW_AL_RFA";
    public const string LIFEAMOUNTARRAY = "BTW_AL_LAA";
    public const string EVERYONECANSET = "BTW_AL_ECS";
    public const string STRICTAT0 = "BTW_AL_SA0";
    public const string RESPAWNSHIELD = "BTW_AL_RPS";
    public const string FLOWERKARMAPROTECTION = "BTW_AL_FKP";
    public const string FLOWERKARMA1UP = "BTW_AL_FKU";
    public const string KILLKARMAPROTECTION = "BTW_AL_KKP";
    public const string KILL1UP = "BTW_AL_K1U";

    public const float textSpacing = 300f;

    public readonly string[] lifeAmountPossibilities = new string[]{"1", "2", "3", "5", "8", "10", "99", "?"};

    public MenuLabel Title, WIP_Warning;
    public OpTextBox 
        AdditionalReviveTime_TextBox,
        LifeAmount_TextBox,
        LifeAmountOfPlayer_TextBox,
        ReviveTime_TextBox,
        RespawnShieldDuration_TextBox,
        KillCountKarmaProtection_TextBox, KillCount1UP_TextBox,
        RainTimerToSuddentDeath_TextBox;
    public MenuLabel 
        LifeAmount_Label, LifeAmountOfPlayer_Label,
        ReviveTime_Label, AdditionalReviveTime_Label,
        RainTimerToSuddentDeath_Label;
    public RestorableCheckbox 
        BlockWin_CheckBox,
        EveryoneCanModifyTheirLifeAmount_CheckBox,
        StrictAt0Lives_CheckBox,
        RespawnShield_CheckBox,
        KarmaFlowerProtection_CheckBox, KarmaFlower1UP_CheckBox,
        KillKarmaProtection_CheckBox, Kill1UP_CheckBox;
    public MultipleChoiceArray LifeAmount_MultipleChoiceArray;
    public StockButton LifeButton;
    public MenuTabWrapper tabWrapper;

    public ArenaMode arenaMode;
    public StockArenaMode stockMode;

    public bool AllSettingsDisabled => arenaMode.initiateLobbyCountdown && arenaMode.arenaClientSettings.ready;
    public bool OwnerSettingsDisabled => !(OnlineManager.lobby?.isOwner == true) || AllSettingsDisabled;
    public bool IsPagesOn => true;

    public bool IsActuallyHidden { get; private set; }

    private void CreateLabel(ref MenuLabel title, string text, Vector2 pos, Color color, FLabelAlignment alignment = FLabelAlignment.Center, float size = textSpacing, bool big = false)
    {
        Vector2 sizeV = new(size, big ? 25 : 20);
        title = new(menu, this, menu.Translate(text), 
            pos - sizeV.y * Vector2.up - (alignment == FLabelAlignment.Left ? sizeV.x/2 * Vector2.right : alignment == FLabelAlignment.Right ? sizeV.x/2 * Vector2.left : Vector2.zero), 
            sizeV, big);
        title.label.alignment = alignment;
        title.label.color = color;
    }
    private void CreateIntTextBox(ref OpTextBox textBox, Configurable<int> setting, Vector2 pos, FLabelAlignment alignment = FLabelAlignment.Center, float size = 40)
    { 
        Vector2 sizeV = new(size, 24f);
        textBox = new OpTextBox(
            setting,
            pos - sizeV,
            sizeV.x)
        {
            alignment = alignment,
            description = setting.info.description
        };
        new PatchedUIelementWrapper(tabWrapper, textBox);
    }
    private void CreateRepeatArray(ref MultipleChoiceArray multipleChoiceArray, string settingID, string[] buttonText, Vector2 pos, float width = 300)
    {
        Vector2 sizeV = new(width, 24);
        multipleChoiceArray = new MultipleChoiceArray(
            menu, this, this, pos - Vector2.up * sizeV.y, 
            "", settingID, 
            0, width, buttonText.Length, true, false);

        for (int i = 0; i < buttonText.Length; i++)
        {
            multipleChoiceArray.buttons[i].label.text = buttonText[i];
        }
    }
    private void CreateCheckBox(ref RestorableCheckbox checkbox, Configurable<bool> setting, string settingName, string settingID, Vector2 pos, float size = textSpacing)
    {
        Vector2 sizeV = new(-size + 24, 24);
        checkbox = new(
            menu, this, this, pos - sizeV, -sizeV.x, settingName,
            settingID, false, setting.info.description);
    }
    public Configurable<int> CreateDummyConfigurableForPlayerLifeSet(OnlinePlayer onlinePlayer, int lives = -1)
    {
        if (lives <= 0)
        {
            lives = stockMode.LivesDefaultAmount;
            ArenaStockClientSettings stockSettings = ArenaHelpers.GetDataSettings<ArenaStockClientSettings>(onlinePlayer);
            if (stockSettings != null)
            {
                lives = stockSettings.lives;
            }
        }
        return new Configurable<int>(lives, BTWRemix.MeadowArenaLivesAmount.info.acceptable)
        {
            info = new ConfigurableInfo($"Set the life amount of {onlinePlayer.id.DisplayName} (was {lives}).", 
                BTWRemix.MeadowArenaLivesAmount.info.acceptable),
            Value = lives
        };
    }
    public OnlineStockArenaModeSettingsInterface(ArenaMode arena, StockArenaMode stockMode, Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        this.arenaMode = arena;
        this.stockMode = stockMode;
        this.tabWrapper = new(menu, this);


        CreateLabel(ref WIP_Warning, "This gamemode is still WIP (here be dragons !)", new(10, size.y - 10), Color.red, FLabelAlignment.Center, size.x - 20);
        

        LifeButton = new(menu, this, new(size.x* 3/4 + 5, size.y - size.x/4 - 20), new(size.x/4 - 15, size.x/4 - 15), stockMode.LivesDefaultAmount);

        CreateIntTextBox(ref LifeAmountOfPlayer_TextBox, CreateDummyConfigurableForPlayerLifeSet(OnlineManager.mePlayer, stockMode.LivesDefaultAmount), new(size.x - 10, size.y - size.x/4 - 25), size: size.x/4 - 15);
        LifeAmountOfPlayer_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (stockMode.SelectedPlayer != null)
            {
                int lives = LifeAmountOfPlayer_TextBox.cfgEntry.ClampValue(LifeAmountOfPlayer_TextBox.valueInt);
                if (stockMode.SelectedPlayer.isMe)
                {
                    ArenaStockClientSettings stockSettings = ArenaHelpers.GetDataSettings<ArenaStockClientSettings>(OnlineManager.mePlayer);
                    if (stockSettings != null)
                    {
                        stockSettings.lives = lives;
                    }
                }
                else if (OnlineManager.lobby.isOwner)
                {
                    MeadowCalls.BTWStockArena_RequestLifeChange(stockMode.SelectedPlayer, lives);
                }
            }
        };
        CreateLabel(ref LifeAmountOfPlayer_Label, "", new(size.x* 3/4 + 5, size.y - size.x/4 - 55), Color.white, FLabelAlignment.Center, size.x/4 - 15);

        CreateLabel(ref LifeAmount_Label, "Default life count :", new(25, size.y - 40), Color.white, FLabelAlignment.Left, size.x * 1/2 - 10);
        CreateIntTextBox(ref LifeAmount_TextBox, BTWRemix.MeadowArenaLivesAmount, new(size.x * 3/4 - 10, size.y - 40), size: size.x * 1/4 - 10);
        LifeAmount_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            stockMode.LivesDefaultAmount = LifeAmount_TextBox.cfgEntry.ClampValue(LifeAmount_TextBox.valueInt);
            LifeButton.ChangeLifeCount(LifeAmount_TextBox.valueInt);
        };
        CreateRepeatArray(ref LifeAmount_MultipleChoiceArray, LIFEAMOUNTARRAY, lifeAmountPossibilities, new(25, size.y - 72), size.x * 3/4 - 35);

        CreateCheckBox(ref EveryoneCanModifyTheirLifeAmount_CheckBox, BTWRemix.MeadowArenaLivesEveryoneCanSet,
            "Everyone can set their life count :", EVERYONECANSET, new(25, size.y - 104), size.x * 3/4 - 35);
        

        CreateCheckBox(ref StrictAt0Lives_CheckBox, BTWRemix.MeadowArenaLivesStrict,
            "Stop reviving at 0 lives :", STRICTAT0, new(25, size.y - 150), size.x * 2/3 - 35);
        
        CreateCheckBox(ref BlockWin_CheckBox, BTWRemix.MeadowArenaLivesBlockWin,
            "Block arena from closing :", BLOCKWIN, new(25, size.y - 180), size.x * 2/3 - 35);
        
        CreateLabel(ref ReviveTime_Label, "Revive time :", new(25, size.y - 210), Color.white, FLabelAlignment.Left, size.x * 2/3 - 10);
        CreateIntTextBox(ref ReviveTime_TextBox, BTWRemix.MeadowArenaLivesReviveTime, new(size.x * 2/3 - 10, size.y - 210));
        ReviveTime_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            stockMode.reviveTime = ReviveTime_TextBox.cfgEntry.ClampValue(ReviveTime_TextBox.valueInt);
        };

        CreateLabel(ref AdditionalReviveTime_Label, "Additional revive time :", new(25, size.y - 240), Color.white, FLabelAlignment.Left, size.x * 2/3 - 10);
        CreateIntTextBox(ref AdditionalReviveTime_TextBox, BTWRemix.MeadowArenaLivesAdditionalReviveTime, new(size.x * 2/3 - 10, size.y - 240));
        AdditionalReviveTime_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            stockMode.additionalReviveTime = AdditionalReviveTime_TextBox.cfgEntry.ClampValue(AdditionalReviveTime_TextBox.valueInt);
        };

        CreateCheckBox(ref RespawnShield_CheckBox, BTWRemix.MeadowArenaLivesRespawnShield,
            "Respawn Shield :", RESPAWNSHIELD, new(25, size.y - 270), size.x * 2/3 - 35);
        CreateIntTextBox(ref RespawnShieldDuration_TextBox, BTWRemix.MeadowArenaLivesRespawnShieldDuration, new(size.x * 2/3 + 35, size.y - 270));
        RespawnShieldDuration_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            stockMode.respawnShieldDuration = RespawnShieldDuration_TextBox.cfgEntry.ClampValue(RespawnShieldDuration_TextBox.valueInt);
        };

        CreateCheckBox(ref KarmaFlowerProtection_CheckBox, BTWRemix.MeadowArenaLivesStrict,
            "Karma flower gives protection :", FLOWERKARMAPROTECTION, new(25, size.y - 300), size.x * 2/3 - 35);
        
        CreateCheckBox(ref KarmaFlower1UP_CheckBox, BTWRemix.MeadowArenaLivesBlockWin,
            "Karma flower gives +1 live :", FLOWERKARMA1UP, new(25, size.y - 330), size.x * 2/3 - 35);

        CreateCheckBox(ref KillKarmaProtection_CheckBox, BTWRemix.MeadowArenaLivesKillKarmaProtection,
            "Kills give protection :", KILLKARMAPROTECTION, new(25, size.y - 360), size.x * 2/3 - 35);
        CreateIntTextBox(ref KillCountKarmaProtection_TextBox, BTWRemix.MeadowArenaLivesKillKarmaProtectionAmount, new(size.x * 2/3 + 35, size.y - 360));
        KillCountKarmaProtection_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            stockMode.killAmountForProtection = KillCountKarmaProtection_TextBox.cfgEntry.ClampValue(KillCountKarmaProtection_TextBox.valueInt);
        };

        CreateCheckBox(ref Kill1UP_CheckBox, BTWRemix.MeadowArenaLivesKill1UP,
            "Kills give +1 live :", KILL1UP, new(25, size.y - 390), size.x * 2/3 - 35);
        CreateIntTextBox(ref KillCount1UP_TextBox, BTWRemix.MeadowArenaLivesKill1UPAmount, new(size.x * 2/3 + 35, size.y - 390));
        KillCount1UP_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            stockMode.killAmountForLife = KillCount1UP_TextBox.cfgEntry.ClampValue(KillCount1UP_TextBox.valueInt);
        };
        
        CreateLabel(ref RainTimerToSuddentDeath_Label, "Rain time before sudden death :", new(25, size.y - 420), Color.white, FLabelAlignment.Left, size.x - 35);
        CreateIntTextBox(ref RainTimerToSuddentDeath_TextBox, BTWRemix.MeadowArenaLivesRainTimerToSuddentDeath, new(size.x - 55, size.y - 420), size: size.x / 3);
        RainTimerToSuddentDeath_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            stockMode.rainTimerToSuddentDeath = RainTimerToSuddentDeath_TextBox.cfgEntry.ClampValue(RainTimerToSuddentDeath_TextBox.valueInt);
        };

        this.SafeAddSubobjects(
            tabWrapper,
            WIP_Warning, 

            EveryoneCanModifyTheirLifeAmount_CheckBox, StrictAt0Lives_CheckBox, BlockWin_CheckBox, RespawnShield_CheckBox,
            KarmaFlowerProtection_CheckBox, KarmaFlower1UP_CheckBox, KillKarmaProtection_CheckBox, Kill1UP_CheckBox,

            LifeAmount_Label, LifeAmountOfPlayer_Label, ReviveTime_Label, AdditionalReviveTime_Label, RainTimerToSuddentDeath_Label,

            LifeButton,

            LifeAmount_MultipleChoiceArray
        );
    }

    public void SyncMenuObjectStatus(MenuObject obj)
    {
        if (obj is CheckBox checkBox) { checkBox.Checked = checkBox.Checked; }
    }
    public void ClearInterface()
    {
        this.ClearMenuObject(LifeButton);
    }
    public void UnloadAnyConfig(params UIelement[] elements)
    {
        if (elements == null) return;
        foreach (UIelement element in elements)
        {
            if (tabWrapper.wrappers.ContainsKey(element))
            {
                tabWrapper.ClearMenuObject(tabWrapper.wrappers[element]);
                tabWrapper.wrappers.Remove(element);
            }
            element.Unload();
        }
    }

    public void OnShutdown()
    {
        if (!(OnlineManager.lobby?.isOwner == true)) return;
        BTWRemix.MeadowArenaLivesAmount.Value = this.LifeAmount_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesReviveTime.Value = this.ReviveTime_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesAdditionalReviveTime.Value = this.AdditionalReviveTime_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesRespawnShieldDuration.Value = this.RespawnShieldDuration_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesKillKarmaProtectionAmount.Value = this.KillCountKarmaProtection_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesKill1UPAmount.Value = this.KillCount1UP_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesRainTimerToSuddentDeath.Value = this.RainTimerToSuddentDeath_TextBox.valueInt;

        BTWRemix.MeadowArenaLivesEveryoneCanSet.Value = this.EveryoneCanModifyTheirLifeAmount_CheckBox.Checked;
        BTWRemix.MeadowArenaLivesStrict.Value = this.StrictAt0Lives_CheckBox.Checked;
        BTWRemix.MeadowArenaLivesBlockWin.Value = this.BlockWin_CheckBox.Checked;
        BTWRemix.MeadowArenaLivesRespawnShield.Value = this.RespawnShield_CheckBox.Checked;
        BTWRemix.MeadowArenaLivesKarmaFlowerProtection.Value = this.KarmaFlowerProtection_CheckBox.Checked;
        BTWRemix.MeadowArenaLivesKarmaFlower1UP.Value = this.KarmaFlower1UP_CheckBox.Checked;
        BTWRemix.MeadowArenaLivesKillKarmaProtection.Value = this.KillKarmaProtection_CheckBox.Checked;
        BTWRemix.MeadowArenaLivesKill1UP.Value = this.Kill1UP_CheckBox.Checked;
        
        BTWRemix.instance._SaveConfigFile();
    }
    
    void UpdateIntTextBox(ref OpTextBox textBox, int value, bool greyoutAll)
    {
        textBox.greyedOut = greyoutAll;
        textBox.held = textBox._KeyboardOn;
        if (!textBox.held) { textBox.valueInt = value; }
    }
    public override void Update()
    {
        base.Update();
        if (this.arenaMode == null) { return; }
        if (IsActuallyHidden) return; 
        
        bool greyoutAll = OwnerSettingsDisabled;
        foreach (MenuObject obj in subObjects)
        {
            if (obj is ButtonTemplate btn) { btn.buttonBehav.greyedOut = greyoutAll; }
        }
        
        if (this.stockMode == null) { return; }

        bool greyoutClient = !(this.stockMode.everyoneCanModifyTheirLifeAmount && !AllSettingsDisabled);
        UpdateIntTextBox(ref this.LifeAmount_TextBox, this.stockMode.LivesDefaultAmount, greyoutAll);
        LifeAmount_MultipleChoiceArray.greyedOut = greyoutAll;
        UpdateIntTextBox(ref this.ReviveTime_TextBox, this.stockMode.reviveTime, greyoutAll);
        UpdateIntTextBox(ref this.AdditionalReviveTime_TextBox, this.stockMode.additionalReviveTime, greyoutAll);
        UpdateIntTextBox(ref this.RespawnShieldDuration_TextBox, this.stockMode.respawnShieldDuration, greyoutAll);
        if (this.RespawnShield_CheckBox.Checked) { this.RespawnShieldDuration_TextBox.Show(); } else { this.RespawnShieldDuration_TextBox.Hide(); }
        UpdateIntTextBox(ref this.KillCountKarmaProtection_TextBox, this.stockMode.killAmountForProtection, greyoutAll);
        if (this.KillKarmaProtection_CheckBox.Checked) { this.KillCountKarmaProtection_TextBox.Show(); } else { this.KillCountKarmaProtection_TextBox.Hide(); }
        UpdateIntTextBox(ref this.KillCount1UP_TextBox, this.stockMode.killAmountForLife, greyoutAll);
        if (this.Kill1UP_CheckBox.Checked) { this.KillCount1UP_TextBox.Show(); } else { this.KillCount1UP_TextBox.Hide(); }
        UpdateIntTextBox(ref this.RainTimerToSuddentDeath_TextBox, this.stockMode.rainTimerToSuddentDeath, greyoutAll);

        if (OnlineManager.lobby.isOwner)
        {
            if (stockMode.SelectedPlayer == null || greyoutAll)
            {
                LifeAmountOfPlayer_TextBox.Hide();
            }
            else
            {
                LifeAmountOfPlayer_TextBox.Show();
            }
            LifeAmountOfPlayer_Label.text = LifeAmountOfPlayer_TextBox.Hidden ? "" : $"{stockMode.SelectedPlayer.id.DisplayName}";
        }
        else
        {
            stockMode.SelectedPlayer = OnlineManager.mePlayer;
            if (greyoutClient)
            {
                LifeAmountOfPlayer_TextBox.Hide();
            }
            else
            {
                LifeAmountOfPlayer_TextBox.Show();
            }
            LifeAmountOfPlayer_Label.text = LifeAmountOfPlayer_TextBox.Hidden ? "" : $"Your lives";
        }
        UpdateIntTextBox(ref this.LifeAmountOfPlayer_TextBox, 
            stockMode.SelectedPlayer == null 
                || ArenaHelpers.GetDataSettings<ArenaStockClientSettings>(stockMode.SelectedPlayer) is not ArenaStockClientSettings settings
                ? stockMode.LivesDefaultAmount 
                : settings.lives, 
            greyoutClient && greyoutAll);
        
        LifeButton.buttonBehav.greyedOut = greyoutClient && greyoutAll;

    }
    
    void GrafUpdateIntTextBox(ref OpTextBox textBox, ref MenuLabel label)
    {
        label.label.color = textBox.rect.colorEdge;
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (IsActuallyHidden) return;

        GrafUpdateIntTextBox(ref LifeAmount_TextBox, ref LifeAmount_Label);
        GrafUpdateIntTextBox(ref LifeAmountOfPlayer_TextBox, ref LifeAmountOfPlayer_Label);
        GrafUpdateIntTextBox(ref ReviveTime_TextBox, ref ReviveTime_Label);
        GrafUpdateIntTextBox(ref AdditionalReviveTime_TextBox, ref AdditionalReviveTime_Label);
        GrafUpdateIntTextBox(ref RainTimerToSuddentDeath_TextBox, ref RainTimerToSuddentDeath_Label);
    }

    public bool GetChecked(CheckBox box)
    {
        string id = box.IDString;
        if (id == EVERYONECANSET) { return this.stockMode.everyoneCanModifyTheirLifeAmount; }
        if (id == STRICTAT0) { return this.stockMode.strictEnforceAfter0Lives; }
        if (id == BLOCKWIN) { return this.stockMode.blockWin; }
        if (id == RESPAWNSHIELD) { return this.stockMode.respawnShieldToggle; }
        if (id == FLOWERKARMAPROTECTION) { return this.stockMode.karmaFlowerGiveProtection; }
        if (id == FLOWERKARMA1UP) { return this.stockMode.karmaFlowerGiveLife; }
        if (id == KILLKARMAPROTECTION) { return this.stockMode.killGiveProtection; }
        if (id == KILL1UP) { return this.stockMode.killGiveLife; }
        return false;
    }
    public void SetChecked(CheckBox box, bool c)
    {
        string id = box.IDString;
        if (id == EVERYONECANSET) { this.stockMode.everyoneCanModifyTheirLifeAmount = c; }
        if (id == STRICTAT0) { this.stockMode.strictEnforceAfter0Lives = c; }
        if (id == BLOCKWIN) { this.stockMode.blockWin = c; }
        if (id == RESPAWNSHIELD) { this.stockMode.respawnShieldToggle = c; }
        if (id == FLOWERKARMAPROTECTION) { this.stockMode.karmaFlowerGiveProtection = c; }
        if (id == FLOWERKARMA1UP) { this.stockMode.karmaFlowerGiveLife = c; }
        if (id == KILLKARMAPROTECTION) { this.stockMode.killGiveProtection = c; }
        if (id == KILL1UP) { this.stockMode.killGiveLife = c; }
    }

    public int GetSelected(MultipleChoiceArray array)
    {
        if (array.IDString == LIFEAMOUNTARRAY)
        {
            for (int i = 0; i < lifeAmountPossibilities.Length - 1; i++)
            {
                if (int.TryParse(lifeAmountPossibilities[i], out int lives) && lives == this.stockMode.LivesDefaultAmount)
                {
                    return i;
                }
            }
            return lifeAmountPossibilities.Length - 1;
        }
        return 0;
    }
    public void SetSelected(MultipleChoiceArray array, int i)
    {
        if (array.IDString == LIFEAMOUNTARRAY)
        {
            if (int.TryParse(lifeAmountPossibilities[i], out int lives))
            {
                this.stockMode.LivesDefaultAmount = lives;
                LifeAmount_TextBox.valueInt = lives;
                LifeButton.ChangeLifeCount(LifeAmount_TextBox.valueInt);
            }
        }
    }

    public class StockButton : SimplerButton
    {
        public float widthOfText = 190;
        public FSprite liveSymbol;
        public ProperlyAlignedMenuLabel liveCount;
        public Color color = Color.white;
        public int lives = -1;
        public StockButton(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, int lives) : base(menu, owner, "", pos, size, "Set back the life count to default")
        {
            liveSymbol = new(KarmaMeter.KarmaSymbolSprite(false, new IntVector2(Mathf.Clamp(lives - 1, 0, 9), 9)));
            Container.AddChild(liveSymbol);
            liveCount = new(menu, this, "", new Vector2(liveSymbol.x + 12, liveSymbol.y + 12), new(12, 12), false);
            ChangeLifeCount(lives);
            this.SafeAddSubobjects(liveCount);
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 drawPos = DrawPos(timeStacker), drawSize = DrawSize(timeStacker);
            liveSymbol.x = drawPos.x + drawSize.x * 0.5f;
            liveSymbol.y = drawPos.y + drawSize.y * 0.5f;
            liveSymbol.scale = Mathf.Min(this.size.x, this.size.y) / 100f;
            liveSymbol.color = color;
        }
        public override void RemoveSprites()
        {
            liveSymbol.RemoveFromContainer();
            base.RemoveSprites();
        }

        public override void Clicked()
        {
            base.Clicked();
            if (this.owner is OnlineStockArenaModeSettingsInterface stockInterface)
            {
                if (!stockInterface.OwnerSettingsDisabled 
                    || (stockInterface.stockMode.everyoneCanModifyTheirLifeAmount 
                        && !stockInterface.AllSettingsDisabled))
                {
                    this.menu.PlaySound(SoundID.MENU_Button_Successfully_Assigned);
                    if (BTWMeadowArenaSettings.TryGetSettings(out var settings))
                    {
                        settings.arenaStockClientSettings.lives = stockInterface.stockMode.LivesDefaultAmount;
                    }
                }
                else
                {
                    this.menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                }
            }
        }
        
        public void ChangeLifeCount(int lives)
        {
            if (lives != this.lives)
            {
                if (lives > 0)
                {
                    liveSymbol.alpha = 1f;
                    liveSymbol.SetElementByName(KarmaMeter.KarmaSymbolSprite(false, new IntVector2(Mathf.Clamp(lives - 1, 0, 9), 9)));
                    liveCount.text = lives > 100 ? "+99" : lives.ToString();
                }
                else
                {
                    liveSymbol.alpha = 0f;
                    liveCount.text = "";
                }
                this.lives = lives;
            }
        }
    }
}
