using UnityEngine;
using System;
using RainMeadow;
using BeyondTheWest;

using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using RainMeadow.UI.Components;


namespace BeyondTheWest.MeadowCompat.ArenaMenu;

public class BTWArenaSettingsPage : SettingsPage, CheckBox.IOwnCheckBox
{
    public const string AL_BLOCKWIN = "BTW_AL_BLW";
    public const string AL_REVIVEFROMABYSS = "BTW_AL_RFA";
    public const string AL_STRICT = "BTW_AL_STR";
    public const string AI_NEWITEMSPAWNSYSTEM = "BTW_AI_NSS";
    public const string AI_ITEMSPAWNDIVERSITY = "BTW_AI_ISD";
    public const string AI_ITEMSPAWNRANDOM = "BTW_AI_ISR";
    public MenuTabWrapper tabWrapper;

    public MenuLabel ArenaLives_Title, ArenaItems_Title, WIP_Warning;
    public OpTextBox ArenaLives_AdditionalReviveTime_TextBox,
        ArenaLives_Amount_TextBox,
        ArenaLives_ReviveTime_TextBox,
        ArenaLives_RespawnShieldDuration_TextBox,
        ArenaItems_ItemSpawnMultiplierCent_TextBox,
        ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox;
    public MenuLabel ArenaLives_AdditionalReviveTime_Label,
        ArenaLives_Amount_Label, 
        ArenaLives_ReviveTime_Label,
        ArenaLives_RespawnShieldDuration_Label,
        ArenaItems_ItemSpawnMultiplierCent_Label,
        ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label;
    public RestorableCheckbox ArenaLives_BlockWin_CheckBox, 
        ArenaLives_Strict_CheckBox, 
        ArenaItems_NewItemSpawningSystem_CheckBox, 
        ArenaItems_ItemSpawnDiversity_CheckBox, 
        ArenaItems_ItemSpawnRandom_CheckBox;

    public SimpleButton backButton;

    public override string Name => "BTW Arena Settings"; //this will appear on Select Settings Page

    public BTWArenaSettingsPage(Menu.Menu menu, MenuObject owner, Vector2 spacing, float textSpacing = 300) : base(menu, owner)
    {
        tabWrapper = new(menu, this);
        Vector2 positioner = new(360 + ((textSpacing - 300) / 2), 410);
        
        void CreateIntTextBox(ref OpTextBox textBox, ref MenuLabel label, Configurable<int> setting, string settingName, int place, bool longInt = false)
        { // got lazy, automatized it
            textBox = new OpTextBox(
                setting,
                positioner - spacing * place + new Vector2(longInt? -47.5f : -7.5f, 0),
                longInt? 80 : 40)
            {
                alignment = FLabelAlignment.Center,
                description = setting.info.description
            };
            label = new(this.menu, this, settingName, textBox.pos + new Vector2(-textSpacing * 1.5f + (longInt? 47.5f : 7.5f), 3), new(textSpacing, 20), false);
            label.label.alignment = FLabelAlignment.Left;
            new PatchedUIelementWrapper(tabWrapper, textBox);
        }
        void CreateCheckBox(ref RestorableCheckbox checkbox, Configurable<bool> setting, string settingName, string settingID, int place)
        { // automatized this too
            checkbox = new(
                menu, this, this, positioner - spacing * place, textSpacing, settingName,
                settingID, false, setting.info.description);
        }
        void CreateTitle(ref MenuLabel title, string titleName, int place)
        { // atp why not
            title = new(menu, this, titleName, positioner - spacing * place + new Vector2(-textSpacing * 1.0f + 7.5f, 3), new(textSpacing, 25), true);
            title.label.alignment = FLabelAlignment.Center;
        }
        void CreateWarning(ref MenuLabel title, string text, float place, bool big = false)
        { // atp why not
            title = new(menu, this, text, positioner - spacing * place + new Vector2(-textSpacing * 1.0f + 7.5f, 3), new(textSpacing, big ? 25 : 20), big);
            title.label.alignment = FLabelAlignment.Center;
            title.label.color = Color.red;
        }

        CreateWarning(ref WIP_Warning, "Those settings are still WIP (here be dragons !)", -1f);

        //--------------------- Arena Lives
        CreateTitle(ref ArenaLives_Title, "Arena Lives", 0);

        // ArenaLives_Amount
        CreateIntTextBox(ref ArenaLives_Amount_TextBox, ref ArenaLives_Amount_Label,
            BTWRemix.MeadowArenaLivesAmount, "Lives :", 1);

        ArenaLives_Amount_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaLives_Amount =
                BTWRemix.MeadowArenaLivesAmount.ClampValue(ArenaLives_Amount_TextBox.valueInt);
        };

