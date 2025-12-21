using UnityEngine;
using System;
using RainMeadow;
using BeyondTheWest;

using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;


namespace BeyondTheWest.MeadowCompat.ArenaMenu;
public class BTWAdditionalSettingsPage : SettingsPage
{
    public MenuTabWrapper tabWrapper;

    public MenuLabel TrailSeeker_Title, Core_Title, Spark_Title, WIP1_Warning, WIP2_Warning;
    public OpTextBox TrailSeeker_PoleClimbBonus_TextBox, Trailseeker_MaxWallClimb_TextBox,
        Core_MaxEnergy_TextBox, Core_OxygenEnergyUsage_TextBox, Core_RegenEnergy_TextBox, Core_AntiGravityCent_TextBox,
        Spark_AdditionnalOvercharge_TextBox, Spark_ChargeRegenerationMult_TextBox, Spark_MaxCharge_TextBox;
    public MenuLabel TrailSeeker_PoleClimbBonus_Label, Trailseeker_MaxWallClimb_Label,
        Core_MaxEnergy_Label, Core_OxygenEnergyUsage_Label, Core_RegenEnergy_Label, Core_AntiGravityCent_Label,
        Spark_AdditionnalOvercharge_Label, Spark_ChargeRegenerationMult_Label, Spark_MaxCharge_Label;
    // public RestorableCheckbox ;

    public SimpleButton backButton;

    public override string Name => "BTW Additional Settings"; 

    public BTWAdditionalSettingsPage(Menu.Menu menu, MenuObject owner, Vector2 spacing, float textSpacing = 300) : base(menu, owner)
    {
        tabWrapper = new(menu, this);
        Vector2 positioner = new(360 + ((textSpacing - 300) / 2), 380);

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

        CreateWarning(ref WIP1_Warning, "Those parameter are for fine tune balancing.", -1.5f);
        CreateWarning(ref WIP2_Warning, "Putting them to the extreme may break the slugcat :<", -1f);

        //--------------------- TrailSeeker
        CreateTitle(ref TrailSeeker_Title, "The Trailseeker", 0);

        // PoleClimbBonus
        CreateIntTextBox(ref TrailSeeker_PoleClimbBonus_TextBox, ref TrailSeeker_PoleClimbBonus_Label,
            BTWRemix.MeadowTrailseekerPoleClimbBonus, "Pole Climb Bonus:", 1);

        TrailSeeker_PoleClimbBonus_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Trailseeker_PoleClimbBonus =
                BTWRemix.MeadowTrailseekerPoleClimbBonus.ClampValue(TrailSeeker_PoleClimbBonus_TextBox.valueInt);
        };

        // MaxWallClimb
        CreateIntTextBox(ref Trailseeker_MaxWallClimb_TextBox, ref Trailseeker_MaxWallClimb_Label,
            BTWRemix.MeadowTrailseekerMaxWallClimb, "Wall Climbs:", 2);

