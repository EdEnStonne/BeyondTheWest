using UnityEngine;
using Menu.Remix.MixedUI;

public class BTWRemix : OptionInterface
{
    private const float basePosY = 550f;
    private const float spacingY = 25f;
    private const float textUpY = 4f;
    private const float columsSizeX = 320f;
    public override void Initialize()
    {
        base.Initialize();

        this.Tabs = new OpTab[] { new(this, "Character config"), new(this, "World config"), new(this, "Miscellaneous") };

        Tabs[0].AddItems(new UIelement[] {
            new OpLabel(20f, basePosY - spacingY * 0, "The Trailseeker", true) { description = "Trailseeker section" },

            new OpCheckBox(TrailseekerIgnorePoleToggle, new Vector2(20f, basePosY - spacingY * 1)) { description = TrailseekerIgnorePoleToggle.info.description },
            new OpLabel(50f, basePosY - spacingY * 1 + textUpY, "Ignore Poles button is toggle") { description = TrailseekerIgnorePoleToggle.info.description },


            new OpLabel(20f, basePosY - spacingY * 3, "The Spark", true) { description = "Spark section" },

            new OpCheckBox(DoSparkShockSlugs, new Vector2(20f, basePosY - spacingY * 4)) { description = DoSparkShockSlugs.info.description },
            new OpLabel(50f, basePosY - spacingY * 4 + textUpY, "Story mode friendly fire") { description = DoSparkShockSlugs.info.description },

            new OpCheckBox(DoDisplaySparkBattery, new Vector2(20f, basePosY - spacingY * 5)) { description = DoDisplaySparkBattery.info.description },
            new OpLabel(50f, basePosY - spacingY * 5 + textUpY, "Display Spark Charge UI") { description = DoDisplaySparkBattery.info.description },

            new OpCheckBox(SparkRiskyOvercharge, new Vector2(20f, basePosY - spacingY * 6)) { description = SparkRiskyOvercharge.info.description },
            new OpLabel(50f, basePosY - spacingY * 6 + textUpY, "Risky Overcharge") { description = SparkRiskyOvercharge.info.description },

            new OpCheckBox(SparkDeadlyOvercharge, new Vector2(20f, basePosY - spacingY * 7)) { description = SparkDeadlyOvercharge.info.description },
            new OpLabel(50f, basePosY - spacingY * 7 + textUpY, "Deadly Overcharge") { description = SparkDeadlyOvercharge.info.description },


            new OpLabel(20f, basePosY - spacingY * 9, "The Core", true) { description = "Core section" },

            new OpCheckBox(Core0GSpecialButton, new Vector2(20f, basePosY - spacingY * 10)) { description = Core0GSpecialButton.info.description },
            new OpLabel(50f, basePosY - spacingY * 10 + textUpY, "Core ability on special button") { description = Core0GSpecialButton.info.description },

            // new OpCheckBox(DoCore0GResetOnLanding, new Vector2(20f, 210f)) { description = DoCore0GResetOnLanding.info.description },
            // new OpLabel(50f, 214f, "Zero G reset on landing") { description = DoCore0GResetOnLanding.info.description },

        });

        Tabs[1].AddItems(new UIelement[] {
            // new OpLabel(20f, 550f, "Day / Night Cycle", true) { description = "Day / Night Cycle section" },

            // new OpCheckBox(EnableNightCycle, new Vector2(20f, 510f)) { description = EnableNightCycle.info.description },
            // new OpLabel(50f, 514f, "Enable Day / Night Cycle") { description = EnableNightCycle.info.description },

            // new OpCheckBox(EnableNightBubbles, new Vector2(20f, 470f)) { description = EnableNightBubbles.info.description },
            // new OpLabel(50f, 474f, "Night Timer Display") { description = EnableNightBubbles.info.description },

            // new OpCheckBox(StarveAtNight, new Vector2(20f, 430f)) { description = StarveAtNight.info.description },
            // new OpLabel(50f, 434f, "Food Mechanic") { description = StarveAtNight.info.description },

            // new OpCheckBox(TireAtNight, new Vector2(20f, 390f)) { description = TireAtNight.info.description },
            // new OpUpdown(TireNightCycleCount, new Vector2(50f, 390f), 70f) { description = TireNightCycleCount.info.description },
            // new OpLabel(130f, 394f, "Tiredness Mechanic") { description = TireAtNight.info.description },
        });

        Tabs[2].AddItems(new UIelement[] {
            new OpLabel(20f, basePosY - spacingY * 0, "Competitive Arena", true) { description = "Competitive Arena section" },

            new OpUpdown(ItemSpawnMultiplier, new Vector2(20f, basePosY - spacingY * 1), 100f) { description = ItemSpawnMultiplier.info.description },
            new OpLabel(130f, basePosY - spacingY * 1 + textUpY, "Competitive Item Spawn") { description = ItemSpawnMultiplier.info.description },

            // new OpCheckBox(DoItemSpawnScalePerPlayers, new Vector2(20f, 470f)) { description = DoItemSpawnScalePerPlayers.info.description },
            // new OpUpdown(ItemSpawnMultiplierPerPlayers, new Vector2(50f, 470f), 100f) { description = ItemSpawnMultiplierPerPlayers.info.description },
            // new OpLabel(160f, 474f, "Competitive Item Spawn Per Player") { description = DoItemSpawnScalePerPlayers.info.description },
        });
    }

