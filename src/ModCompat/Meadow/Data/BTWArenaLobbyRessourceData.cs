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
        // [OnlineField(group = "Trailseeker")]
        // public int TS_PoleClimbBonus;
        // [OnlineField(group = "Trailseeker")]
        // public int TS_MaxWallClimb;
        [OnlineField(group = "Trailseeker")]
        public int TS_WallGripTimer;
        
        // Group: Core
        // [OnlineField(group = "Core")]
        // public int CO_RegenEnergy;
        // [OnlineField(group = "Core")]
        // public int CO_MaxEnergy;
        [OnlineField(group = "Core")]
        public int CO_MaxLeap;
        // [OnlineField(group = "Core")]
        // public int CO_AntiGravityCent;
        // [OnlineField(group = "Core")]
        // public int CO_OxygenEnergyUsage;
        [OnlineField(group = "Core")]
        public bool CO_Shockwave;

        // Group: Spark
        // [OnlineField(group = "Spark")]            
        // public int SP_AdditionnalOvercharge;
        // [OnlineField(group = "Spark")]
        // public int SP_ChargeRegenerationMult;
        [OnlineField(group = "Spark")]
        public int SP_MaxElectricBounce;
        [OnlineField(group = "Spark")]
        public bool SP_DoDischargeDamage;
        [OnlineField(group = "Spark")]
        public bool SP_RiskyOvercharge;
        [OnlineField(group = "Spark")]
        public bool SP_DeadlyOvercharge;
        // [OnlineField(group = "Spark")]
        // public int SP_MaxCharge;
        
        // Group: ArenaLives
        [OnlineField(group = "ArenaLives")]
        public int AL_Amount;
        [OnlineField(group = "ArenaLives")]
        public bool AL_EveryoneCanSet;
        [OnlineField(group = "ArenaLives")]
        public bool AL_BlockWin;
        [OnlineField(group = "ArenaLives")]
        public bool AL_Strict0Life;
        [OnlineField(group = "ArenaLives")]
        public int AL_ReviveTime;
        [OnlineField(group = "ArenaLives")]
        public int AL_AdditionalReviveTime;
        [OnlineField(group = "ArenaLives")]
        public bool AL_RespawnShieldToggle;
        [OnlineField(group = "ArenaLives")]
        public int AL_RespawnShieldDuration;
        [OnlineField(group = "ArenaLives")]
        public bool AL_KarmaLife;
        [OnlineField(group = "ArenaLives")]
        public bool AL_KarmaProtection;
        [OnlineField(group = "ArenaLives")]
        public bool AL_KillLife;
        [OnlineField(group = "ArenaLives")]
        public int AL_KillLifeAmount;
        [OnlineField(group = "ArenaLives")]
        public bool AL_KillProtection;
        [OnlineField(group = "ArenaLives")]
        public int AL_KillProtectionAmount;
        
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
        [OnlineField(group = "ArenaItems")]
        public int AT_ItemRespawnTimer;
        [OnlineField(group = "ArenaItems")]
        public bool AT_ItemCheckSpearCount;
        [OnlineField(group = "ArenaItems")]
        public bool AT_ItemCheckThrowableCount;
        [OnlineField(group = "ArenaItems")]
        public bool AT_ItemCheckMiscellaneousCount;
        [OnlineField(group = "ArenaItems")]
        public bool AT_ItemNoSpear;
        
        // Group: ArenaBonus
        [OnlineField(group = "ArenaBonus")]
        public bool AB_InstantDeath;
        [OnlineField(group = "ArenaBonus")]
        public bool AB_ExtraItemUses;
        
        

        public State() { }
        public State(BTWMeadowArenaSettings arenaSettings, OnlineResource onlineResource)
        {
            if (arenaSettings == null) { return; }
            this.TS_EveryoneCanPoleTech = arenaSettings.Trailseeker_EveryoneCanPoleTech;
            // this.TS_PoleClimbBonus = arenaSettings.Trailseeker_PoleClimbBonus;
            // this.TS_MaxWallClimb = arenaSettings.Trailseeker_MaxWallClimb;
            this.TS_WallGripTimer = arenaSettings.Trailseeker_WallGripTimer;

            // this.CO_RegenEnergy = arenaSettings.Core_RegenEnergy;
            // this.CO_OxygenEnergyUsage = arenaSettings.Core_OxygenEnergyUsage;
            // this.CO_MaxEnergy = arenaSettings.Core_MaxEnergy;
            this.CO_MaxLeap = arenaSettings.Core_MaxLeap;
            // this.CO_AntiGravityCent = arenaSettings.Core_AntiGravityCent;
            this.CO_Shockwave = arenaSettings.Core_Shockwave;

            // this.SP_AdditionnalOvercharge = arenaSettings.Spark_AdditionnalOvercharge;
            // this.SP_ChargeRegenerationMult = arenaSettings.Spark_ChargeRegenerationMult;
            this.SP_DoDischargeDamage = arenaSettings.Spark_DoDischargeDamage;
            this.SP_MaxElectricBounce = arenaSettings.Spark_MaxElectricBounce;
            // this.SP_MaxCharge = arenaSettings.Spark_MaxCharge;
            this.SP_RiskyOvercharge = arenaSettings.Spark_RiskyOvercharge;
            this.SP_DeadlyOvercharge = arenaSettings.Spark_DeadlyOvercharge;
            
            if (MeadowFunc.IsMeadowArena(out var arenaOnline) && arenaOnline.IsStockArenaMode(out var stockArenaMode))
            {
                this.AL_Amount = stockArenaMode.LivesDefaultAmount;
                this.AL_EveryoneCanSet = stockArenaMode.everyoneCanModifyTheirLifeAmount;
                this.AL_BlockWin = stockArenaMode.blockWin;
                this.AL_Strict0Life = stockArenaMode.strictEnforceAfter0Lives;
                this.AL_ReviveTime = stockArenaMode.reviveTime;
                this.AL_AdditionalReviveTime = stockArenaMode.additionalReviveTime;
                this.AL_RespawnShieldToggle = stockArenaMode.respawnShieldToggle;
                this.AL_RespawnShieldDuration = stockArenaMode.respawnShieldDuration;
                this.AL_KarmaLife = stockArenaMode.karmaFlowerGiveLife;
                this.AL_KarmaProtection = stockArenaMode.karmaFlowerGiveProtection;
                this.AL_KillLife = stockArenaMode.killGiveLife;
                this.AL_KillLifeAmount = stockArenaMode.killAmountForLife;
                this.AL_KillProtection = stockArenaMode.killGiveProtection;
                this.AL_KillProtectionAmount = stockArenaMode.killAmountForProtection;
            }
            
            this.AT_ItemSpawnDiversity = arenaSettings.ArenaItems_ItemSpawnDiversity;
            this.AT_ItemSpawnMultiplierCent = arenaSettings.ArenaItems_ItemSpawnMultiplierCent;
            this.AT_ItemSpawnMultiplierPerPlayersCent = arenaSettings.ArenaItems_ItemSpawnMultiplierPerPlayersCent;
            this.AT_ItemSpawnRandom = arenaSettings.ArenaItems_ItemSpawnRandom;
            this.AT_NewItemSpawningSystem = arenaSettings.ArenaItems_NewItemSpawningSystem;
            this.AT_ItemRespawn = arenaSettings.ArenaItems_ItemRespawn;
            this.AT_ItemRespawnTimer = arenaSettings.ArenaItems_ItemRespawnTimer;
            this.AT_ItemCheckSpearCount = arenaSettings.ArenaItems_CheckSpearCount;
            this.AT_ItemCheckThrowableCount = arenaSettings.ArenaItems_CheckThrowableCount;
            this.AT_ItemCheckMiscellaneousCount = arenaSettings.ArenaItems_CheckMiscellaneousCount;
            this.AT_ItemNoSpear = arenaSettings.ArenaItems_NoSpear;

            this.AB_InstantDeath = arenaSettings.ArenaBonus_InstantDeath;
            this.AB_ExtraItemUses = arenaSettings.ArenaBonus_ExtraItemUses;
        }

        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
        {
            if (BTWMeadowArenaSettings.TryGetSettings(out var arenaSettings))
            {
                arenaSettings.Trailseeker_EveryoneCanPoleTech = this.TS_EveryoneCanPoleTech;
                // arenaSettings.Trailseeker_PoleClimbBonus = this.TS_PoleClimbBonus;
                // arenaSettings.Trailseeker_MaxWallClimb = this.TS_MaxWallClimb;
                arenaSettings.Trailseeker_WallGripTimer = this.TS_WallGripTimer;

                // arenaSettings.Core_MaxEnergy = this.CO_MaxEnergy;
                // arenaSettings.Core_OxygenEnergyUsage = this.CO_OxygenEnergyUsage;
                // arenaSettings.Core_RegenEnergy = this.CO_RegenEnergy;
                // arenaSettings.Core_AntiGravityCent = this.CO_AntiGravityCent;
                arenaSettings.Core_MaxLeap = this.CO_MaxLeap;
                arenaSettings.Core_Shockwave = this.CO_Shockwave;

                // arenaSettings.Spark_AdditionnalOvercharge = this.SP_AdditionnalOvercharge;
                // arenaSettings.Spark_ChargeRegenerationMult = this.SP_ChargeRegenerationMult;
                arenaSettings.Spark_DoDischargeDamage = this.SP_DoDischargeDamage;
                arenaSettings.Spark_MaxElectricBounce = this.SP_MaxElectricBounce;
                // arenaSettings.Spark_MaxCharge = this.SP_MaxCharge;
                arenaSettings.Spark_RiskyOvercharge = this.SP_RiskyOvercharge;
                arenaSettings.Spark_DeadlyOvercharge = this.SP_DeadlyOvercharge;
                
                if (MeadowFunc.IsMeadowArena(out var arenaOnline) && arenaOnline.IsStockArenaMode(out var stockArenaMode))
                {
                    stockArenaMode.LivesDefaultAmount = this.AL_Amount;
                    stockArenaMode.everyoneCanModifyTheirLifeAmount = this.AL_EveryoneCanSet;
                    stockArenaMode.blockWin = this.AL_BlockWin;
                    stockArenaMode.strictEnforceAfter0Lives = this.AL_Strict0Life;
                    stockArenaMode.reviveTime = this.AL_ReviveTime;
                    stockArenaMode.additionalReviveTime = this.AL_AdditionalReviveTime;
                    stockArenaMode.respawnShieldToggle = this.AL_RespawnShieldToggle;
                    stockArenaMode.respawnShieldDuration = this.AL_RespawnShieldDuration;
                    stockArenaMode.karmaFlowerGiveLife = this.AL_KarmaLife;
                    stockArenaMode.karmaFlowerGiveProtection = this.AL_KarmaProtection;
                    stockArenaMode.killGiveLife = this.AL_KillLife;
                    stockArenaMode.killAmountForLife = this.AL_KillLifeAmount;
                    stockArenaMode.killGiveProtection = this.AL_KillProtection;
                    stockArenaMode.killAmountForProtection = this.AL_KillProtectionAmount;
                }

                arenaSettings.ArenaItems_ItemSpawnDiversity = this.AT_ItemSpawnDiversity;
                arenaSettings.ArenaItems_ItemSpawnMultiplierCent = this.AT_ItemSpawnMultiplierCent;
                arenaSettings.ArenaItems_ItemSpawnMultiplierPerPlayersCent = this.AT_ItemSpawnMultiplierPerPlayersCent;
                arenaSettings.ArenaItems_ItemSpawnRandom = this.AT_ItemSpawnRandom;
                arenaSettings.ArenaItems_NewItemSpawningSystem = this.AT_NewItemSpawningSystem;
                arenaSettings.ArenaItems_ItemRespawn = this.AT_ItemRespawn;
                arenaSettings.ArenaItems_ItemRespawnTimer = this.AT_ItemRespawnTimer;
                arenaSettings.ArenaItems_CheckSpearCount = this.AT_ItemCheckSpearCount;
                arenaSettings.ArenaItems_CheckThrowableCount = this.AT_ItemCheckThrowableCount;
                arenaSettings.ArenaItems_CheckMiscellaneousCount = this.AT_ItemCheckMiscellaneousCount;
                arenaSettings.ArenaItems_NoSpear = this.AT_ItemNoSpear;

                arenaSettings.ArenaBonus_InstantDeath = this.AB_InstantDeath;
                arenaSettings.ArenaBonus_ExtraItemUses = this.AB_ExtraItemUses;
            }
        }

        public override Type GetDataType() => typeof(BTWArenaLobbyRessourceData);
    }
}