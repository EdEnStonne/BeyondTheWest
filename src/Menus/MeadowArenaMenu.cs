using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using RainMeadow;
using BeyondTheWest;

using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using RainMeadow.UI.Components.Patched;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using MonoMod.RuntimeDetour;
using System.Reflection;
using RainMeadow.UI;

public class MeadowBTWArenaMenu
{
    public static ConditionalWeakTable<ArenaMode, BTWMeadowArenaSettings> cwtMeadowArenaData = new();

    public class BTWMeadowArenaSettings
    {
        public BTWMeadowArenaSettings(ArenaMode arena) { this.arena = arena; }
        public ArenaMode arena;
        // TrailSeeker
        public bool Trailseeker_AlteredMovementTech = BTWRemix.MeadowTrailseekerAlteredMovementTech.Value;
        public int Trailseeker_PoleClimbBonus = BTWRemix.MeadowTrailseekerPoleClimbBonus.Value;
        public int Trailseeker_MaxWallClimb = BTWRemix.MeadowTrailseekerMaxWallClimb.Value;
        public int Trailseeker_WallGripTimer = BTWRemix.MeadowTrailseekerWallGripTimer.Value;

        // Core
        public int Core_RegenEnergy = BTWRemix.MeadowCoreRegenEnergy.Value;
        public int Core_MaxEnergy = BTWRemix.MeadowCoreMaxEnergy.Value;
        public int Core_MaxLeap = BTWRemix.MeadowCoreMaxLeap.Value;
        public int Core_AntiGravityCent = BTWRemix.MeadowCoreAntiGravityCent.Value;
        public int Core_OxygenEnergyUsage = BTWRemix.MeadowCoreOxygenEnergyUsage.Value;
        public bool Core_Shockwave = BTWRemix.MeadowCoreShockwave.Value;
        
        // Spark
        public int Spark_MaxCharge = BTWRemix.MeadowSparkMaxCharge.Value;
        public int Spark_AdditionnalOvercharge = BTWRemix.MeadowSparkAdditionnalOvercharge.Value;
        public int Spark_ChargeRegenerationMult = BTWRemix.MeadowSparkChargeRegenerationMult.Value;
        public int Spark_MaxElectricBounce = BTWRemix.MeadowSparkMaxElectricBounce.Value;
        public bool Spark_DoDischargeDamage = BTWRemix.MeadowSparkDoDischargeDamage.Value;
        public bool Spark_RiskyOvercharge = BTWRemix.MeadowSparkRiskyOvercharge.Value;
        public bool Spark_DeadlyOvercharge = BTWRemix.MeadowSparkDeadlyOvercharge.Value;
        
        // Arena Lives
        public int ArenaLives_AdditionalReviveTime = BTWRemix.MeadowArenaLivesAdditionalReviveTime.Value;
        public int ArenaLives_Amount = BTWRemix.MeadowArenaLivesAmount.Value;
        public int ArenaLives_ReviveTime = BTWRemix.MeadowArenaLivesReviveTime.Value;
        public bool ArenaLives_BlockWin = BTWRemix.MeadowArenaLivesBlockWin.Value;
        public bool ArenaLives_ReviveFromAbyss = false; //BTWRemix.MeadowArenaLivesReviveFromAbyss.Value;
        public bool ArenaLives_Strict = BTWRemix.MeadowArenaLivesStrict.Value;
        public int ArenaLives_RespawnShieldDuration = BTWRemix.MeadowArenaLivesRespawnShieldDuration.Value;
    }
    internal class BTWArenaLobbyRessourceData : OnlineResource.ResourceData
    {
        public BTWArenaLobbyRessourceData() { }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(GetBTWArenaSettings(), resource);
        }

        internal class State : ResourceDataState
        {
            // Group: Trailseeker
            [OnlineField(group = "Trailseeker")]
            public bool TS_AlteredMovementTech;
            [OnlineField(group = "Trailseeker")]
            public int TS_PoleClimbBonus;
            [OnlineField(group = "Trailseeker")]
            public int TS_MaxWallClimb;
            [OnlineField(group = "Trailseeker")]
            public int TS_WallGripTimer;
            