    public static BTWRemix instance = new();

    public static Configurable<float> ItemSpawnMultiplier = instance.config.Bind("ItemSpawnMultiplier", 1f,
        new ConfigurableInfo(
            "How much times the arena will attempt to spawn items in Competitive.  Default 1.0.",
            new ConfigAcceptableRange<float>(0f, 50.0f)
        )
    );
    // public static Configurable<float> ItemSpawnMultiplierPerPlayers = instance.config.Bind("ItemSpawnMultiplierPerPlayers", 1f, 
    //     new ConfigurableInfo(
    //         "How much times the arena will attempt to spawn items in Competitive per players.  Default 1.0.",
    //         new ConfigAcceptableRange<float>(0f, 50.0f)
    //     )
    // );
    // public static Configurable<bool> DoItemSpawnScalePerPlayers = instance.config.Bind("ItemSpawnScalePlayers", true, 
    //     new ConfigurableInfo("If the amount of items scales with the amount of players.  Default true.")
    // );
    
    // Trailseeker
    public static Configurable<bool> TrailseekerIgnorePoleToggle = instance.config.Bind("TrailseekerIgnorePoleToggle", true, 
        new ConfigurableInfo("If the \"ignore poles\" feature of Trailseeker (special) is toggle. False makes it hold to active.  Default true.")
    );

    // Spark
    public static Configurable<bool> DoSparkShockSlugs = instance.config.Bind("DoSparkShockSlugs", false, 
        new ConfigurableInfo("If the Spark can damage other players using his electric abilities in story mode.  Default false.")
    );
    public static Configurable<bool> DoDisplaySparkBattery = instance.config.Bind("DoDisplaySparkBattery", true, 
        new ConfigurableInfo("If the Spark displays a battery UI to indicate his charge. Useful if you can't tell if your slug has enough charge for an action.  Default true.")
    );
    public static Configurable<bool> SparkRiskyOvercharge = instance.config.Bind("SparkRiskyOvercharge", true, 
        new ConfigurableInfo("Change if being overcharge has a chance to stun The Spark. Default: true.")
    );
    public static Configurable<bool> SparkDeadlyOvercharge = instance.config.Bind("SparkDeadlyOvercharge", true, 
        new ConfigurableInfo("Change if going above overcharge will kill The Spark. If false, it'll stun the spark instead. Default: true.")
    );

    // Core
    public static Configurable<bool> DoCore0GResetOnLanding = instance.config.Bind("DoCore0GResetOnLanding", true, 
        new ConfigurableInfo("If the Core 0G ability automatically stops after landing. Default true.")
    );
    public static Configurable<bool> Core0GSpecialButton = instance.config.Bind("Core0GSpecialButton", false, 
        new ConfigurableInfo("If the leaping ability of the Core should be put on the special button instead of the jump button, allowing it to jump freely. Default false.")
    );

