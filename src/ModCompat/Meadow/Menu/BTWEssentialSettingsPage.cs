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
public class BTWEssentialSettingsPage : SettingsPage, CheckBox.IOwnCheckBox
{
    public const string TS_EVERYONECANPOLETECH = "BTW_TR_EPT";
    public const string CO_SHOCKWAVE = "BTW_CO_SHW";
    public const string SP_DODISCHARGEDAMAGE = "BTW_SP_DDD";
    public const string SP_RISKYOVERCHARGE = "BTW_SP_ROC";
    public const string SP_DEADLYOVERCHARGE = "BTW_SP_DOC";
    public const string RESETTODEFAULT = "BTW_RESETTODEFAULT";
    public MenuTabWrapper tabWrapper;

    public MenuLabel TrailSeeker_Title, Core_Title, Spark_Title;
    public OpTextBox Trailseeker_WallGripTimer_TextBox,
        Core_MaxLeap_TextBox,
        Spark_MaxElectricBounce_TextBox;
    public MenuLabel Trailseeker_WallGripTimer_Label,
        Core_MaxLeap_Label, 
        Spark_MaxElectricBounce_Label;
    public RestorableCheckbox TrailSeeker_EveryoneCanPoleTech_CheckBox,
        Core_Shockwave_CheckBox, 
        Spark_DoDischargeDamage_CheckBox, Spark_RiskyOvercharge_CheckBox, Spark_DeadlyOvercharge_CheckBox;

    public SimpleButton backButton, resetButton;

    public override string Name => "BTW Essential Settings"; //this will appear on Select Settings Page

    public BTWEssentialSettingsPage(global::Menu.Menu menu, MenuObject owner, Vector2 spacing, float textSpacing = 300) : base(menu, owner)
    {
        tabWrapper = new(menu, this);
        Vector2 positioner = new(360 + ((textSpacing - 300) / 2), 400);
        
        void CreateIntTextBox(ref OpTextBox textBox, ref MenuLabel label, Configurable<int> setting, string settingName, int place)
        { // got lazy, automatized it
            textBox = new OpTextBox(
                setting,
                positioner - spacing * place + new Vector2(-7.5f, 0),
                40)
            {
                alignment = FLabelAlignment.Center,
                description = setting.info.description
            };
            label = new(this.menu, this, settingName, textBox.pos + new Vector2(-textSpacing * 1.5f + 7.5f, 3), new(textSpacing, 20), false);
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

        //--------------------- TrailSeeker
        CreateTitle(ref TrailSeeker_Title, "The Trailseeker", 0);

        // AlteredMovementTech
        CreateCheckBox(ref TrailSeeker_EveryoneCanPoleTech_CheckBox, BTWRemix.MeadowEveryoneCanPoleTech,
            "New Pole Tech to everyone:", TS_EVERYONECANPOLETECH, 1);

        // WallGripTimer
        CreateIntTextBox(ref Trailseeker_WallGripTimer_TextBox, ref Trailseeker_WallGripTimer_Label,
            BTWRemix.MeadowTrailseekerWallGripTimer, "Wall Grip Timer:", 2);

        Trailseeker_WallGripTimer_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Trailseeker_WallGripTimer =
                BTWRemix.MeadowTrailseekerWallGripTimer.ClampValue(Trailseeker_WallGripTimer_TextBox.valueInt);
        };
        
        //--------------------- Core
        CreateTitle(ref Core_Title, "The Core", 3);

        // MaxLeap
        CreateIntTextBox(ref Core_MaxLeap_TextBox, ref Core_MaxLeap_Label,
            BTWRemix.MeadowCoreMaxLeap, "Mid-air Leaps:", 4);

        Core_MaxLeap_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Core_MaxLeap =
                BTWRemix.MeadowCoreMaxLeap.ClampValue(Core_MaxLeap_TextBox.valueInt);
        };

        // Shockwave
        CreateCheckBox(ref Core_Shockwave_CheckBox, BTWRemix.MeadowCoreShockwave,
            "Shockwave Enabled:", CO_SHOCKWAVE, 5);
        
        //--------------------- Spark
        CreateTitle(ref Spark_Title, "The Spark", 6);