        Trailseeker_MaxWallClimb_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Trailseeker_MaxWallClimb =
                BTWRemix.MeadowTrailseekerMaxWallClimb.ClampValue(Trailseeker_MaxWallClimb_TextBox.valueInt);
        };

        //--------------------- Core
        CreateTitle(ref Core_Title, "The Core", 3);

        // MaxEnergy
        CreateIntTextBox(ref Core_MaxEnergy_TextBox, ref Core_MaxEnergy_Label,
            BTWRemix.MeadowCoreMaxEnergy, "Max Energy:", 4, true);

        Core_MaxEnergy_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Core_MaxEnergy =
                BTWRemix.MeadowCoreMaxEnergy.ClampValue(Core_MaxEnergy_TextBox.valueInt);
        };

        // RegenEnergy
        CreateIntTextBox(ref Core_RegenEnergy_TextBox, ref Core_RegenEnergy_Label,
            BTWRemix.MeadowCoreRegenEnergy, "Energy Regeneration:", 5);

        Core_RegenEnergy_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Core_RegenEnergy =
                BTWRemix.MeadowCoreRegenEnergy.ClampValue(Core_RegenEnergy_TextBox.valueInt);
        };

        // OxygenEnergyUsage
        CreateIntTextBox(ref Core_OxygenEnergyUsage_TextBox, ref Core_OxygenEnergyUsage_Label,
            BTWRemix.MeadowCoreOxygenEnergyUsage, "Underwater Breathing Energy Usage:", 6, true);

        Core_OxygenEnergyUsage_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Core_OxygenEnergyUsage =
                BTWRemix.MeadowCoreOxygenEnergyUsage.ClampValue(Core_OxygenEnergyUsage_TextBox.valueInt);
        };

        // AntiGravityCent
        CreateIntTextBox(ref Core_AntiGravityCent_TextBox, ref Core_AntiGravityCent_Label,
            BTWRemix.MeadowCoreAntiGravityCent, "Anti-gravity Pourcent:", 7);

        Core_AntiGravityCent_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Core_AntiGravityCent =
                BTWRemix.MeadowCoreAntiGravityCent.ClampValue(Core_AntiGravityCent_TextBox.valueInt);
        };

        //--------------------- Spark
        CreateTitle(ref Spark_Title, "The Spark", 8);

        // MaxCharge
        CreateIntTextBox(ref Spark_MaxCharge_TextBox, ref Spark_MaxCharge_Label,
            BTWRemix.MeadowSparkMaxCharge, "Max Charge:", 9, true);

        Spark_MaxCharge_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Spark_MaxCharge =
                BTWRemix.MeadowSparkMaxCharge.ClampValue(Spark_MaxCharge_TextBox.valueInt);

            if (btwAData.Spark_MaxCharge <= 0 && btwAData.Spark_AdditionnalOvercharge <= 0)
            {
                btwAData.Spark_AdditionnalOvercharge = 1;
                Spark_AdditionnalOvercharge_TextBox.valueInt = 1;
            }
        };

        // AdditionnalOvercharge
        CreateIntTextBox(ref Spark_AdditionnalOvercharge_TextBox, ref Spark_AdditionnalOvercharge_Label,
            BTWRemix.MeadowSparkAdditionnalOvercharge, "Additional Overcharge:", 10, true);

        Spark_AdditionnalOvercharge_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Spark_AdditionnalOvercharge =
                BTWRemix.MeadowSparkAdditionnalOvercharge.ClampValue(Spark_AdditionnalOvercharge_TextBox.valueInt);
                
            if (btwAData.Spark_MaxCharge <= 0 && btwAData.Spark_AdditionnalOvercharge <= 0)
            {
                btwAData.Spark_MaxCharge = 1;
                Spark_MaxCharge_TextBox.valueInt = 1;
            }
        };

        // ChargeRegenerationMult
        CreateIntTextBox(ref Spark_ChargeRegenerationMult_TextBox, ref Spark_ChargeRegenerationMult_Label,
            BTWRemix.MeadowSparkChargeRegenerationMult, "Recharge multiplier:", 11);

        Spark_ChargeRegenerationMult_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
        {
            if (!BTWMeadowArenaSettings.TryGetSettings(out var btwAData)) return;
            btwAData.Spark_ChargeRegenerationMult =
                BTWRemix.MeadowSparkChargeRegenerationMult.ClampValue(Spark_ChargeRegenerationMult_TextBox.valueInt);
        };


        this.SafeAddSubobjects(
            tabWrapper,
            TrailSeeker_Title, Core_Title, Spark_Title, WIP1_Warning, WIP2_Warning,
            TrailSeeker_PoleClimbBonus_Label, Trailseeker_MaxWallClimb_Label,
            Core_MaxEnergy_Label, Core_OxygenEnergyUsage_Label, Core_RegenEnergy_Label, Core_AntiGravityCent_Label,
            Spark_AdditionnalOvercharge_Label, Spark_ChargeRegenerationMult_Label, Spark_MaxCharge_Label);
    }

    public void SyncMenuObjectStatus(MenuObject obj)
    {
        if (obj is CheckBox checkBox) { checkBox.Checked = checkBox.Checked; }
    }
    public override void SaveInterfaceOptions()
    {
        BTWRemix.MeadowTrailseekerPoleClimbBonus.Value = this.TrailSeeker_PoleClimbBonus_TextBox.valueInt;
        BTWRemix.MeadowTrailseekerMaxWallClimb.Value = this.Trailseeker_MaxWallClimb_TextBox.valueInt;

        BTWRemix.MeadowCoreMaxEnergy.Value = this.Core_MaxEnergy_TextBox.valueInt;
        BTWRemix.MeadowCoreOxygenEnergyUsage.Value = this.Core_OxygenEnergyUsage_TextBox.valueInt;
        BTWRemix.MeadowCoreRegenEnergy.Value = this.Core_RegenEnergy_TextBox.valueInt;
        BTWRemix.MeadowCoreAntiGravityCent.Value = this.Core_AntiGravityCent_TextBox.valueInt;

        BTWRemix.MeadowSparkAdditionnalOvercharge.Value = this.Spark_AdditionnalOvercharge_TextBox.valueInt;
        BTWRemix.MeadowSparkChargeRegenerationMult.Value = this.Spark_ChargeRegenerationMult_TextBox.valueInt;
        BTWRemix.MeadowSparkMaxCharge.Value = this.Spark_MaxCharge_TextBox.valueInt;
    }
    public override void CallForSync()
    {
        foreach (MenuObject menuObj in subObjects) { SyncMenuObjectStatus(menuObj); }

        if (!BTWMeadowArenaSettings.TryGetSettings(out var btwSettings)) return;
        btwSettings.Trailseeker_PoleClimbBonus = TrailSeeker_PoleClimbBonus_TextBox.valueInt;
        btwSettings.Trailseeker_MaxWallClimb = this.Trailseeker_MaxWallClimb_TextBox.valueInt;

        btwSettings.Core_MaxEnergy = this.Core_MaxEnergy_TextBox.valueInt;
        btwSettings.Core_OxygenEnergyUsage = this.Core_OxygenEnergyUsage_TextBox.valueInt;
        btwSettings.Core_RegenEnergy = this.Core_RegenEnergy_TextBox.valueInt;
        btwSettings.Core_AntiGravityCent = this.Core_AntiGravityCent_TextBox.valueInt;

        btwSettings.Spark_AdditionnalOvercharge = this.Spark_AdditionnalOvercharge_TextBox.valueInt;
        btwSettings.Spark_ChargeRegenerationMult = this.Spark_ChargeRegenerationMult_TextBox.valueInt;
        btwSettings.Spark_MaxCharge = this.Spark_MaxCharge_TextBox.valueInt;
    }

    public override void SelectAndCreateBackButtons(SettingsPage previousSettingPage, bool forceSelectedObject)
    {
        if (backButton == null)
        {
            backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(15, 15), new(80, 30));
            AddObjects(backButton);
            menu.MutualVerticalButtonBind(backButton, TrailSeeker_PoleClimbBonus_Label);
            menu.MutualVerticalButtonBind(Spark_ChargeRegenerationMult_Label, backButton); //loop
        }
        if (forceSelectedObject) menu.selectedObject = TrailSeeker_PoleClimbBonus_Label;
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
            void UpdateIntTextBox(ref OpTextBox textBox, int value)
            { // oops, it's all functions
                textBox.greyedOut = greyoutAll;
                textBox.held = textBox._KeyboardOn;
                if (!textBox.held) { textBox.valueInt = value; }
            }
            UpdateIntTextBox(ref TrailSeeker_PoleClimbBonus_TextBox, settings.Trailseeker_PoleClimbBonus);
            UpdateIntTextBox(ref Trailseeker_MaxWallClimb_TextBox, settings.Trailseeker_MaxWallClimb);

            UpdateIntTextBox(ref Core_MaxEnergy_TextBox, settings.Core_MaxEnergy);
            UpdateIntTextBox(ref Core_OxygenEnergyUsage_TextBox, settings.Core_OxygenEnergyUsage);
            UpdateIntTextBox(ref Core_RegenEnergy_TextBox, settings.Core_RegenEnergy);
            UpdateIntTextBox(ref Core_AntiGravityCent_TextBox, settings.Core_AntiGravityCent);

            UpdateIntTextBox(ref Spark_AdditionnalOvercharge_TextBox, settings.Spark_AdditionnalOvercharge);
            UpdateIntTextBox(ref Spark_ChargeRegenerationMult_TextBox, settings.Spark_ChargeRegenerationMult);
            UpdateIntTextBox(ref Spark_MaxCharge_TextBox, settings.Spark_MaxCharge);
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
        GrafUpdateIntTextBox(ref TrailSeeker_PoleClimbBonus_TextBox, ref TrailSeeker_PoleClimbBonus_Label);
        GrafUpdateIntTextBox(ref Trailseeker_MaxWallClimb_TextBox, ref Trailseeker_MaxWallClimb_Label);

        GrafUpdateIntTextBox(ref Core_MaxEnergy_TextBox, ref Core_MaxEnergy_Label);
        GrafUpdateIntTextBox(ref Core_OxygenEnergyUsage_TextBox, ref Core_OxygenEnergyUsage_Label);
        GrafUpdateIntTextBox(ref Core_RegenEnergy_TextBox, ref Core_RegenEnergy_Label);
        GrafUpdateIntTextBox(ref Core_AntiGravityCent_TextBox, ref Core_AntiGravityCent_Label);

        GrafUpdateIntTextBox(ref Spark_AdditionnalOvercharge_TextBox, ref Spark_AdditionnalOvercharge_Label);
        GrafUpdateIntTextBox(ref Spark_ChargeRegenerationMult_TextBox, ref Spark_ChargeRegenerationMult_Label);
        GrafUpdateIntTextBox(ref Spark_MaxCharge_TextBox, ref Spark_MaxCharge_Label);
    }
}