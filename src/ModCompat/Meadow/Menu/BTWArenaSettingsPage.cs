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


namespace BeyondTheWest.MeadowCompat.BTWMenu;

public class BTWArenaSettingsPage : SettingsPage, CheckBox.IOwnCheckBox
{
    public const string AI_NEWITEMSPAWNSYSTEM = "BTW_AI_NSS";
    public const string AI_ITEMSPAWNDIVERSITY = "BTW_AI_ISD";
    public const string AI_ITEMSPAWNRANDOM = "BTW_AI_ISR";
    public const string AI_ITEMRESPAWN = "BTW_AI_IRS";
    public const string AI_CHECKSPEAR = "BTW_AI_CSC";
    public const string AI_CHECKTROWABLE = "BTW_AI_CTC";
    public const string AI_CHECKOTHERS = "BTW_AI_CMC";
    public const string AI_NOSPEAR = "BTW_AI_NSR";
    public const string AB_INSTANTDEATH = "BTW_AB_DIE";
    public const string AB_EXTRAITEMUSES = "BTW_AB_EIU";

    public MenuTabWrapper tabWrapper;

    public MenuLabel ArenaItems_Title, WIP_Warning;
    public OpTextBox ArenaItems_ItemSpawnMultiplierCent_TextBox,
        ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox,
        ArenaItems_ItemRespawnTimer_TextBox;
    public MenuLabel ArenaItems_ItemSpawnMultiplierCent_Label,
        ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label,
        ArenaItems_ItemRespawnTimer_Label;
    public RestorableCheckbox ArenaItems_NewItemSpawningSystem_CheckBox, 
        ArenaItems_ItemSpawnDiversity_CheckBox, 
        ArenaItems_ItemSpawnRandom_CheckBox, 
        ArenaItems_ItemRespawn_CheckBox, 
        ArenaItems_CheckSpearCount_CheckBox, 
        ArenaItems_CheckThrowableCount_CheckBox, 
        ArenaItems_CheckMiscellaneousCount_CheckBox, 
        ArenaItems_NoSpear_CheckBox, 
        ArenaBonus_InstantDeath_CheckBox, 
        ArenaBonus_ExtraItemsUses_CheckBox;

    public SimpleButton backButton;

    public override string Name => "BTW Arena Settings"; //this will appear on Select Settings Page

    public BTWArenaSettingsPage(global::Menu.Menu menu, MenuObject owner, Vector2 spacing, float textSpacing = 300) : base(menu, owner)
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
        
        //--------------------- Arena Item Spawn 
        CreateTitle(ref ArenaItems_Title, "Arena Settings", 0);

        // ArenaItems_NewItemSpawningSystem
        CreateCheckBox(ref ArenaItems_NewItemSpawningSystem_CheckBox, BTWRemix.MeadowNewItemSpawningSystem,
            "New item spawning system :", AI_NEWITEMSPAWNSYSTEM, 1);

        // ArenaItems_ItemSpawnDiversity
        CreateCheckBox(ref ArenaItems_ItemSpawnDiversity_CheckBox, BTWRemix.MeadowItemSpawnDiversity,
            "Item Diversity :", AI_ITEMSPAWNDIVERSITY, 2);

        // ArenaItems_ItemSpawnRandom
        CreateCheckBox(ref ArenaItems_ItemSpawnRandom_CheckBox, BTWRemix.MeadowItemSpawnRandom,
            "Random Items :", AI_ITEMSPAWNRANDOM, 3);