    // Night cycle
    public static Configurable<bool> EnableNightBubbles = instance.config.Bind("EnableNightBubbles", true, 
        new ConfigurableInfo("Enables blue bubbles to indicate the night/rain timer. Default true.")
    );
    public static Configurable<bool> StarveAtNight = instance.config.Bind("StarveAtNight", true, 
        new ConfigurableInfo("Remove food as if you were sleeping when spending a whole cycle outside. Default true.")
    );
    public static Configurable<bool> TireAtNight = instance.config.Bind("TireAtNight", true, 
        new ConfigurableInfo("Makes your character tired if you spend too much cycles without sleeping. Default true.")
    );
    public static Configurable<int> TireNightCycleCount = instance.config.Bind("TireNightCycleCount", 2, 
        new ConfigurableInfo("How many cycles you have to spend without sleeping until your character gets a level of tiredness.  Default 2.",
        new ConfigAcceptableRange<int>(1, 100))
    );
    public static Configurable<bool> EnableNightCycle = instance.config.Bind("EnableNightCycle", true,
        new ConfigurableInfo("Enables night cycle system, making day and night/rain loop. Doesn't work with Watcher. Default true.")
    );
    
    // Meadow Configuration
    public static Configurable<bool> MeadowTrailseekerAlteredMovementTech = instance.config.Bind("MeadowTrailseekerAlteredMovementTech", true, 
        new ConfigurableInfo("Change the velocity of certain vanilla movement tech of the Trailseeker.  Default: true.")
    );
    public static Configurable<int> MeadowTrailseekerPoleClimbBonus = instance.config.Bind("MeadowTrailseekerPoleClimbBonus", 4, 
        new ConfigurableInfo("How much bonus frame the Trailseeker will slide up a pole. Default: 4 frames.",
        new ConfigAcceptableRange<int>(0, 40))
    );
    public static Configurable<int> MeadowTrailseekerMaxWallClimb = instance.config.Bind("MeadowTrailseekerMaxWallClimb", 3, 
        new ConfigurableInfo("How much times the Trailseeker can use Wall Climbing (not in a row !) without landing. Default: 3 times.",
        new ConfigAcceptableRange<int>(1, int.MaxValue))
    );
    public static Configurable<int> MeadowTrailseekerWallGripTimer = instance.config.Bind("MeadowTrailseekerWallGripTimer", 15,
        new ConfigurableInfo("How long (in seconds) the Trailseeker can hold its grip to a wall. Default: 15s.",
        new ConfigAcceptableRange<int>(1, int.MaxValue))
    );
    
