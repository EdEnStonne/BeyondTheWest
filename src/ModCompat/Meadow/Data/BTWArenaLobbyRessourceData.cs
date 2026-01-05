using System;
using RainMeadow;

namespace BeyondTheWest.MeadowCompat.Data;
public class BTWArenaLobbyRessourceData : OnlineResource.ResourceData
{
    public BTWArenaLobbyRessourceData() { }

    public override ResourceDataState MakeState(OnlineResource resource)
    {
        return new State(BTWMeadowArenaSettings.GetSettings(), resource);
    }

    internal class State : ResourceDataState
    {
        // Group: Trailseeker
        [OnlineField(group = "Trailseeker")]
        public bool TS_EveryoneCanPoleTech;
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
        public int AL_RespawnShieldDuration;
        
        // Group: ArenaItems
        [OnlineField(group = "ArenaItems")]
        public bool AT_NewItemSpawningSystem;
        [OnlineField(group = "ArenaItems")]
        public int AT_ItemSpawnMultiplierCent;
        [OnlineField(group = "ArenaItems")]
        public int AT_ItemSpawnMultiplierPerPlayersCent;
        [OnlineField(group = "ArenaItems")]
        public bool AT_ItemSpawnDiversity;
        [OnlineField(group = "ArenaItems")]
        public bool AT_ItemSpawnRandom;
        [OnlineField(group = "ArenaItems")]
        public bool AT_ItemRespawn;
        
        

        public State() { }
        public State(BTWMeadowArenaSettings BTWMeadowArenaSettings, OnlineResource onlineResource)
        {
            if (BTWMeadowArenaSettings == null) { return; }
            this.TS_EveryoneCanPoleTech = BTWMeadowArenaSettings.Trailseeker_EveryoneCanPoleTech;
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
            this.AL_ReviveTime = BTWMeadowArenaSettings.ArenaLives_ReviveTime;
            this.AL_RespawnShieldDuration = BTWMeadowArenaSettings.ArenaLives_RespawnShieldDuration;
            
            this.AT_ItemSpawnDiversity = BTWMeadowArenaSettings.ArenaItems_ItemSpawnDiversity;
            this.AT_ItemSpawnMultiplierCent = BTWMeadowArenaSettings.ArenaItems_ItemSpawnMultiplierCent;
            this.AT_ItemSpawnMultiplierPerPlayersCent = BTWMeadowArenaSettings.ArenaItems_ItemSpawnMultiplierPerPlayersCent;
            this.AT_ItemSpawnRandom = BTWMeadowArenaSettings.ArenaItems_ItemSpawnRandom;
            this.AT_NewItemSpawningSystem = BTWMeadowArenaSettings.ArenaItems_NewItemSpawningSystem;
            this.AT_ItemRespawn = BTWMeadowArenaSettings.ArenaItems_ItemRespawn;
        }

        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
        {
            if (BTWMeadowArenaSettings.TryGetSettings(out var btwData))
            {
                btwData.Trailseeker_EveryoneCanPoleTech = this.TS_EveryoneCanPoleTech;
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
                btwData.ArenaLives_ReviveTime = this.AL_ReviveTime;
                btwData.ArenaLives_RespawnShieldDuration = this.AL_RespawnShieldDuration;

                btwData.ArenaItems_ItemSpawnDiversity = this.AT_ItemSpawnDiversity;
                btwData.ArenaItems_ItemSpawnMultiplierCent = this.AT_ItemSpawnMultiplierCent;
                btwData.ArenaItems_ItemSpawnMultiplierPerPlayersCent = this.AT_ItemSpawnMultiplierPerPlayersCent;
                btwData.ArenaItems_ItemSpawnRandom = this.AT_ItemSpawnRandom;
                btwData.ArenaItems_NewItemSpawningSystem = this.AT_NewItemSpawningSystem;
                btwData.ArenaItems_ItemRespawn = this.AT_ItemRespawn;
            }
        }

        public override Type GetDataType() => typeof(BTWArenaLobbyRessourceData);
    }
}