        //  ArenaItems_ItemSpawnMultiplierCent
        CreateIntTextBox(ref ArenaItems_ItemSpawnMultiplierCent_TextBox, ref ArenaItems_ItemSpawnMultiplierCent_Label,
            BTWRemix.MeadowItemSpawnMultiplierCent, "Item Spawn multiplicator % :", 4, true);
        ArenaItems_ItemSpawnMultiplierCent_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaItems_ItemSpawnMultiplierCent =
                BTWRemix.MeadowItemSpawnMultiplierCent.ClampValue(ArenaItems_ItemSpawnMultiplierCent_TextBox.valueInt);
        };

        //  ArenaItems_ItemSpawnMultiplierPerPlayersCent
        CreateIntTextBox(ref ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox, ref ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label,
            BTWRemix.MeadowItemSpawnMultiplierPerPlayersCent, "Added multiplicator per player % :", 5, true);
        ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaItems_ItemSpawnMultiplierPerPlayersCent =
                BTWRemix.MeadowItemSpawnMultiplierPerPlayersCent.ClampValue(ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox.valueInt);
        };

        // ArenaItems_ItemRespawn
        CreateCheckBox(ref ArenaItems_ItemRespawn_CheckBox, BTWRemix.MeadowItemRespawn,
            "Items Respawn :", AI_ITEMRESPAWN, 6);

        // ArenaItems_ItemRespawnTimer
        CreateIntTextBox(ref ArenaItems_ItemRespawnTimer_TextBox, ref ArenaItems_ItemRespawnTimer_Label,
            BTWRemix.MeadowItemRespawnTimer, "Item Respawn Timer :", 7, true);
        ArenaItems_ItemRespawnTimer_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.ArenaItems_ItemRespawnTimer = 
                ArenaItems_ItemRespawnTimer_TextBox.cfgEntry.ClampValue(ArenaItems_ItemRespawnTimer_TextBox.valueInt);
        };

        // ArenaItems_CheckSpearCount
        CreateCheckBox(ref ArenaItems_CheckSpearCount_CheckBox, BTWRemix.MeadowItemCheckSpearCount,
            "Check Spear Count :", AI_CHECKSPEAR, 8);

        // ArenaItems_CheckThrowableCount
        CreateCheckBox(ref ArenaItems_CheckThrowableCount_CheckBox, BTWRemix.MeadowItemCheckThrowableCount,
            "Check Throwable Count :", AI_CHECKTROWABLE, 9);

        // ArenaItems_CheckMiscellaneousCount
        CreateCheckBox(ref ArenaItems_CheckMiscellaneousCount_CheckBox, BTWRemix.MeadowItemCheckMiscellaneousCount,
            "Check Miscellaneous Count :", AI_CHECKOTHERS, 10);

        // ArenaItems_NoSpears
        CreateCheckBox(ref ArenaItems_NoSpear_CheckBox, BTWRemix.MeadowItemNoSpear,
            "No Spears :", AI_NOSPEAR, 11);

        // ArenaBonus_InstantDeath
        CreateCheckBox(ref ArenaBonus_InstantDeath_CheckBox, BTWRemix.MeadowArenaInstantDeath,
            "Instant Death :", AB_INSTANTDEATH, 12);

        // ArenaBonus_ExtraItemsUses
        CreateCheckBox(ref ArenaBonus_ExtraItemsUses_CheckBox, BTWRemix.MeadowArenaExtraItemUses,
            "Extra Item Uses :", AB_EXTRAITEMUSES, 13);


        this.SafeAddSubobjects(
            tabWrapper,
            WIP_Warning, ArenaItems_Title,

            ArenaItems_NewItemSpawningSystem_CheckBox,
            ArenaItems_ItemSpawnDiversity_CheckBox,
            ArenaItems_ItemSpawnRandom_CheckBox,
            ArenaItems_ItemRespawn_CheckBox,
            ArenaItems_CheckSpearCount_CheckBox,
            ArenaItems_CheckThrowableCount_CheckBox,
            ArenaItems_CheckMiscellaneousCount_CheckBox,
            ArenaItems_NoSpear_CheckBox,
            ArenaBonus_InstantDeath_CheckBox,
            ArenaBonus_ExtraItemsUses_CheckBox,

            ArenaItems_ItemSpawnMultiplierCent_Label,
            ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label,
            ArenaItems_ItemRespawnTimer_Label);
    }

    public void SyncMenuObjectStatus(MenuObject obj)
    {
        if (obj is CheckBox checkBox) { checkBox.Checked = checkBox.Checked; }
    }
    public override void SaveInterfaceOptions()
    {
        BTWRemix.MeadowItemSpawnMultiplierCent.Value = this.ArenaItems_ItemSpawnMultiplierCent_TextBox.valueInt;
        BTWRemix.MeadowItemSpawnMultiplierPerPlayersCent.Value = this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox.valueInt;
        BTWRemix.MeadowItemRespawnTimer.Value = this.ArenaItems_ItemRespawnTimer_TextBox.valueInt;

        BTWRemix.MeadowNewItemSpawningSystem.Value = this.ArenaItems_NewItemSpawningSystem_CheckBox.Checked;
        BTWRemix.MeadowItemSpawnDiversity.Value = this.ArenaItems_ItemSpawnDiversity_CheckBox.Checked;
        BTWRemix.MeadowItemSpawnRandom.Value = this.ArenaItems_ItemSpawnRandom_CheckBox.Checked;
        BTWRemix.MeadowItemRespawn.Value = this.ArenaItems_ItemRespawn_CheckBox.Checked;

        BTWRemix.MeadowItemCheckSpearCount.Value = this.ArenaItems_CheckSpearCount_CheckBox.Checked;
        BTWRemix.MeadowItemCheckThrowableCount.Value = this.ArenaItems_CheckThrowableCount_CheckBox.Checked;
        BTWRemix.MeadowItemCheckMiscellaneousCount.Value = this.ArenaItems_CheckMiscellaneousCount_CheckBox.Checked;
        BTWRemix.MeadowItemNoSpear.Value = this.ArenaItems_NoSpear_CheckBox.Checked;
        BTWRemix.MeadowArenaInstantDeath.Value = this.ArenaBonus_InstantDeath_CheckBox.Checked;
        BTWRemix.MeadowArenaExtraItemUses.Value = this.ArenaBonus_ExtraItemsUses_CheckBox.Checked;
        
        BTWRemix.instance._SaveConfigFile();
    }
    public override void CallForSync()
    {
        foreach (MenuObject menuObj in subObjects) { SyncMenuObjectStatus(menuObj); }
        
        if (!BTWMeadowArenaSettings.TryGetSettings(out var btwSettings)) return;

        btwSettings.ArenaItems_ItemSpawnMultiplierCent = this.ArenaItems_ItemSpawnMultiplierCent_TextBox.valueInt;
        btwSettings.ArenaItems_ItemSpawnMultiplierPerPlayersCent = this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox.valueInt;
        btwSettings.ArenaItems_ItemRespawnTimer = this.ArenaItems_ItemRespawnTimer_TextBox.valueInt;
    }
    
    public override void SelectAndCreateBackButtons(SettingsPage previousSettingPage, bool forceSelectedObject)
    {
        if (backButton == null)
        {
            backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(15, 15), new(80, 30));
            AddObjects(backButton);
            menu.MutualVerticalButtonBind(backButton, ArenaItems_NewItemSpawningSystem_CheckBox);
            menu.MutualVerticalButtonBind(ArenaBonus_InstantDeath_CheckBox, backButton); //loop
        }
        if (forceSelectedObject) menu.selectedObject = ArenaBonus_InstantDeath_CheckBox;
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

            UpdateIntTextBox(ref this.ArenaItems_ItemSpawnMultiplierCent_TextBox, settings.ArenaItems_ItemSpawnMultiplierCent);
            UpdateIntTextBox(ref this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox, settings.ArenaItems_ItemSpawnMultiplierPerPlayersCent);
            UpdateIntTextBox(ref this.ArenaItems_ItemRespawnTimer_TextBox, settings.ArenaItems_ItemRespawnTimer);
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

        GrafUpdateIntTextBox(ref this.ArenaItems_ItemSpawnMultiplierCent_TextBox, ref this.ArenaItems_ItemSpawnMultiplierCent_Label);
        GrafUpdateIntTextBox(ref this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_TextBox, ref this.ArenaItems_ItemSpawnMultiplierPerPlayersCent_Label);
        GrafUpdateIntTextBox(ref this.ArenaItems_ItemRespawnTimer_TextBox, ref this.ArenaItems_ItemRespawnTimer_Label);
    }

    public bool GetChecked(CheckBox box)
    {
        string id = box.IDString;
        if (BTWMeadowArenaSettings.TryGetSettings(out var settings))
        {
            if (id == AI_ITEMSPAWNDIVERSITY) return settings.ArenaItems_ItemSpawnDiversity;
            if (id == AI_ITEMSPAWNRANDOM) return settings.ArenaItems_ItemSpawnRandom; 
            if (id == AI_NEWITEMSPAWNSYSTEM) return settings.ArenaItems_NewItemSpawningSystem;
            if (id == AI_ITEMRESPAWN) return settings.ArenaItems_ItemRespawn;
            if (id == AI_CHECKSPEAR) return settings.ArenaItems_CheckSpearCount;
            if (id == AI_CHECKTROWABLE) return settings.ArenaItems_CheckThrowableCount;
            if (id == AI_CHECKOTHERS) return settings.ArenaItems_CheckMiscellaneousCount;
            if (id == AI_NOSPEAR) return settings.ArenaItems_NoSpear;
            if (id == AB_INSTANTDEATH) return settings.ArenaBonus_InstantDeath;
            if (id == AB_EXTRAITEMUSES) return settings.ArenaBonus_ExtraItemUses;
        }
        return false;
    }

    public void SetChecked(CheckBox box, bool c)
    {
        if (!BTWMeadowArenaSettings.TryGetSettings(out var settings)) return;
        string id = box.IDString;

        if (id == AI_ITEMSPAWNDIVERSITY) {settings.ArenaItems_ItemSpawnDiversity = c;}
        else if (id == AI_ITEMSPAWNRANDOM) {settings.ArenaItems_ItemSpawnRandom = c;}
        else if (id == AI_NEWITEMSPAWNSYSTEM) {settings.ArenaItems_NewItemSpawningSystem = c;}
        else if (id == AI_ITEMRESPAWN) {settings.ArenaItems_ItemRespawn = c;}
        else if (id == AI_CHECKSPEAR) {settings.ArenaItems_CheckSpearCount = c;}
        else if (id == AI_CHECKTROWABLE) {settings.ArenaItems_CheckThrowableCount = c;}
        else if (id == AI_CHECKOTHERS) {settings.ArenaItems_CheckMiscellaneousCount = c;}
        else if (id == AI_NOSPEAR) {settings.ArenaItems_NoSpear = c;}
        else if (id == AB_INSTANTDEATH) {settings.ArenaBonus_InstantDeath = c;}
        else if (id == AB_EXTRAITEMUSES) {settings.ArenaBonus_ExtraItemUses = c;}
    }
}