            // Group: Core
            [OnlineField(group = "Core")]
            public int CO_RegenEnergy;
            [OnlineField(group = "Core")]
            public int CO_MaxEnergy;
            [OnlineField(group = "Core")]
            public int CO_MaxLeap;
            [OnlineField(group = "Core")]
            public int CO_AntiGravityCent;
            [OnlineField(group = "Core")]
            public int CO_OxygenEnergyUsage;
            [OnlineField(group = "Core")]
            public bool CO_Shockwave;

            // Group: Spark
            [OnlineField(group = "Spark")]            
            public int SP_AdditionnalOvercharge;
            [OnlineField(group = "Spark")]
            public int SP_ChargeRegenerationMult;
            [OnlineField(group = "Spark")]
            public int SP_MaxElectricBounce;
            [OnlineField(group = "Spark")]
            public bool SP_DoDischargeDamage;
            [OnlineField(group = "Spark")]
            public bool SP_RiskyOvercharge;
            [OnlineField(group = "Spark")]
            public bool SP_DeadlyOvercharge;
            [OnlineField(group = "Spark")]
            public int SP_MaxCharge;
            
            // Group: ArenaLives
            [OnlineField(group = "ArenaLives")]
            public int AL_AdditionalReviveTime;
            [OnlineField(group = "ArenaLives")]
            public int AL_Amount;
            [OnlineField(group = "ArenaLives")]
            public int AL_ReviveTime;
            [OnlineField(group = "ArenaLives")]
            public bool AL_BlockWin;
            [OnlineField(group = "ArenaLives")]
            public bool AL_ReviveFromAbyss;
            [OnlineField(group = "ArenaLives")]
            public bool AL_Strict;
            [OnlineField(group = "ArenaLives")]
            public int AL_RespawnShieldDuration;
            
            

            public State() { }
            public State(BTWMeadowArenaSettings BTWMeadowArenaSettings, OnlineResource onlineResource)
            {
                this.TS_AlteredMovementTech = BTWMeadowArenaSettings.Trailseeker_AlteredMovementTech;
                this.TS_PoleClimbBonus = BTWMeadowArenaSettings.Trailseeker_PoleClimbBonus;
                this.TS_MaxWallClimb = BTWMeadowArenaSettings.Trailseeker_MaxWallClimb;
                this.TS_WallGripTimer = BTWMeadowArenaSettings.Trailseeker_WallGripTimer;

                this.CO_RegenEnergy = BTWMeadowArenaSettings.Core_RegenEnergy;
                this.CO_OxygenEnergyUsage = BTWMeadowArenaSettings.Core_OxygenEnergyUsage;
                this.CO_MaxEnergy = BTWMeadowArenaSettings.Core_MaxEnergy;
                this.CO_MaxLeap = BTWMeadowArenaSettings.Core_MaxLeap;
                this.CO_AntiGravityCent = BTWMeadowArenaSettings.Core_AntiGravityCent;
                this.CO_Shockwave = BTWMeadowArenaSettings.Core_Shockwave;

                this.SP_AdditionnalOvercharge = BTWMeadowArenaSettings.Spark_AdditionnalOvercharge;
                this.SP_ChargeRegenerationMult = BTWMeadowArenaSettings.Spark_ChargeRegenerationMult;
                this.SP_DoDischargeDamage = BTWMeadowArenaSettings.Spark_DoDischargeDamage;
                this.SP_MaxElectricBounce = BTWMeadowArenaSettings.Spark_MaxElectricBounce;
                this.SP_MaxCharge = BTWMeadowArenaSettings.Spark_MaxCharge;
                this.SP_RiskyOvercharge = BTWMeadowArenaSettings.Spark_RiskyOvercharge;
                this.SP_DeadlyOvercharge = BTWMeadowArenaSettings.Spark_DeadlyOvercharge;
                
                this.AL_AdditionalReviveTime = BTWMeadowArenaSettings.ArenaLives_AdditionalReviveTime;
                this.AL_Amount = BTWMeadowArenaSettings.ArenaLives_Amount;
                this.AL_BlockWin = BTWMeadowArenaSettings.ArenaLives_BlockWin;
                this.AL_ReviveFromAbyss = BTWMeadowArenaSettings.ArenaLives_ReviveFromAbyss;
                this.AL_ReviveTime = BTWMeadowArenaSettings.ArenaLives_ReviveTime;
                this.AL_Strict = BTWMeadowArenaSettings.ArenaLives_Strict;
                this.AL_RespawnShieldDuration = BTWMeadowArenaSettings.ArenaLives_RespawnShieldDuration;
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                BTWMeadowArenaSettings btwData = GetBTWArenaSettings();
                if (btwData != null)
                {
                    btwData.Trailseeker_AlteredMovementTech = this.TS_AlteredMovementTech;
                    btwData.Trailseeker_PoleClimbBonus = this.TS_PoleClimbBonus;
                    btwData.Trailseeker_MaxWallClimb = this.TS_MaxWallClimb;
                    btwData.Trailseeker_WallGripTimer = this.TS_WallGripTimer;

                    btwData.Core_MaxEnergy = this.CO_MaxEnergy;
                    btwData.Core_OxygenEnergyUsage = this.CO_OxygenEnergyUsage;
                    btwData.Core_RegenEnergy = this.CO_RegenEnergy;
                    btwData.Core_AntiGravityCent = this.CO_AntiGravityCent;
                    btwData.Core_MaxLeap = this.CO_MaxLeap;
                    btwData.Core_Shockwave = this.CO_Shockwave;

                    btwData.Spark_AdditionnalOvercharge = this.SP_AdditionnalOvercharge;
                    btwData.Spark_ChargeRegenerationMult = this.SP_ChargeRegenerationMult;
                    btwData.Spark_DoDischargeDamage = this.SP_DoDischargeDamage;
                    btwData.Spark_MaxElectricBounce = this.SP_MaxElectricBounce;
                    btwData.Spark_MaxCharge = this.SP_MaxCharge;
                    btwData.Spark_RiskyOvercharge = this.SP_RiskyOvercharge;
                    btwData.Spark_DeadlyOvercharge = this.SP_DeadlyOvercharge;
                    
                    btwData.ArenaLives_AdditionalReviveTime = this.AL_AdditionalReviveTime;
                    btwData.ArenaLives_Amount = this.AL_Amount;
                    btwData.ArenaLives_BlockWin = this.AL_BlockWin;
                    btwData.ArenaLives_ReviveFromAbyss = this.AL_ReviveFromAbyss;
                    btwData.ArenaLives_ReviveTime = this.AL_ReviveTime;
                    btwData.ArenaLives_Strict = this.AL_Strict;
                    btwData.ArenaLives_RespawnShieldDuration = this.AL_RespawnShieldDuration;
                }
            }