        // ArenaLives_Strict
        CreateCheckBox(ref ArenaLives_Strict_CheckBox, BTWRemix.MeadowArenaLivesStrict,
            "Block revives after reaching 0 live :", AL_STRICT, 2);

        // ArenaLives_BlockWin
        CreateCheckBox(ref ArenaLives_BlockWin_CheckBox, BTWRemix.MeadowArenaLivesBlockWin,
            "Block session end on revival :", AL_BLOCKWIN, 3);

        //  ArenaLives_ReviveTime
        CreateIntTextBox(ref ArenaLives_ReviveTime_TextBox, ref ArenaLives_ReviveTime_Label,
            BTWRemix.MeadowArenaLivesReviveTime, "Revive time :", 4);

        ArenaLives_ReviveTime_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaLives_ReviveTime =
                BTWRemix.MeadowArenaLivesReviveTime.ClampValue(ArenaLives_ReviveTime_TextBox.valueInt);
        };

        //  ArenaLives_ReviveTime
        CreateIntTextBox(ref ArenaLives_AdditionalReviveTime_TextBox, ref ArenaLives_AdditionalReviveTime_Label,
            BTWRemix.MeadowArenaLivesAdditionalReviveTime, "Additionnal per life revive time :", 5);

        ArenaLives_AdditionalReviveTime_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaLives_AdditionalReviveTime =
                BTWRemix.MeadowArenaLivesAdditionalReviveTime.ClampValue(ArenaLives_AdditionalReviveTime_TextBox.valueInt);
        };

        //  ArenaLives_RespawnShieldDuration
        CreateIntTextBox(ref ArenaLives_RespawnShieldDuration_TextBox, ref ArenaLives_RespawnShieldDuration_Label,
            BTWRemix.MeadowArenaLivesRespawnShieldDuration, "Respawn shield duration :", 6);

        ArenaLives_RespawnShieldDuration_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaLives_RespawnShieldDuration =
                BTWRemix.MeadowArenaLivesRespawnShieldDuration.ClampValue(ArenaLives_RespawnShieldDuration_TextBox.valueInt);
        };
        
        //--------------------- Arena Item Spawn 
        CreateTitle(ref ArenaItems_Title, "Arena Item Spawn", 7);

        // ArenaItems_NewItemSpawningSystem
        CreateCheckBox(ref ArenaItems_NewItemSpawningSystem_CheckBox, BTWRemix.MeadowNewItemSpawningSystem,
            "New item spawning system :", AI_ITEMSPAWNDIVERSITY, 8);

        // ArenaItems_ItemSpawnDiversity
        CreateCheckBox(ref ArenaItems_ItemSpawnDiversity_CheckBox, BTWRemix.MeadowItemSpawnDiversity,
            "Item Diversity :", AI_NEWITEMSPAWNSYSTEM, 9);

        // ArenaItems_ItemSpawnRandom
        CreateCheckBox(ref ArenaItems_ItemSpawnRandom_CheckBox, BTWRemix.MeadowItemSpawnRandom,
            "Random Items :", AI_ITEMSPAWNRANDOM, 10);

        //  ArenaItems_ItemSpawnMultiplierCent
        CreateIntTextBox(ref ArenaItems_ItemSpawnMultiplierCent_TextBox, ref ArenaItems_ItemSpawnMultiplierCent_Label,
            BTWRemix.MeadowItemSpawnMultiplierCent, "Item Spawn multiplicator % :", 11, true);

        ArenaItems_ItemSpawnMultiplierCent_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaItems_ItemSpawnMultiplierCent =
                BTWRemix.MeadowItemSpawnMultiplierCent.ClampValue(ArenaItems_ItemSpawnMultiplierCent_TextBox.valueInt);
        };

        //  ArenaItems_ItemSpawnMultiplierPerPlayersCent
        CreateIntTextBox(ref ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox, ref ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label,
            BTWRemix.MeadowItemSpawnMultiplierPerPlayersCent, "Added multiplicator per player % :", 12, true);

        ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaItems_ItemSpawnMultiplierPerPlayersCent =
                BTWRemix.MeadowItemSpawnMultiplierPerPlayersCent.ClampValue(ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox.valueInt);
        };


        this.SafeAddSubobjects(
            tabWrapper,
            WIP_Warning, ArenaLives_Title, ArenaItems_Title,

            ArenaLives_BlockWin_CheckBox, 
            ArenaLives_Strict_CheckBox,
            ArenaItems_NewItemSpawningSystem_CheckBox,
            ArenaItems_ItemSpawnDiversity_CheckBox,
            ArenaItems_ItemSpawnRandom_CheckBox,

            ArenaLives_AdditionalReviveTime_Label,
            ArenaLives_Amount_Label, 
            ArenaLives_ReviveTime_Label,
            ArenaLives_RespawnShieldDuration_Label,
            ArenaItems_ItemSpawnMultiplierCent_Label,
            ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label);
    }

    public void SyncMenuObjectStatus(MenuObject obj)
    {
        if (obj is CheckBox checkBox) { checkBox.Checked = checkBox.Checked; }
    }
    public override void SaveInterfaceOptions()
    {
        BTWRemix.MeadowArenaLivesAdditionalReviveTime.Value = this.ArenaLives_AdditionalReviveTime_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesAmount.Value = this.ArenaLives_Amount_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesBlockWin.Value = this.ArenaLives_BlockWin_CheckBox.Checked;
        BTWRemix.MeadowArenaLivesReviveTime.Value = this.ArenaLives_ReviveTime_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesRespawnShieldDuration.Value = this.ArenaLives_RespawnShieldDuration_TextBox.valueInt;
        BTWRemix.MeadowArenaLivesStrict.Value = this.ArenaLives_Strict_CheckBox.Checked;
        
        BTWRemix.MeadowNewItemSpawningSystem.Value = this.ArenaItems_NewItemSpawningSystem_CheckBox.Checked;
        BTWRemix.MeadowItemSpawnDiversity.Value = this.ArenaItems_ItemSpawnDiversity_CheckBox.Checked;
        BTWRemix.MeadowItemSpawnRandom.Value = this.ArenaItems_ItemSpawnRandom_CheckBox.Checked;
        BTWRemix.MeadowItemSpawnMultiplierCent.Value = this.ArenaItems_ItemSpawnMultiplierCent_TextBox.valueInt;
        BTWRemix.MeadowItemSpawnMultiplierPerPlayersCent.Value = this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox.valueInt;
    }
    public override void CallForSync()
    {
        foreach (MenuObject menuObj in subObjects) { SyncMenuObjectStatus(menuObj); }
        
        if (!BTWMeadowArenaSettings.TryGetSettings(out var btwSettings)) return;

        btwSettings.ArenaLives_AdditionalReviveTime = this.ArenaLives_AdditionalReviveTime_TextBox.valueInt;
        btwSettings.ArenaLives_Amount = this.ArenaLives_Amount_TextBox.valueInt;
        btwSettings.ArenaLives_ReviveTime = this.ArenaLives_ReviveTime_TextBox.valueInt;
        btwSettings.ArenaLives_RespawnShieldDuration = this.ArenaLives_RespawnShieldDuration_TextBox.valueInt;

        btwSettings.ArenaItems_ItemSpawnMultiplierCent = this.ArenaItems_ItemSpawnMultiplierCent_TextBox.valueInt;
        btwSettings.ArenaItems_ItemSpawnMultiplierPerPlayersCent = this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox.valueInt;
    }
    
    public override void SelectAndCreateBackButtons(SettingsPage previousSettingPage, bool forceSelectedObject)
    {
        if (backButton == null)
            {
                backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(15, 15), new(80, 30));
                AddObjects(backButton);
                menu.MutualVerticalButtonBind(backButton, ArenaLives_Amount_Label);
                menu.MutualVerticalButtonBind(ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label, backButton); //loop
            }
            if (forceSelectedObject) menu.selectedObject = ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label;
    }
    public override void Update()
    {
        base.Update();
        if (IsActuallyHidden) return; 
        
        bool greyoutAll = SettingsDisabled;
        foreach (MenuObject obj in subObjects)
        {
            if (obj != backButton && obj is ButtonTemplate btn) { btn.buttonBehav.greyedOut = greyoutAll; }
        }
        
        if (BTWMeadowArenaSettings.TryGetSettings(out var settings))
        {
            void UpdateIntTextBox(ref OpTextBox textBox, int value )
            { // oops, it's all functions
                textBox.greyedOut = greyoutAll;
                textBox.held = textBox._KeyboardOn;
                if (!textBox.held) { textBox.valueInt = value; }
            }
            UpdateIntTextBox(ref this.ArenaLives_AdditionalReviveTime_TextBox, settings.ArenaLives_AdditionalReviveTime);
            UpdateIntTextBox(ref this.ArenaLives_Amount_TextBox, settings.ArenaLives_Amount);
            UpdateIntTextBox(ref this.ArenaLives_ReviveTime_TextBox, settings.ArenaLives_ReviveTime);
            UpdateIntTextBox(ref this.ArenaLives_RespawnShieldDuration_TextBox, settings.ArenaLives_RespawnShieldDuration);

            UpdateIntTextBox(ref this.ArenaItems_ItemSpawnMultiplierCent_TextBox, settings.ArenaItems_ItemSpawnMultiplierCent);
            UpdateIntTextBox(ref this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox, settings.ArenaItems_ItemSpawnMultiplierPerPlayersCent);
        }
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (IsActuallyHidden) return;

        static void GrafUpdateIntTextBox(ref OpTextBox textBox, ref MenuLabel label)
        { // can you guess this menu made me go crazy ? No ? Too bad I just told you.
            label.label.color = textBox.rect.colorEdge;
        }
        GrafUpdateIntTextBox(ref ArenaLives_AdditionalReviveTime_TextBox, ref ArenaLives_AdditionalReviveTime_Label);
        GrafUpdateIntTextBox(ref ArenaLives_Amount_TextBox, ref ArenaLives_Amount_Label);
        GrafUpdateIntTextBox(ref ArenaLives_ReviveTime_TextBox, ref ArenaLives_ReviveTime_Label);
        GrafUpdateIntTextBox(ref ArenaLives_RespawnShieldDuration_TextBox, ref ArenaLives_RespawnShieldDuration_Label);

        GrafUpdateIntTextBox(ref this.ArenaItems_ItemSpawnMultiplierCent_TextBox, ref this.ArenaItems_ItemSpawnMultiplierCent_Label);
        GrafUpdateIntTextBox(ref this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox, ref this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label);
    }

    public bool GetChecked(CheckBox box)
    {
        string id = box.IDString;
        if (BTWMeadowArenaSettings.TryGetSettings(out var settings))
        {
            if (id == AL_BLOCKWIN) return settings.ArenaLives_BlockWin;
            if (id == AL_STRICT) return settings.ArenaLives_Strict;

            if (id == AI_ITEMSPAWNDIVERSITY) return settings.ArenaItems_ItemSpawnDiversity;
            if (id == AI_ITEMSPAWNRANDOM) return settings.ArenaItems_ItemSpawnRandom; 
            if (id == AI_NEWITEMSPAWNSYSTEM) return settings.ArenaItems_NewItemSpawningSystem;
        }
        return false;
    }

    public void SetChecked(CheckBox box, bool c)
    {
        if (!BTWMeadowArenaSettings.TryGetSettings(out var settings)) return;
        string id = box.IDString;
        if (id == AL_BLOCKWIN) {settings.ArenaLives_BlockWin = c;}
        else if (id == AL_STRICT) {settings.ArenaLives_Strict = c;}

        else if (id == AI_ITEMSPAWNDIVERSITY) {settings.ArenaItems_ItemSpawnDiversity = c;}
        else if (id == AI_ITEMSPAWNRANDOM) {settings.ArenaItems_ItemSpawnRandom = c;}
        else if (id == AI_NEWITEMSPAWNSYSTEM) {settings.ArenaItems_NewItemSpawningSystem = c;}
    }
}