    public static Configurable<int> MeadowCoreMaxEnergy = instance.config.Bind("MeadowCoreMaxEnergy", 1200, 
        new ConfigurableInfo("The maximum energy capacity of the Core.  Default: 1200e.",
        new ConfigAcceptableRange<int>(1, int.MaxValue))
    );
    public static Configurable<int> MeadowCoreRegenEnergy = instance.config.Bind("MeadowCoreRegenEnergy", 40, 
        new ConfigurableInfo("The natural energy regeneration of the Core, in energy unit per second.  Default: 40e/s.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
    public static Configurable<int> MeadowCoreOxygenEnergyUsage = instance.config.Bind("MeadowCoreOxygenEnergyUsage", 100, 
        new ConfigurableInfo("The energy convertion per cent of oxygen when going underwater.  Default: 250e.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
    public static Configurable<int> MeadowCoreAntiGravityCent = instance.config.Bind("MeadowCoreAntiGravityCent", 85, 
        new ConfigurableInfo("How much the gravity is reduced (in pourcent) when activating the anti-gravity ability. Default: 85(%).",
        new ConfigAcceptableRange<int>(0, 100))
    );
    public static Configurable<int> MeadowCoreMaxLeap = instance.config.Bind("MeadowCoreMaxLeap", 2, 
        new ConfigurableInfo("How many leaps The Core can do mid-air before landing. Putting it to 0 will only allow leaps on the ground. Default: 2 leaps.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
    public static Configurable<bool> MeadowCoreShockwave = instance.config.Bind("MeadowCoreShockwave", true,
        new ConfigurableInfo("Change if the Core will do a shockwave when charging a leap for too long. Default: true.")
    );
    
    public static Configurable<int> MeadowSparkMaxCharge = instance.config.Bind("MeadowSparkMaxCharge", 100, 
        new ConfigurableInfo("The \"maximum\" charge of the Spark. Going above it will Overcharge the Spark. Default: 100c.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
    public static Configurable<int> MeadowSparkAdditionnalOvercharge = instance.config.Bind("MeadowSparkAdditionnalOvercharge", 100, 
        new ConfigurableInfo("The additional overcharge of the Spark. Going above it will kill The Spark. Putting it to 0 disable overcharge. Default: +100c.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
    public static Configurable<int> MeadowSparkChargeRegenerationMult = instance.config.Bind("MeadowSparkChargeRegenerationMult", 4, 
        new ConfigurableInfo("An arbitrary multiplier of The Spark charge regeneration. Default: 4.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
    public static Configurable<int> MeadowSparkMaxElectricBounce = instance.config.Bind("MeadowSparkElectricBounce", 1, 
        new ConfigurableInfo("How many electric bounce The Spark can do before landing. Putting it to 0 disables it. Default: 1 bounce.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
    public static Configurable<bool> MeadowSparkDoDischargeDamage = instance.config.Bind("MeadowSparkDoDischargeDamage", true, 
        new ConfigurableInfo("Change if the Discharge ability does damage to player. False means it'll only stun. Default: true.")
    );
    public static Configurable<bool> MeadowSparkRiskyOvercharge = instance.config.Bind("MeadowSparkRiskyOvercharge", true, 
        new ConfigurableInfo("Change if being overcharge has a chance to stun The Spark. Default: true.")
    );
    public static Configurable<bool> MeadowSparkDeadlyOvercharge = instance.config.Bind("MeadowSparkDeadlyOvercharge", true, 
        new ConfigurableInfo("Change if going above overcharge will kill The Spark. If false, it'll stun the spark instead. Default: true.")
    );
    
    public static Configurable<int> MeadowArenaLivesAmount = instance.config.Bind("MeadowArenaLivesAmount", 1, 
        new ConfigurableInfo("How many lives player have. Putting it to 0 disables it. Default: 1 life.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
    public static Configurable<int> MeadowArenaLivesReviveTime = instance.config.Bind("MeadowArenaLivesReviveTime", 15, 
        new ConfigurableInfo("The time in seconds to revive a player after they die. Default : 15s",
        new ConfigAcceptableRange<int>(1, int.MaxValue))
    );
    public static Configurable<int> MeadowArenaLivesAdditionalReviveTime = instance.config.Bind("MeadowArenaLivesAdditionalReviveTime", 0, 
        new ConfigurableInfo("The additional time in seconds to revive a player after each time they die. Default : 0s",
        new ConfigAcceptableRange<int>(int.MinValue, int.MaxValue))
    );
    public static Configurable<bool> MeadowArenaLivesStrict = instance.config.Bind("MeadowArenaLivesStrict", true, 
        new ConfigurableInfo("Change if the Arena Lives system will strictly enforce lives. This can be a solution against Revivify Meadow. Default: true.")
    );
    public static Configurable<bool> MeadowArenaLivesBlockWin = instance.config.Bind("MeadowArenaLivesBlockWin", true, 
        new ConfigurableInfo("Change if the Arena Lives system will wait for everyone to revive before closing the arena session. Default: true.")
    );
    public static Configurable<bool> MeadowArenaLivesReviveFromAbyss = instance.config.Bind("MeadowArenaLivesReviveFromAbyss", false, 
        new ConfigurableInfo("Change if the Arena Lives system will try reviving destroyed bodies. Default: true.")
    );
    public static Configurable<int> MeadowArenaLivesRespawnShieldDuration = instance.config.Bind("MeadowArenaLivesRespawnShieldDuration", 10, 
        new ConfigurableInfo("How long in second a player will have respawn protection. Putting it to 0 disables it. Default: 10s.",
        new ConfigAcceptableRange<int>(0, int.MaxValue))
    );
}