            public override Type GetDataType() => typeof(BTWArenaLobbyRessourceData);
        }
    }
    public class BTWAdditionalSettingsPage : SettingsPage//, CheckBox.IOwnCheckBox
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
            // void CreateCheckBox(ref RestorableCheckbox checkbox, Configurable<bool> setting, string settingName, string settingID, int place)
            // { // automatized this too
            //     checkbox = new(
            //         menu, this, this, positioner - spacing * place, textSpacing, settingName,
            //         settingID, false, setting.info.description);
            // }
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
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
                btwAData.Trailseeker_PoleClimbBonus =
                    BTWRemix.MeadowTrailseekerPoleClimbBonus.ClampValue(TrailSeeker_PoleClimbBonus_TextBox.valueInt);
            };

            // MaxWallClimb
            CreateIntTextBox(ref Trailseeker_MaxWallClimb_TextBox, ref Trailseeker_MaxWallClimb_Label,
                BTWRemix.MeadowTrailseekerMaxWallClimb, "Wall Climbs:", 2);

            Trailseeker_MaxWallClimb_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
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
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
                btwAData.Core_MaxEnergy =
                    BTWRemix.MeadowCoreMaxEnergy.ClampValue(Core_MaxEnergy_TextBox.valueInt);
            };

            // RegenEnergy
            CreateIntTextBox(ref Core_RegenEnergy_TextBox, ref Core_RegenEnergy_Label,
                BTWRemix.MeadowCoreRegenEnergy, "Energy Regeneration:", 5);

            Core_RegenEnergy_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
                btwAData.Core_RegenEnergy =
                    BTWRemix.MeadowCoreRegenEnergy.ClampValue(Core_RegenEnergy_TextBox.valueInt);
            };

            // OxygenEnergyUsage
            CreateIntTextBox(ref Core_OxygenEnergyUsage_TextBox, ref Core_OxygenEnergyUsage_Label,
                BTWRemix.MeadowCoreOxygenEnergyUsage, "Underwater Breathing Energy Usage:", 6, true);

            Core_OxygenEnergyUsage_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
                btwAData.Core_OxygenEnergyUsage =
                    BTWRemix.MeadowCoreOxygenEnergyUsage.ClampValue(Core_OxygenEnergyUsage_TextBox.valueInt);
            };

            // AntiGravityCent
            CreateIntTextBox(ref Core_AntiGravityCent_TextBox, ref Core_AntiGravityCent_Label,
                BTWRemix.MeadowCoreAntiGravityCent, "Anti-gravity Pourcent:", 7);

            Core_AntiGravityCent_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
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
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
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
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
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
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
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

            if (!MeadowCompat.IsMeadowArena(out _)) return;
            BTWMeadowArenaSettings btwSettings = GetBTWArenaSettings();
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

            if (MeadowCompat.IsMeadowArena(out _) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings)
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

        // public bool GetChecked(CheckBox box)
        // {
        //     string id = box.IDString;
        //     if (MeadowCompat.IsMeadowArena(out var arena) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings && settings != null)
        //     {
        //         if (id == TS_ALTEREDMVTTECH) return settings.Trailseeker_AlteredMovementTech;
        //         if (id == SP_DODISCHARGEDAMAGE) return settings.Spark_DoDischargeDamage;
        //         if (id == SP_RISKYOVERCHARGE) return settings.Spark_RiskyOvercharge;
        //     }
        //     return false;
        // }

        // public void SetChecked(CheckBox box, bool c)
        // {
        //     if (!(MeadowCompat.IsMeadowArena(out ArenaMode arena) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings && settings != null)) return;
        //     string id = box.IDString;
        //     if (id == TS_ALTEREDMVTTECH) { settings.Trailseeker_AlteredMovementTech = c; }
        //     else if (id == SP_DODISCHARGEDAMAGE) { settings.Spark_DoDischargeDamage = c; }
        //     else if (id == SP_RISKYOVERCHARGE) { settings.Spark_RiskyOvercharge = c; }
        // }
    }
    public class BTWEssentialSettingsPage : SettingsPage, CheckBox.IOwnCheckBox
    {
        public const string TS_ALTEREDMVTTECH = "BTW_TR_AMT";
        public const string CO_SHOCKWAVE = "BTW_CO_SHW";
        public const string SP_DODISCHARGEDAMAGE = "BTW_SP_DDD";
        public const string SP_RISKYOVERCHARGE = "BTW_SP_ROC";
        public const string SP_DEADLYOVERCHARGE = "BTW_SP_DOC";
        public MenuTabWrapper tabWrapper;

        public MenuLabel TrailSeeker_Title, Core_Title, Spark_Title;
        public OpTextBox Trailseeker_WallGripTimer_TextBox,
            Core_MaxLeap_TextBox,
            Spark_MaxElectricBounce_TextBox;
        public MenuLabel Trailseeker_WallGripTimer_Label,
            Core_MaxLeap_Label, 
            Spark_MaxElectricBounce_Label;
        public RestorableCheckbox TrailSeeker_AlteredMovementTech_CheckBox,
            Core_Shockwave_CheckBox, 
            Spark_DoDischargeDamage_CheckBox, Spark_RiskyOvercharge_CheckBox, Spark_DeadlyOvercharge_CheckBox;

        public SimpleButton backButton;

        public override string Name => "BTW Essential Settings"; //this will appear on Select Settings Page

        public BTWEssentialSettingsPage(Menu.Menu menu, MenuObject owner, Vector2 spacing, float textSpacing = 300) : base(menu, owner)
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
            CreateCheckBox(ref TrailSeeker_AlteredMovementTech_CheckBox, BTWRemix.MeadowTrailseekerAlteredMovementTech,
                "Altered Movement Tech:", TS_ALTEREDMVTTECH, 1);

            // WallGripTimer
            CreateIntTextBox(ref Trailseeker_WallGripTimer_TextBox, ref Trailseeker_WallGripTimer_Label,
                BTWRemix.MeadowTrailseekerWallGripTimer, "Wall Grip Timer:", 2);

            Trailseeker_WallGripTimer_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
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
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
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
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
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
                TrailSeeker_AlteredMovementTech_CheckBox,
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
            BTWRemix.MeadowTrailseekerAlteredMovementTech.Value = this.TrailSeeker_AlteredMovementTech_CheckBox.Checked;
            BTWRemix.MeadowTrailseekerWallGripTimer.Value = this.Trailseeker_WallGripTimer_TextBox.valueInt;

            BTWRemix.MeadowCoreMaxLeap.Value = this.Core_MaxLeap_TextBox.valueInt;
            BTWRemix.MeadowCoreShockwave.Value = this.Core_Shockwave_CheckBox.Checked;

            BTWRemix.MeadowSparkMaxElectricBounce.Value = this.Spark_MaxElectricBounce_TextBox.valueInt;
            BTWRemix.MeadowSparkDoDischargeDamage.Value = this.Spark_DoDischargeDamage_CheckBox.Checked;
            BTWRemix.MeadowSparkRiskyOvercharge.Value = this.Spark_RiskyOvercharge_CheckBox.Checked;
            BTWRemix.MeadowSparkDeadlyOvercharge.Value = this.Spark_DeadlyOvercharge_CheckBox.Checked;
        }
        public override void CallForSync()
        {
            foreach (MenuObject menuObj in subObjects) { SyncMenuObjectStatus(menuObj); }
            
            if (!MeadowCompat.IsMeadowArena(out _)) return;
            BTWMeadowArenaSettings btwSettings = GetBTWArenaSettings();
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
                    menu.MutualVerticalButtonBind(TrailSeeker_AlteredMovementTech_CheckBox, backButton); //loop
                }
                if (forceSelectedObject) menu.selectedObject = TrailSeeker_AlteredMovementTech_CheckBox;
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
            
            if (MeadowCompat.IsMeadowArena(out _) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings)
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
            if (MeadowCompat.IsMeadowArena(out var _) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings && settings != null)
            {
                if (id == TS_ALTEREDMVTTECH) return settings.Trailseeker_AlteredMovementTech;
                if (id == CO_SHOCKWAVE) return settings.Core_Shockwave;
                if (id == SP_DODISCHARGEDAMAGE) return settings.Spark_DoDischargeDamage;
                if (id == SP_RISKYOVERCHARGE) return settings.Spark_RiskyOvercharge;
                if (id == SP_DEADLYOVERCHARGE) return settings.Spark_DeadlyOvercharge;
            }
            return false;
        }

        public void SetChecked(CheckBox box, bool c)
        {
            if (!(MeadowCompat.IsMeadowArena(out ArenaMode _) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings && settings != null)) return;
            string id = box.IDString;
            if (id == TS_ALTEREDMVTTECH) {settings.Trailseeker_AlteredMovementTech = c;}
            else if (id == CO_SHOCKWAVE) {settings.Core_Shockwave = c;}
            else if (id == SP_DODISCHARGEDAMAGE) {settings.Spark_DoDischargeDamage = c;}
            else if (id == SP_RISKYOVERCHARGE) {settings.Spark_RiskyOvercharge = c;}
            else if (id == SP_DEADLYOVERCHARGE) {settings.Spark_DeadlyOvercharge = c;}
        }
    }
    public class BTWArenaSettingsPage : SettingsPage, CheckBox.IOwnCheckBox
    {
        public const string AL_BLOCKWIN = "BTW_AL_BLW";
        public const string AL_REVIVEFROMABYSS = "BTW_AL_RFA";
        public const string AL_STRICT = "BTW_AL_STR";
        public MenuTabWrapper tabWrapper;

        public MenuLabel ArenaLives_Title, WIP_Warning;
        public OpTextBox ArenaLives_AdditionalReviveTime_TextBox,
            ArenaLives_Amount_TextBox,
            ArenaLives_ReviveTime_TextBox,
            ArenaLives_RespawnShieldDuration_TextBox;
        public MenuLabel ArenaLives_AdditionalReviveTime_Label,
            ArenaLives_Amount_Label, 
            ArenaLives_ReviveTime_Label,
            ArenaLives_RespawnShieldDuration_Label;
        public RestorableCheckbox ArenaLives_BlockWin_CheckBox, 
            ArenaLives_ReviveFromAbyss_CheckBox, 
            ArenaLives_Strict_CheckBox;

        public SimpleButton backButton;

        public override string Name => "BTW Arena Settings"; //this will appear on Select Settings Page

        public BTWArenaSettingsPage(Menu.Menu menu, MenuObject owner, Vector2 spacing, float textSpacing = 300) : base(menu, owner)
        {
            tabWrapper = new(menu, this);
            Vector2 positioner = new(360 + ((textSpacing - 300) / 2), 380);
            
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
            void CreateWarning(ref MenuLabel title, string text, float place, bool big = false)
            { // atp why not
                title = new(menu, this, text, positioner - spacing * place + new Vector2(-textSpacing * 1.0f + 7.5f, 3), new(textSpacing, big ? 25 : 20), big);
                title.label.alignment = FLabelAlignment.Center;
                title.label.color = Color.red;
            }

            CreateWarning(ref WIP_Warning, "Those settings are still WIP (here be dragons !)", -1f);
            //--------------------- TrailSeeker
            CreateTitle(ref ArenaLives_Title, "Arena Lives", 0);

            // ArenaLives_Amount
            CreateIntTextBox(ref ArenaLives_Amount_TextBox, ref ArenaLives_Amount_Label,
                BTWRemix.MeadowArenaLivesAmount, "Lives :", 1);

            ArenaLives_Amount_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
                btwAData.ArenaLives_Amount =
                    BTWRemix.MeadowArenaLivesAmount.ClampValue(ArenaLives_Amount_TextBox.valueInt);
            };

            // ArenaLives_Strict
            CreateCheckBox(ref ArenaLives_Strict_CheckBox, BTWRemix.MeadowArenaLivesStrict,
                "Block revives after reaching 0 live :", AL_STRICT, 2);

            // ArenaLives_BlockWin
            CreateCheckBox(ref ArenaLives_BlockWin_CheckBox, BTWRemix.MeadowArenaLivesBlockWin,
                "Block session end on revival :", AL_BLOCKWIN, 3);

            // ArenaLives_ReviveFromAbyss
            CreateCheckBox(ref ArenaLives_ReviveFromAbyss_CheckBox, BTWRemix.MeadowArenaLivesReviveFromAbyss,
                "Revive destroyed bodies :", AL_REVIVEFROMABYSS, 4);

            //  ArenaLives_ReviveTime
            CreateIntTextBox(ref ArenaLives_ReviveTime_TextBox, ref ArenaLives_ReviveTime_Label,
                BTWRemix.MeadowArenaLivesReviveTime, "Revive time :", 5);

            ArenaLives_ReviveTime_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
                btwAData.ArenaLives_ReviveTime =
                    BTWRemix.MeadowArenaLivesReviveTime.ClampValue(ArenaLives_ReviveTime_TextBox.valueInt);
            };

            //  ArenaLives_ReviveTime
            CreateIntTextBox(ref ArenaLives_AdditionalReviveTime_TextBox, ref ArenaLives_AdditionalReviveTime_Label,
                BTWRemix.MeadowArenaLivesAdditionalReviveTime, "Additionnal per life revive time :", 6);

            ArenaLives_AdditionalReviveTime_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
                btwAData.ArenaLives_AdditionalReviveTime =
                    BTWRemix.MeadowArenaLivesAdditionalReviveTime.ClampValue(ArenaLives_AdditionalReviveTime_TextBox.valueInt);
            };

            //  ArenaLives_RespawnShieldDuration
            CreateIntTextBox(ref ArenaLives_RespawnShieldDuration_TextBox, ref ArenaLives_RespawnShieldDuration_Label,
                BTWRemix.MeadowArenaLivesRespawnShieldDuration, "Respawn shield duration :", 7);

            ArenaLives_RespawnShieldDuration_TextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!MeadowCompat.IsMeadowArena(out var arena)) return;
                BTWMeadowArenaSettings btwAData = GetBTWArenaSettings();
                btwAData.ArenaLives_RespawnShieldDuration =
                    BTWRemix.MeadowArenaLivesRespawnShieldDuration.ClampValue(ArenaLives_RespawnShieldDuration_TextBox.valueInt);
            };


            this.SafeAddSubobjects(
                tabWrapper,
                WIP_Warning, ArenaLives_Title,

                ArenaLives_BlockWin_CheckBox, 
                ArenaLives_ReviveFromAbyss_CheckBox, 
                ArenaLives_Strict_CheckBox,

                ArenaLives_AdditionalReviveTime_Label,
                ArenaLives_Amount_Label, 
                ArenaLives_ReviveTime_Label,
                ArenaLives_RespawnShieldDuration_Label);
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
            BTWRemix.MeadowArenaLivesReviveFromAbyss.Value = this.ArenaLives_ReviveFromAbyss_CheckBox.Checked;
            BTWRemix.MeadowArenaLivesReviveTime.Value = this.ArenaLives_ReviveTime_TextBox.valueInt;
            BTWRemix.MeadowArenaLivesRespawnShieldDuration.Value = this.ArenaLives_RespawnShieldDuration_TextBox.valueInt;
            BTWRemix.MeadowArenaLivesStrict.Value = this.ArenaLives_Strict_CheckBox.Checked;
        }
        public override void CallForSync()
        {
            foreach (MenuObject menuObj in subObjects) { SyncMenuObjectStatus(menuObj); }
            
            if (!MeadowCompat.IsMeadowArena(out _)) return;
            BTWMeadowArenaSettings btwSettings = GetBTWArenaSettings();

            btwSettings.ArenaLives_AdditionalReviveTime = this.ArenaLives_AdditionalReviveTime_TextBox.valueInt;
            btwSettings.ArenaLives_Amount = this.ArenaLives_Amount_TextBox.valueInt;
            btwSettings.ArenaLives_ReviveTime = this.ArenaLives_ReviveTime_TextBox.valueInt;
            btwSettings.ArenaLives_RespawnShieldDuration = this.ArenaLives_RespawnShieldDuration_TextBox.valueInt;
        }
        
        public override void SelectAndCreateBackButtons(SettingsPage previousSettingPage, bool forceSelectedObject)
        {
            if (backButton == null)
                {
                    backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(15, 15), new(80, 30));
                    AddObjects(backButton);
                    menu.MutualVerticalButtonBind(backButton, ArenaLives_Amount_Label);
                    menu.MutualVerticalButtonBind(ArenaLives_RespawnShieldDuration_Label, backButton); //loop
                }
                if (forceSelectedObject) menu.selectedObject = ArenaLives_RespawnShieldDuration_Label;
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
            ArenaLives_ReviveFromAbyss_CheckBox.buttonBehav.greyedOut = true;
            
            if (MeadowCompat.IsMeadowArena(out _) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings)
            {
                void UpdateIntTextBox(ref OpTextBox textBox, int value )
                { // oops, it's all functions
                    textBox.greyedOut = greyoutAll;
                    textBox.held = textBox._KeyboardOn;
                    if (!textBox.held) { textBox.valueInt = value; }
                }

                UpdateIntTextBox(ref ArenaLives_AdditionalReviveTime_TextBox, settings.ArenaLives_AdditionalReviveTime);
                UpdateIntTextBox(ref ArenaLives_Amount_TextBox, settings.ArenaLives_Amount);
                UpdateIntTextBox(ref ArenaLives_ReviveTime_TextBox, settings.ArenaLives_ReviveTime);
                UpdateIntTextBox(ref ArenaLives_RespawnShieldDuration_TextBox, settings.ArenaLives_RespawnShieldDuration);
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
        }

        public bool GetChecked(CheckBox box)
        {
            string id = box.IDString;
            if (MeadowCompat.IsMeadowArena(out var _) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings && settings != null)
            {
                if (id == AL_BLOCKWIN) return settings.ArenaLives_BlockWin;
                if (id == AL_REVIVEFROMABYSS) return false; //settings.ArenaLives_ReviveFromAbyss;
                if (id == AL_STRICT) return settings.ArenaLives_Strict;
            }
            return false;
        }

        public void SetChecked(CheckBox box, bool c)
        {
            if (!(MeadowCompat.IsMeadowArena(out ArenaMode _) && GetBTWArenaSettings() is BTWMeadowArenaSettings settings && settings != null)) return;
            string id = box.IDString;
            if (id == AL_BLOCKWIN) {settings.ArenaLives_BlockWin = c;}
            else if (id == AL_REVIVEFROMABYSS) {settings.ArenaLives_ReviveFromAbyss = false;}
            else if (id == AL_STRICT) {settings.ArenaLives_Strict = c;}
        }
    }

    //----------- Functions

    public static BTWMeadowArenaSettings GetBTWArenaSettings()
    {
        if (MeadowCompat.IsMeadowArena(out var arenaOnlineGameMode) && cwtMeadowArenaData.TryGetValue(arenaOnlineGameMode, out var data))
        {
            return data;
        }
        return null;
    }
    public static bool TryGetBTWArenaSettings(out BTWMeadowArenaSettings meadowArenaSettings)
    {
        meadowArenaSettings = GetBTWArenaSettings();
        if (meadowArenaSettings == null) { return false; }
        return true;
    }


    //----------- Hooks

    public static void ApplyHooks()
    {
        try
        {
            ArenaDataHook();
            new Hook(typeof(OnlineSlugcatAbilitiesInterface).GetMethod(nameof(OnlineSlugcatAbilitiesInterface.AddAllSettings)), OnlineSlugcatAbilitiesInterface_AddAllSettings);
            new Hook(typeof(ArenaMode).GetConstructor(new[] { typeof(Lobby) }), ArenaMode_AddData);
            new Hook(typeof(RainMeadow.UI.Pages.ArenaMainLobbyPage).GetMethod("ShouldOpenSlugcatAbilitiesTab"), ArenaMainLobbyPage_ShouldOpenSlugcatAbilitiesTab);
            On.Player.ctor += Player_AddArenaLivesFromSettings;
            new Hook(typeof(ArenaOnlineLobbyMenu).GetMethod(nameof(ArenaOnlineLobbyMenu.ShutDownProcess)), ArenaMode_SaveData);
        }
        catch (Exception e)
        {
            Plugin.logger.LogError(e);
        }
        Plugin.Log("MeadowBTWArenaMenu ApplyHooks Done !");
    }

    private static void ArenaDataHook()
    {
        Plugin.Log("Meadow BTW Arena Data starts");
        try
        {
            new Hook(typeof(Lobby).GetMethod("ActivateImpl", BindingFlags.NonPublic | BindingFlags.Instance), (Action<Lobby> orig, Lobby self) =>
            {
                orig(self);
                if (MeadowCompat.IsMeadowArena(out var arenaOnlineGameMode))
                {
                    OnlineManager.lobby.AddData(new BTWArenaLobbyRessourceData());
                }
            });
            Plugin.Log("Meadow hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("Meadow BTW Arena Data ends");
    }
    private static void OnlineSlugcatAbilitiesInterface_AddAllSettings(Action<OnlineSlugcatAbilitiesInterface, string> orig, OnlineSlugcatAbilitiesInterface self, string painCatName)
    {
        orig(self, painCatName);
        BTWEssentialSettingsPage essentialSettingsPage = new(self.menu, self, new Vector2(0f, 30f), 300f);
        self.AddSettingsTab(essentialSettingsPage, "BTWESSSETTINGS");
        BTWAdditionalSettingsPage additionalSettingsPage = new(self.menu, self, new Vector2(0f, 28.5f), 300f);
        self.AddSettingsTab(additionalSettingsPage, "BTWADDSETTINGS");
        BTWArenaSettingsPage arenaSettingsPage = new(self.menu, self, new Vector2(0f, 30f), 300f);
        self.AddSettingsTab(arenaSettingsPage, "BTWARESETTINGS");
    }
    private static void ArenaMode_AddData(Action<ArenaMode, Lobby> orig, ArenaMode self, Lobby lobby)
    {
        orig(self, lobby);
        BTWMeadowArenaSettings arenaSettings;
        if (!cwtMeadowArenaData.TryGetValue(self, out _))
        {
            arenaSettings = new BTWMeadowArenaSettings(self);
            cwtMeadowArenaData.Add(self, arenaSettings);
        }
    }
    private static bool ArenaMainLobbyPage_ShouldOpenSlugcatAbilitiesTab(Func<RainMeadow.UI.Pages.ArenaMainLobbyPage, bool> orig, RainMeadow.UI.Pages.ArenaMainLobbyPage self)
    {
        return true;
    }
    private static void Player_AddArenaLivesFromSettings(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (TryGetBTWArenaSettings(out var meadowArenaSettings))
        {
            if (meadowArenaSettings.ArenaLives_Amount > 0 && self.room != null)
            {
                CompetitiveAddition.ArenaLives arenaLives = new(
                    abstractCreature, 
                    meadowArenaSettings.ArenaLives_Amount,
                    meadowArenaSettings.ArenaLives_ReviveTime * BTWFunc.FrameRate,
                    meadowArenaSettings.ArenaLives_AdditionalReviveTime * BTWFunc.FrameRate,
                    meadowArenaSettings.ArenaLives_BlockWin, !MeadowCompat.IsMine(abstractCreature));
                self.room.AddObject( arenaLives );
            }
        }
    }
    private static void ArenaMode_SaveData(Action<ArenaOnlineLobbyMenu> orig, ArenaOnlineLobbyMenu self)
    {
        orig(self);
        BTWRemix.instance._SaveConfigFile();
    }
}