        // MaxElectricBounce
        CreateIntTextBox(ref Spark_MaxElectricBounce_TextBox, ref Spark_MaxElectricBounce_Label,
            BTWRemix.MeadowSparkMaxElectricBounce, "Electric Bounces:", 7);

        Spark_MaxElectricBounce_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Spark_MaxElectricBounce =
                BTWRemix.MeadowSparkMaxElectricBounce.ClampValue(Spark_MaxElectricBounce_TextBox.valueInt);
        };

        // DoDischargeDamage
        CreateCheckBox(ref Spark_DoDischargeDamage_CheckBox, BTWRemix.MeadowSparkDoDischargeDamage,
            "Enable Discharge Damage:", SP_DODISCHARGEDAMAGE, 8);

        // RiskyOvercharge
        CreateCheckBox(ref Spark_RiskyOvercharge_CheckBox, BTWRemix.MeadowSparkRiskyOvercharge,
            "Risky Overcharge:", SP_RISKYOVERCHARGE, 9);

        // DeadlyOvercharge
        CreateCheckBox(ref Spark_DeadlyOvercharge_CheckBox, BTWRemix.MeadowSparkDeadlyOvercharge,
            "Deadly Surcharge:", SP_DEADLYOVERCHARGE, 10);


        this.SafeAddSubobjects(
            tabWrapper,
            TrailSeeker_Title, Core_Title, Spark_Title,
            TrailSeeker_EveryoneCanPoleTech_CheckBox,
            Core_Shockwave_CheckBox,
            Spark_DoDischargeDamage_CheckBox, Spark_RiskyOvercharge_CheckBox, Spark_DeadlyOvercharge_CheckBox,
            Trailseeker_WallGripTimer_Label,
            Core_MaxLeap_Label,
            Spark_MaxElectricBounce_Label);
    }

    public void SyncMenuObjectStatus(MenuObject obj)
    {
        if (obj is CheckBox checkBox) { checkBox.Checked = checkBox.Checked; }
    }
    public override void SaveInterfaceOptions()
    {
        BTWRemix.MeadowEveryoneCanPoleTech.Value = this.TrailSeeker_EveryoneCanPoleTech_CheckBox.Checked;
        BTWRemix.MeadowTrailseekerWallGripTimer.Value = this.Trailseeker_WallGripTimer_TextBox.valueInt;

        BTWRemix.MeadowCoreMaxLeap.Value = this.Core_MaxLeap_TextBox.valueInt;
        BTWRemix.MeadowCoreShockwave.Value = this.Core_Shockwave_CheckBox.Checked;

        BTWRemix.MeadowSparkMaxElectricBounce.Value = this.Spark_MaxElectricBounce_TextBox.valueInt;
        BTWRemix.MeadowSparkDoDischargeDamage.Value = this.Spark_DoDischargeDamage_CheckBox.Checked;
        BTWRemix.MeadowSparkRiskyOvercharge.Value = this.Spark_RiskyOvercharge_CheckBox.Checked;
        BTWRemix.MeadowSparkDeadlyOvercharge.Value = this.Spark_DeadlyOvercharge_CheckBox.Checked;
        
        BTWRemix.instance._SaveConfigFile();
    }
    public override void CallForSync()
    {
        foreach (MenuObject menuObj in subObjects) { SyncMenuObjectStatus(menuObj); }
        
        if (!BTWMeadowArenaSettings.TryGetSettings(out var btwSettings)) return;
        btwSettings.Trailseeker_WallGripTimer = this.Trailseeker_WallGripTimer_TextBox.valueInt;

        btwSettings.Core_MaxLeap = this.Core_MaxLeap_TextBox.valueInt;

        btwSettings.Spark_MaxElectricBounce = this.Spark_MaxElectricBounce_TextBox.valueInt;
    }
    
    public override void SelectAndCreateBackButtons(SettingsPage previousSettingPage, bool forceSelectedObject)
    {
        if (backButton == null)
        {
            backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(15, 15), new(80, 30));
            AddObjects(backButton);
            menu.MutualVerticalButtonBind(backButton, Spark_DeadlyOvercharge_CheckBox);
        }
        if (forceSelectedObject) menu.selectedObject = TrailSeeker_EveryoneCanPoleTech_CheckBox;
        if (resetButton == null)
        {
            resetButton = new(menu, this, menu.Translate("RESET"), RESETTODEFAULT, new(355, 15), new(80, 30));
            AddObjects(resetButton);
            menu.MutualVerticalButtonBind(resetButton, backButton); 
            menu.MutualVerticalButtonBind(TrailSeeker_EveryoneCanPoleTech_CheckBox, resetButton); //loop
        }
    }
    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        if (message == RESETTODEFAULT)
        {
            (RWCustom.Custom.rainWorld.processManager.currentMainLoop as Menu.Menu)?.PlaySound(SoundID.MENU_Button_Successfully_Assigned); 
            this.TrailSeeker_EveryoneCanPoleTech_CheckBox.Checked = ValueConverter.ConvertToValue<bool>(BTWRemix.MeadowEveryoneCanPoleTech.defaultValue);
            this.Trailseeker_WallGripTimer_TextBox.valueInt = ValueConverter.ConvertToValue<int>(BTWRemix.MeadowTrailseekerWallGripTimer.defaultValue);
            
            this.Core_MaxLeap_TextBox.valueInt = ValueConverter.ConvertToValue<int>(BTWRemix.MeadowCoreMaxLeap.defaultValue);
            this.Core_Shockwave_CheckBox.Checked = ValueConverter.ConvertToValue<bool>(BTWRemix.MeadowCoreShockwave.defaultValue);
            
            this.Spark_MaxElectricBounce_TextBox.valueInt = ValueConverter.ConvertToValue<int>(BTWRemix.MeadowSparkMaxElectricBounce.defaultValue);
            this.Spark_DoDischargeDamage_CheckBox.Checked = ValueConverter.ConvertToValue<bool>(BTWRemix.MeadowSparkDoDischargeDamage.defaultValue);
            this.Spark_RiskyOvercharge_CheckBox.Checked = ValueConverter.ConvertToValue<bool>(BTWRemix.MeadowSparkRiskyOvercharge.defaultValue);
            this.Spark_DeadlyOvercharge_CheckBox.Checked = ValueConverter.ConvertToValue<bool>(BTWRemix.MeadowSparkDeadlyOvercharge.defaultValue);
        }
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
            UpdateIntTextBox(ref Trailseeker_WallGripTimer_TextBox, settings.Trailseeker_WallGripTimer);

            UpdateIntTextBox(ref Core_MaxLeap_TextBox, settings.Core_MaxLeap);

            UpdateIntTextBox(ref Spark_MaxElectricBounce_TextBox, settings.Spark_MaxElectricBounce);
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
        GrafUpdateIntTextBox(ref Trailseeker_WallGripTimer_TextBox, ref Trailseeker_WallGripTimer_Label);

        GrafUpdateIntTextBox(ref Core_MaxLeap_TextBox, ref Core_MaxLeap_Label);
        
        GrafUpdateIntTextBox(ref Spark_MaxElectricBounce_TextBox, ref Spark_MaxElectricBounce_Label);
    }

    public bool GetChecked(CheckBox box)
    {
        string id = box.IDString;
        if (BTWMeadowArenaSettings.TryGetSettings(out var settings))
        {
            if (id == TS_EVERYONECANPOLETECH) return settings.Trailseeker_EveryoneCanPoleTech;
            if (id == CO_SHOCKWAVE) return settings.Core_Shockwave;
            if (id == SP_DODISCHARGEDAMAGE) return settings.Spark_DoDischargeDamage;
            if (id == SP_RISKYOVERCHARGE) return settings.Spark_RiskyOvercharge;
            if (id == SP_DEADLYOVERCHARGE) return settings.Spark_DeadlyOvercharge;
        }
        return false;
    }

    public void SetChecked(CheckBox box, bool c)
    {
        if (!BTWMeadowArenaSettings.TryGetSettings(out var settings)) return;
        string id = box.IDString;
        if (id == TS_EVERYONECANPOLETECH) {settings.Trailseeker_EveryoneCanPoleTech = c;}
        else if (id == CO_SHOCKWAVE) {settings.Core_Shockwave = c;}
        else if (id == SP_DODISCHARGEDAMAGE) {settings.Spark_DoDischargeDamage = c;}
        else if (id == SP_RISKYOVERCHARGE) {settings.Spark_RiskyOvercharge = c;}
        else if (id == SP_DEADLYOVERCHARGE) {settings.Spark_DeadlyOvercharge = c;}
    }
}
