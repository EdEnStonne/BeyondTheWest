using System.Runtime.CompilerServices;
using SlugBase.Features;
using UnityEngine;
using System;
using MonoMod.Cil;
using RWCustom;
using BeyondTheWest;
using Mono.Cecil.Cil;
using System.Linq;
using HUD;
using BeyondTheWest.MeadowCompat;

public class RainTimerAddition
{
    public static ConditionalWeakTable<RainMeter, NightTimeRainMeter> cwtNightTimeRM = new();
    public static ConditionalWeakTable<RainCycle, NightTimeRainCycle> cwtNightTimeRC = new();
    public static GameData<int> Tiredness = new(null);
    public static void ApplyHooks()
    {
        On.HUD.RainMeter.ctor += AddBlueCircles;
        On.HUD.RainMeter.Update += UpdateBlueCircles;
        On.HUD.RainMeter.Draw += DrawBlueCircles;

        On.RainCycle.ctor += AddNightCycle;
        On.RainCycle.Update += UpdateNightCycle;

        On.RainWorldGame.GoToDeathScreen += ResetExtraCycles;
        On.ShelterDoor.DoorClosed += OnResting;

        IL.RoomCamera.UpdateDayNightPalette += NightILAdapt;
    }

    // Class
    public class NightTimeRainMeter : HudPart
    {
        public NightTimeRainMeter(RainMeter rainMeter, HUD.HUD hud, FContainer fContainer) : base(hud)
        {
            BTWPlugin.Log("NightTimeRainMeter ctor start");
            this.rainMeter = rainMeter;
            this.fContainer = fContainer;
            SetNightCircles();
            SetDayCircles();

            // Plugin.logger.LogDebug("ctor done");

            InitFades();
            InitFatigueCircle();
            
            if (Player != null && NightRainCycle != null)
            {
                if (Player.Malnourished)
                {
                    NightRainCycle.starvingLevel = 2;
                }
            }
            BTWPlugin.Log("NightTimeRainMeter ctor done !");
        }

        public void ResetEmptyCircle()
        {
            for (int i = 0; i < bcircles.Length; i++)
            {
                bcircles[i].thickness = -1f;
                bcircles[i].snapGraphic = HUDCircle.SnapToGraphic.Circle4;
                bcircles[i].snapRad = 2f;
                bcircles[i].snapThickness = -1f;
            }
        }
        public void ResetFullCircle()
        {
            for (int i = 0; i < bcircles.Length; i++)
            {
                bcircles[i].thickness = 3.5f;
                bcircles[i].snapGraphic = HUDCircle.SnapToGraphic.smallEmptyCircle;
                bcircles[i].snapRad = 3f;
                bcircles[i].snapThickness = 1f;
            }
        }
        public override void Update()
        {
            base.Update();
            bool flag = ModManager.MMF
                && MoreSlugcats.MMF.cfgHideRainMeterNoThreat.Value
                && ThisWorld.rainCycle.RegionHidesTimer
                && ((rainMeter.hud.owner as Player).room == null || (rainMeter.hud.owner as Player).room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.DayNight) == 0f);

            for (int i = 0; i < bcircles.Length; i++)
            {
                bcircles[i].Update();
                if ((rainMeter.fade > 0f || rainMeter.lastFade > 0f) && (NightRainCycle.NightLeft != -1f || NightRainCycle.RainApproaching > 0f))
                {
                    float num = (float)i / (bcircles.Length - 1);
                    float value; float num2;
                    if (NightRainCycle.NightLeft != -1f)
                    {
                        value = Mathf.InverseLerp((float)i / bcircles.Length, (float)(i + 1) / bcircles.Length, NightRainCycle.NightLeft);
                        num2 = Mathf.InverseLerp(0.5f, 0.475f, Mathf.Abs(0.5f - Mathf.InverseLerp(0.035f, 1f, value)));
                        // if (i == bcircles.Length - 1) { Plugin.logger.LogDebug("Night Timer (n): " + NightLeft + " / " + value + " / " + num2); }
                    }
                    else
                    {
                        value = Mathf.InverseLerp((float)(bcircles.Length - i - 1) / bcircles.Length,
                            (float)(bcircles.Length - i - 0) / bcircles.Length,
                            NightRainCycle.RainApproaching);
                        num2 = Mathf.InverseLerp(1.0f, 0.85f, value);
                        // if (i == bcircles.Length - 1) { Plugin.logger.LogDebug("Night Timer (ra): " + RainApproaching + " / " + value + " / " + num2); }
                    }

                    if (flag)
                    {
                        bcircles[i].rad = (3f * Mathf.Pow(rainMeter.fade, 2f) + Mathf.InverseLerp(0.075f, 0f, Mathf.Abs(1f - num - Mathf.Lerp((1f - NightRainCycle.NightLeft) * rainMeter.fade - 0.075f, 1.075f, Mathf.Pow(rainMeter.plop, 0.85f)))) * 2f * rainMeter.fade) * Mathf.InverseLerp(0f, 0.035f, 1f);
                        bcircles[i].thickness = 1f;
                        bcircles[i].snapGraphic = HUDCircle.SnapToGraphic.smallEmptyCircle;
                        bcircles[i].snapRad = 3f;
                        bcircles[i].snapThickness = 1f;
                    }
                    else
                    {
                        bcircles[i].rad = ((2f + num2) * Mathf.Pow(rainMeter.fade, 2f) + Mathf.InverseLerp(0.075f, 0f, Mathf.Abs(1f - num - Mathf.Lerp((1f - NightRainCycle.NightLeft) * rainMeter.fade - 0.075f, 1.075f, Mathf.Pow(rainMeter.plop, 0.85f)))) * 2f * rainMeter.fade) * Mathf.InverseLerp(0f, 0.035f, value);
                        if (num2 == 0f)
                        {
                            bcircles[i].thickness = -1f;
                            bcircles[i].snapGraphic = HUDCircle.SnapToGraphic.Circle4;
                            bcircles[i].snapRad = 2f;
                            bcircles[i].snapThickness = -1f;
                        }
                        else
                        {
                            bcircles[i].thickness = Mathf.Lerp(3.5f, 1f, num2);
                            bcircles[i].snapGraphic = HUDCircle.SnapToGraphic.smallEmptyCircle;
                            bcircles[i].snapRad = 3f;
                            bcircles[i].snapThickness = 1f;
                        }
                    }
                    // bcircles[i].rad *= 0.85f;
                    bcircles[i].pos = rainMeter.pos
                        + Custom.DegToVec((1f - (float)i / bcircles.Length) * 360f * Custom.SCurve(Mathf.Pow(rainMeter.fade, 1.5f - num), 0.6f))
                        * (rainMeter.hud.karmaMeter.Radius + 4f + num2 + 4f * rainMeter.tickPulse);
                }
                else
                {
                    bcircles[i].rad = 0f;
                }
            }
            if (ThisWorld.rainCycle.preTimer > 0 && (rainMeter.fade > 0f || rainMeter.lastFade > 0f) && !flag && BTWRemix.EnableNightBubbles.Value)
            {
                for (int i = 0; i < rainMeter.circles.Length; i++)
                {
                    float num = (float)i / (rainMeter.circles.Length - 1);
                    float value = Mathf.InverseLerp((float)(rainMeter.circles.Length - i - 1) / rainMeter.circles.Length,
                        (float)(rainMeter.circles.Length - i - 0) / rainMeter.circles.Length,
                        1f - NightRainCycle.RainRetreating);
                    float num2 = Mathf.InverseLerp(1.0f, 0.85f, value);
                    // if (i == rainMeter.circles.Length - 1) { Plugin.logger.LogDebug("Normal Timer : " + RainRetreating + " / " + value + " / " + num2); }

                    rainMeter.circles[i].rad = ((2f + num2) * Mathf.Pow(rainMeter.fade, 2f) + Mathf.InverseLerp(0.075f, 0f, Mathf.Abs(1f - num - Mathf.Lerp((1f - NightRainCycle.NightLeft) * rainMeter.fade - 0.075f, 1.075f, Mathf.Pow(rainMeter.plop, 0.85f)))) * 2f * rainMeter.fade) * Mathf.InverseLerp(0f, 0.035f, value);
                    if (num2 == 0f)
                    {
                        rainMeter.circles[i].thickness = -1f;
                        rainMeter.circles[i].snapGraphic = HUDCircle.SnapToGraphic.Circle4;
                        rainMeter.circles[i].snapRad = 2f;
                        rainMeter.circles[i].snapThickness = -1f;
                    }
                    else
                    {
                        rainMeter.circles[i].thickness = Mathf.Lerp(3.5f, 1f, num2);
                        rainMeter.circles[i].snapGraphic = HUDCircle.SnapToGraphic.smallEmptyCircle;
                        rainMeter.circles[i].snapRad = 3f;
                        rainMeter.circles[i].snapThickness = 1f;
                    }
                }
            }
            if (foodUpdate && hud.foodMeter != null) {
                if (hud.foodMeter.showSurvLim != hud.foodMeter.survLimTo)
                {
                    hud.foodMeter.survivalLimit = (int)Math.Floor(hud.foodMeter.showSurvLim);
                    hud.foodMeter.MoveSurvivalLimit(hud.foodMeter.showSurvLim, false);
                }
                else
                {
                    hud.foodMeter.survivalLimit = Player.slugcatStats.foodToHibernate;
                    hud.foodMeter.MoveSurvivalLimit(Player.slugcatStats.foodToHibernate, false);
                    foodUpdate = false;
                }
            }
            if (NightRainCycle != null && NightRainCycle.faintingCounter > 0)
            {
                if (blackFade == null) { InitFades(); }
                float prop = Mathf.Clamp01(1f - (Math.Abs((float)(NightRainCycle.faintingCounter - NightRainCycle.faintingTime / 2f) / NightRainCycle.faintingTime) * 2f));
                BTWPlugin.Log(NightRainCycle.faintingCounter + "/" + prop);
                blackFade.alpha = Math.Min(prop * 1.5f, 1f);
                blackFade.scaleX = ThisWorld.game.rainWorld.screenSize.x * Mathf.Lerp(0.25f, 1.5f, prop);
                blackFade.scaleY = ThisWorld.game.rainWorld.screenSize.y * Mathf.Lerp(0.25f, 1.5f, prop);

                if (!ThisWorld.game.GamePaused)
                {
                    NightRainCycle.faintingCounter--;
                }
            }
            else if (blackFade.alpha > 0f)
            {
                if (blackFade == null) { InitFades(); }
                blackFade.alpha = 0f;
            }
            if (hud.karmaMeter != null && NightRainCycle != null)
            {
                if (glowfatigueCircle.Count() == 0) { InitFatigueCircle(); }
                for (int i = 0; i < glowfatigueCircle.Length; i++)
                {
                    float posRatio = (float)i / (glowfatigueCircle.Length - 1);
                    float radValue = Mathf.InverseLerp(i, i + 1, NightRainCycle.FaintLevel);

                    glowfatigueCircle[i].alpha = radValue * (float)Math.Pow(hud.karmaMeter.fade, 1/2);
                    glowfatigueCircle[i].height = 12f * (float)Math.Pow(radValue, 1/2) * hud.karmaMeter.fade;
                    glowfatigueCircle[i].width = 12f * (float)Math.Pow(radValue, 1/2) * hud.karmaMeter.fade;
                    glowfatigueCircle[i].x = hud.karmaMeter.pos.x + Mathf.Lerp(-hud.karmaMeter.rad, hud.karmaMeter.rad, posRatio) * 0.75f * (float)Math.Pow(hud.karmaMeter.fade, 1/2);
                    glowfatigueCircle[i].y = hud.karmaMeter.pos.y + 20f + hud.karmaMeter.rad;
                }
            }
            // Plugin.logger.LogDebug(ThisWorld.rainCycle.dayNightCounter);

        }
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (!BTWRemix.EnableNightBubbles.Value) { ResetEmptyCircle(); }
            else
            {
                for (int i = 0; i < bcircles.Length; i++)
                {
                    bcircles[i].Draw(timeStacker);
                }
            }
        }

        public void InitFades()
        {
            if (ThisWorld != null && ThisWorld.game != null && ThisWorld.game.rainWorld != null)
            {
                if (blackFade == null)
                {
                    blackFade = new FSprite("Futile_White", true)
                    {
                        color = new Color(0f, 0f, 0f),
                        x = ThisWorld.game.rainWorld.screenSize.x / 2f,
                        y = ThisWorld.game.rainWorld.screenSize.y / 2f,
                        scaleX = ThisWorld.game.rainWorld.screenSize.x,
                        scaleY = ThisWorld.game.rainWorld.screenSize.y,
                        alpha = 0f,
                        shader = ThisWorld.game.rainWorld.Shaders["EdgeFade"]
                    };
                    Futile.stage.AddChild(blackFade);
                }
            }
        }
        public void InitFatigueCircle()
        {
            glowfatigueCircle = new FSprite[5];
            for (int i = 0; i < glowfatigueCircle.Length; i++)
            {
                glowfatigueCircle[i] = new("Futile_White", true)
                {
                    shader = hud.rainWorld.Shaders["FlatLight"],
                    color = new Color(0.10f, 0.35f, 0.55f),
                };
                fContainer.AddChild(glowfatigueCircle[i]);
            }
        }
        public void SetNightCircles()
        {
            if (NightRainCycle != null)
            {
                if (bcircles != null && bcircles.Length > 0)
                {
                    for (int i = 0; i < bcircles.Length; i++)
                    {
                        bcircles[i].ClearSprite();
                    }
                }
                ntickPerCircle = 1200;
                int num = NightRainCycle.nightCycleTotalTick / ntickPerCircle;
                if (num > 30)
                {
                    num = 30;
                    ntickPerCircle = NightRainCycle.nightCycleTotalTick / num;
                }

                bcircles = new HUDCircle[num];
                for (int i = 0; i < bcircles.Length; i++)
                {
                    bcircles[i] = new HUDCircle(hud, HUDCircle.SnapToGraphic.Circle4, fContainer, 0)
                    {
                        // forceColor = new Color(0.25f, 0.25f, 0.85f),
                        color = 3,
                        thickness = -1f,
                        snapRad = 2f,
                        snapThickness = -1f,
                    };
                    // bcircles[i].circleShader = bcircles[i].basicShader;
                }
                BTWPlugin.Log("Night Cycle time : " + NightRainCycle.nightCycleTotalTick + " / " + ntickPerCircle + " / " + bcircles.Length);
            }
        }
        public void SetDayCircles()
        {
            if (rainMeter.circles != null && rainMeter.circles.Length > 0)
            {
                for (int i = 0; i < rainMeter.circles.Length; i++)
                {
                    rainMeter.circles[i].ClearSprite();
                }
            }
            rainMeter.timePerCircle = 1200;
            int num = ThisWorld.rainCycle.cycleLength / rainMeter.timePerCircle;
            if (num > 30)
            {
                num = 30;
                rainMeter.timePerCircle = ThisWorld.rainCycle.cycleLength / num;
            }
            rainMeter.circles = new HUDCircle[num];
            for (int i = 0; i < rainMeter.circles.Length; i++)
            {
                rainMeter.circles[i] = new HUDCircle(hud, HUDCircle.SnapToGraphic.smallEmptyCircle, fContainer, 0);
            }
            BTWPlugin.Log("Day Cycle time : " + ThisWorld.rainCycle.cycleLength + " / " + rainMeter.timePerCircle + " / " + rainMeter.circles.Length);
        }
        public void ShowRegionAndCycle()
        {
            if (this.Player != null && this.Player.room != null && !this.Player.room.world.singleRoomWorld && this.Player.room.world.region != null)
            {
                // From Subregion tracker
                int num = 0;
                for (int i = 1; i < this.Player.room.world.region.subRegions.Count; i++)
                {
                    if (this.Player.room.abstractRoom.subregionName == this.Player.room.world.region.subRegions[i])
                    {
                        num = i;
                        break;
                    }
                }
                string s = this.Player.room.world.region.subRegions[num];
                if (num < this.Player.room.world.region.altSubRegions.Count && this.Player.room.world.region.altSubRegions[num] != null)
                {
                    s = this.Player.room.world.region.altSubRegions[num];
                }
                int num2 = this.Player.room.game.GetStorySession.saveState.cycleNumber;
                if (this.Player.room.game.StoryCharacter == SlugcatStats.Name.Red && !Custom.rainWorld.ExpeditionMode)
                {
                    num2 = RedsIllness.RedsCycles(this.Player.room.game.GetStorySession.saveState.redExtraCycles) - num2;
                }
                hud.textPrompt.AddMessage(string.Concat(new string[]
                {
                    hud.textPrompt.hud.rainWorld.inGameTranslator.Translate("Cycle"),
                    " ",
                    num2.ToString(),
                    " ~ ",
                    hud.textPrompt.hud.rainWorld.inGameTranslator.Translate(s)
                }), 0, 160, false, true);
            }

        }
        public void UpdateFoodLimit()
        {
            if (hud.foodMeter != null && Player.slugcatStats.foodToHibernate != hud.foodMeter.survivalLimit)
            {
                hud.foodMeter.MoveSurvivalLimit(Player.slugcatStats.foodToHibernate, true);
                foodUpdate = true;
            }
        }

        public HUDCircle[] bcircles;
        public FSprite blackFade;
        public FSprite[] glowfatigueCircle;

        public RainMeter rainMeter;
        public FContainer fContainer;
        public int ntickPerCircle = 1200;
        public bool foodUpdate = false;
        public World ThisWorld
        {
            get
            {
                return this.Player.abstractCreature.world;
            }
        }
        public Player Player
        {
            get
            {
                return hud.owner as Player;
            }
        }
        NightTimeRainCycle NightRainCycle
        {
            get
            {
                if (ThisWorld != null && ThisWorld.rainCycle != null && cwtNightTimeRC.TryGetValue(ThisWorld.rainCycle, out var NTRC))
                {
                    return NTRC;
                }
                BTWPlugin.Log("Can't find NightTimeRainCycle ! "+ (ThisWorld != null) +"/"+ (ThisWorld != null && ThisWorld.rainCycle != null) +"/false");
                return null;
            }
        }
    }
    public class NightTimeRainCycle
    {
        public NightTimeRainCycle(RainCycle rainCycle, World world)
        {
            BTWPlugin.Log("NightTimeRainCycle ctor start with : " + rainCycle +"/"+ world);
            BTWPlugin.Log("Cycle settings : " + world.game.rainWorld.setup.cycleTimeMin + " / " + world.game.rainWorld.setup.cycleTimeMax);
            this.rainCycle = rainCycle;
            this.world = world;
            SetNightTimer();

            if (rainCycle.maxPreTimer > 0)
            {
                NightIntermission(rainCycle.maxPreTimer);
                cyclePassed = -1;
            }
            BTWPlugin.Log("NightTimeRainCycle ctor done !");
        }

        public void Update()
        {
            if (goBlink > 0 && world.game.Players.Count > 0 && world.game.Players[0].realizedCreature != null && world.game.Players[0].realizedCreature is Player mj && mj.stun <= 0)
            {
                for (int i = 0; i < world.game.Players.Count; i++)
                {
                    if (world.game.Players[i].realizedCreature != null && world.game.Players[i].realizedCreature is Player pl)
                    {
                        pl.Blink(goBlink);
                    }
                }
                goBlink = 0;
            }
            if (rainCycle.cycleLength < rainCycle.timer)
            {
                nightTimer++;
            }
            else if (rainCycle.preTimer == 0f && nightTimer != 0)
            {
                nightTimer = 0;
            }
            if (TimeUntilNextDay <= nightIntermissiontime && !outOfNight && NightLeft != -1f)
            {
                BTWPlugin.logger.LogDebug("Resetting rain");
                if (world.game.globalRain != null)
                {
                    world.game.globalRain.ResetRain();
                    float floodLevel = 0;
                    for (int i = 0; i < world.abstractRooms.Length; i++)
                    {
                        AbstractRoom ar = world.abstractRooms[i];
                        float y = ar.mapPos != null ? ar.mapPos.y : 0f;
                        if (ar.realizedRoom != null)
                        {
                            y += (ar.realizedRoom.waterObject != null ? ar.realizedRoom.waterObject.originalWaterLevel : 0f)
                                + ar.realizedRoom.PixelHeight + 500f;
                        }
                        if (y > floodLevel) { floodLevel = y; }
                    }
                    floodLevel = Math.Min(world.game.globalRain.flood - 1, floodLevel);
                    world.game.globalRain.drainWorldFlood = floodLevel;
                }
                NightIntermission(TimeUntilNextDay);
            }
            if (outOfNight && RainRetreating > 0)
            {
                if (rainCycle.dayNightCounter > 0)
                {
                    rainCycle.dayNightCounter--;
                }
                if (BTWRemix.StarveAtNight.Value && cyclePassed >= 0 && world.game.Players.Count > 0 && world.game.Players[0].realizedCreature != null)
                {
                    Player mainPlayer = (Player)world.game.Players[0].realizedCreature;
                    SlugcatStats slugcatStats = mainPlayer.slugcatStats;
                    
                    if ((int)Math.Ceiling(slugcatStats.foodToHibernate * (1f - RainRetreating)) > foodTaken)
                    {
                        if (mainPlayer.FoodInStomach > 0)
                        {
                            foodTaken++;
                            mainPlayer.SubtractFood(1);
                            // mainPlayer.room.PlaySound(SoundID.HUD_Food_Meter_Deplete_Plop_A, mainPlayer.mainBodyChunk.pos, 1f, UnityEngine.Random.Range(0.75f, 1.5f));
                            BTWPlugin.Log("Getting a night snack at "+ RainRetreating +"/"+ (slugcatStats.foodToHibernate * (1f - RainRetreating)) +"/"+ foodTaken +"/"+ slugcatStats.foodToHibernate);
                        }
                    }
                }
            }
            if ((NightLeft == -1f && outOfNight && RainRetreating == 0) || TimeUntilNextDay == 0)
            {
                BTWPlugin.logger.LogDebug("A new day has dawned");
                outOfNight = false;
                rainCycle.dayNightCounter = 0;
                cyclePassed++;
                if (world.game.session is StoryGameSession && cyclePassed > 0)
                {
                    (world.game.session as StoryGameSession).saveState.cycleNumber++;
                }
                NightRainMeter?.ShowRegionAndCycle();

                if (BTWRemix.StarveAtNight.Value && world.game.Players.Count > 0 && world.game.Players[0].realizedCreature != null && world.game.Players[0].realizedCreature is Player mainPlayer)
                {
                    SlugcatStats slugcatStats = mainPlayer.slugcatStats;
                    if (foodTaken >= slugcatStats.foodToHibernate)
                    {
                        mainPlayer.SetMalnourished(false);
                        NightRainMeter?.UpdateFoodLimit();
                        starvingLevel = 0;
                    }
                    else
                    {
                        mainPlayer.room.PlaySound(SoundID.HUD_Food_Meter_Deplete_Plop_A, mainPlayer.mainBodyChunk.pos, 1.5f, 0.75f);
                        int NewHibFood = slugcatStats.foodToHibernate + slugcatStats.foodToHibernate - foodTaken;
                        if (NewHibFood > slugcatStats.maxFood)
                        {
                            additionnalfood += slugcatStats.maxFood - slugcatStats.foodToHibernate;
                            slugcatStats.foodToHibernate = slugcatStats.maxFood;
                            starvingLevel += NewHibFood - slugcatStats.maxFood;
                        }
                        else
                        {
                            additionnalfood += NewHibFood - slugcatStats.foodToHibernate;
                            slugcatStats.foodToHibernate = NewHibFood;
                        }
                        if (slugcatStats.foodToHibernate == slugcatStats.maxFood && !slugcatStats.malnourished)
                        {
                            mainPlayer.SetMalnourished(true);
                        }
                        NightRainMeter?.UpdateFoodLimit();
                        for (int i = 0; i < world.game.Players.Count; i++)
                        {
                            if (world.game.Players[i].realizedCreature != null && world.game.Players[i].realizedCreature is Player pl)
                            {
                                pl.Blink(100);
                                pl.slowMovementStun = Math.Max(pl.slowMovementStun, 100);
                                goBlink = 100;
                                pl.Stun(40);
                            }
                        }
                        BTWPlugin.Log("Ouch ! " + slugcatStats.foodToHibernate);
                    }
                    BTWPlugin.Log("Food to hibernate now : "+ slugcatStats.foodToHibernate);
                }
                foodTaken = 0;

                if (BTWRemix.TireAtNight.Value && cyclePassed > 0 && cyclePassed % BTWRemix.TireNightCycleCount.Value == 0 && tiredness < 3)
                {
                    tiredness += 1;
                }
            }
            if (faintCooldown <= 0)
            {
                CheckFainting();
            }
            else
            {
                faintCooldown--;
            }
            if (faintingCounter > 0 && faintingCounter == Mathf.Clamp(faintingCounter, faintingTime * 1 / 4, faintingTime * 3 / 4) && !timeSkip && !outOfNight && RainApproaching == 0f && RainRetreating == 0f)
            {
                BTWPlugin.Log("Fainting cycle skip !");
                timeSkip = true;
                if (NightLeft == -1f)
                {
                    rainCycle.timer = (int)Math.Min(rainCycle.cycleLength, rainCycle.timer + rainCycle.cycleLength * UnityEngine.Random.Range(0.1f, 0.4f));
                }
                else
                {
                    nightTimer = (int)Math.Min(nightCycleTotalTick, nightTimer + nightCycleTotalTick * UnityEngine.Random.Range(0.1f, 0.4f));
                }
            }
        }

        public void NightIntermission(int fade = -1)
        {
            if (fade < 0) { fade = nightIntermissiontime; }
            outOfNight = true;
            rainCycle.dayNightCounter = cyclePassed == -1 ? fade : Math.Min(rainCycle.dayNightCounter, fade);
            rainCycle.maxPreTimer = fade;
            rainCycle.preTimer = rainCycle.maxPreTimer;
            rainCycle.preCycleRainPulse_WaveA = 0f;
            rainCycle.preCycleRainPulse_WaveB = 0f;
            rainCycle.preCycleRainPulse_WaveC = 1.5707964f;
            world.game.globalRain.preCycleRainPulse_Scale = 1f;
            // ThisWorld.game.globalRain.DrainWorldFloodInit(new WorldCoordinate(0, 0, 0, -1));
            if (cyclePassed > 0)
            {
                float minutes; // from World ctor
                if (world.game.GetStorySession.characterStats.name == SlugcatStats.Name.Yellow ||
                    (ModManager.MSC && (
                        world.game.GetStorySession.characterStats.name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Rivulet ||
                        world.game.GetStorySession.characterStats.name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand ||
                        world.game.GetStorySession.characterStats.name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint
                )))
                {
                    minutes = Mathf.Lerp(world.game.rainWorld.setup.cycleTimeMin, world.game.rainWorld.setup.cycleTimeMax, 0.35f + 0.65f * Mathf.Pow(UnityEngine.Random.value, 1.2f)) / 60f;
                }
                else
                {
                    minutes = Mathf.Lerp(world.game.rainWorld.setup.cycleTimeMin, world.game.rainWorld.setup.cycleTimeMax, UnityEngine.Random.value) / 60f;
                }
                if (ModManager.MMF && MoreSlugcats.MMF.cfgNoRandomCycles.Value)
                {
                    minutes = world.game.rainWorld.setup.cycleTimeMax / 60f;
                }
                world.rainCycle.cycleLength = (int)(minutes * 40f * 60f);
                world.rainCycle.baseCycleLength = world.rainCycle.cycleLength;
                NightRainMeter?.SetDayCircles();
                SetNightTimer();
            }
        }
        public void SetNightTimer()
        {
            // Plugin.Log("Night timer on : "+ BTWFunc.NightCycleTime(this.world));
            nightCycleTotalTick = BTWFunc.NightCycleTime(this.world, this.rainCycle.cycleLength);
            if (world.game.rainWorld.setup.cycleTimeMax * 40 < nightIntermissiontime * 1.5f) { nightCycleTotalTick *= 2; }
            if (nightCycleTotalTick < nightIntermissiontime * 1.5f) { nightCycleTotalTick = (int)(nightIntermissiontime * 1.5f); }
            NightRainMeter?.SetNightCircles();
            // rainCycle.cycleLength = 6000;
            NightRainMeter?.SetDayCircles();
        }
        public void CheckFainting()
        {
            float chance = (float)(0.00010f * Math.Pow(FaintLevel, 2));
            if (FaintLevel >= 1f && UnityEngine.Random.value < chance)
            {
                int mvtStun = (int)(UnityEngine.Random.Range(20f, 100f) * Math.Pow(FaintLevel, 2));
                int stun = FaintLevel < 2f ? 0 : (int)(UnityEngine.Random.Range(20f, 120f) * FaintLevel);
                bool fainting = !outOfNight && RainApproaching == 0f && RainRetreating == 0f && FaintLevel >= 3f
                    && UnityEngine.Random.value < 0.01f * Math.Pow(FaintLevel -2f, 2);
                BTWPlugin.Log("Doc, i don't feel so good... (Faint level at :" + FaintLevel + ", mvtStun : "+ mvtStun + ", stun : "+ stun + ", fainting : "+ fainting +")");
                for (int i = 0; i < world.game.Players.Count; i++)
                {
                    if (world.game.Players[i].realizedCreature != null && world.game.Players[i].realizedCreature is Player pl)
                    {
                        if (fainting)
                        {
                            timeSkip = false;
                            pl.Stun(faintingTime);
                        }
                        else
                        {
                            if (stun > 0)
                            {
                                pl.Stun(stun);
                            }
                            pl.Blink(mvtStun + stun);
                            goBlink = mvtStun;
                            pl.slowMovementStun = Math.Max(pl.slowMovementStun, mvtStun + stun);
                        }
                        
                    }
                }
                faintingCounter = fainting ? faintingTime : 0;
                faintCooldown = (fainting ? faintingTime : mvtStun + stun) + 1200;
            }
        }

        public RainCycle rainCycle;
        public World world;
        public int nightCycleTotalTick;
        public bool outOfNight = false;
        public int nightTimer = 0;
        public int cyclePassed = 0;
        public int tiredness = 0;
        public int lastTiredness = 0;
        public int starvingLevel = 0;
        public int foodTaken = 0;
        public bool timeSkip = false;
        public int faintCooldown = 0;
        public float FaintLevel
        {
            get
            {
                return Mathf.Clamp(
                    -0.5f
                    + starvingLevel * 0.5f
                    + ExtraCycles * 0.1f
                    + (float)Math.Pow(tiredness, 2) / 2f
                , 0, 5);
            }
        }
        public int faintingCounter = 0;
        public int additionnalfood = 0;
        public readonly int nightIntermissiontime = 3000;
        public readonly int faintingTime = 800;
        public int goBlink = 0;
        public float RainApproaching
        {
            get
            {
                return Mathf.InverseLerp(2400f, 0f, (float)rainCycle.TimeUntilRain);
            }
        }
        public float RainRetreating
        {
            get
            {
                if (rainCycle.maxPreTimer > 0f)
                {
                    return (float)rainCycle.preTimer / rainCycle.maxPreTimer;
                }
                return 0f;
            }
        }
        public float NightLeft
        {
            get
            {
                if (rainCycle.preTimer > 0f)
                {
                    return (float)rainCycle.preTimer / nightCycleTotalTick;
                }
                else if (rainCycle.AmountLeft > 0f)
                {
                    return -1f;
                }
                else
                {
                    return (float)TimeUntilNextDay / nightCycleTotalTick;
                }
            }
        }
        public int TimeUntilNextDay
        {
            get
            {
                if (rainCycle.preTimer > 0f)
                {
                    return rainCycle.preTimer;
                }
                else
                {
                    return nightCycleTotalTick - nightTimer;
                }
            }
        }
        public int ExtraCycles
        {
            get
            {
                return Math.Max(0, cyclePassed);
            }
        }
        public RainMeter RainMeter
        {
            get
            {
                if (world.game.cameras[0].hud != null && world.game.cameras[0].hud.rainMeter != null)
                {
                    return world.game.cameras[0].hud.rainMeter;
                }
                return null;
            }
        }
        public NightTimeRainMeter NightRainMeter
        {
            get
            {
                if (world.game.cameras[0].hud != null && world.game.cameras[0].hud.rainMeter != null && cwtNightTimeRM.TryGetValue(world.game.cameras[0].hud.rainMeter, out var NTRM))
                {
                    return NTRM;
                }
                return null;
            }
        }
    }

    // Function
    static int GetOrigAPallete(RoomCamera rC)
    {
        if (rC.room != null)
        {
            return rC.room.roomSettings.Palette;
        }
        return -1;
    }
    static int GetOrigBPallete(RoomCamera rC)
    {
        if (rC.room != null)
        {
            if (rC.room.roomSettings.fadePalette == null)
            {
                return -1;
            }
            else
            {
                return rC.room.roomSettings.fadePalette.palette;
            }
        }
        return -1;
    }
    static float GetOrigPalleteFade(RoomCamera rC)
    {
        if (rC.room != null)
        {
            if (rC.room.roomSettings.fadePalette == null)
            {
                return -1;
            }
            else
            {
                return rC.room.roomSettings.fadePalette.fades[rC.currentCameraPosition];
            }
        }
        return -1;
    }
    static void SetOrigPallete(RoomCamera rC)
    {
        if (rC.room != null)
        {
            if (rC.room.roomSettings.fadePalette == null)
            {
                rC.paletteBlend = 0f;
                rC.ChangeMainPalette(GetOrigAPallete(rC));
            }
            else
            {
                rC.ChangeBothPalettes(GetOrigAPallete(rC), GetOrigBPallete(rC), GetOrigPalleteFade(rC));
            }
        }
    }

    static bool MeadowAllowNightCycle()
    {
        if (BTWPlugin.meadowEnabled)
        {
            return !MeadowFunc.IsMeadowLobby();
        }
        return true;
    }
    static bool CanNightCycle(RainWorldGame game)
    {
        if (game == null) { return false; }
        if (BTWPlugin.meadowEnabled && MeadowAllowNightCycle()) { return false; }
        BTWPlugin.Log("Is there night today..? " );
        BTWPlugin.Log(
            "Let's check : " + 
            (BTWRemix.EnableNightCycle.Value) + "/" + 
            (game.session != null && (game.session is StoryGameSession)) + "/" + 
            (game.cameras[0] == null) + "/" + 
            (game.cameras[0] == null && game.cameras[0].hud == null) + "/" + 
            (game.cameras[0] != null && game.cameras[0].hud != null && game.cameras[0].hud.owner != null) + "/" + 
            (game.cameras[0] != null && game.cameras[0].hud != null && game.cameras[0].hud.owner is Player plo && !plo.isNPC) + "/" + 
            (game.cameras[0] != null && game.cameras[0].followAbstractCreature != null) + "/" + 
            (game.cameras[0] != null && game.cameras[0].game != null && game.cameras[0].game.world != null && !game.cameras[0].game.world.singleRoomWorld) + "/" + 
            (!(ModManager.Watcher && game.cameras[0] != null && game.cameras[0].hud != null && game.cameras[0].hud.owner != null && game.cameras[0].hud.owner is Player pla && (pla.rippleLevel >= 1f || pla.KarmaCap >= 100)))
        );
        if (
            BTWRemix.EnableNightCycle.Value && game.session != null && game.session is StoryGameSession
            // && (game.cameras[0] == null || (game.cameras[0] != null && game.cameras[0].hud == null) ||
            // (
            //     game.cameras[0] != null && game.cameras[0].hud == null && game.cameras[0].hud.owner != null
            //     && game.cameras[0].hud.owner is Player pl && !pl.isNPC
            //     && (
            //         // game.cameras[0].room != null &&
            //         game.cameras[0].followAbstractCreature != null &&
            //         // game.cameras[0].followAbstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat && 
            //         // game.cameras[0].followAbstractCreature.realizedCreature != null &&
            //         game.cameras[0].game.world != null &&
            //         !game.cameras[0].game.world.singleRoomWorld
            //     ) // RoomCam Update
            //     && !(ModManager.Watcher && (pl.rippleLevel >= 1f || pl.KarmaCap >= 100)) //NO watcher moment
            // ))
        )
        {
        BTWPlugin.Log("Night allowed !");
            return true;
        }
        BTWPlugin.Log("No night today !");
        return false;
    }

    // Hooks
    private static void NightILAdapt(ILContext il)
    {
        try
        {
            BTWPlugin.Log("IL hook of Rain Night Timer Starting...");
            ILCursor cursor = new(il);
            Instruction Mark = cursor.Next;

            Instruction[] getIfs()
            {
                Instruction[] ifs = new Instruction[8];
                cursor.Goto(0, MoveType.After); //to start
                for (int i = 0; i < ifs.Length; i++)
                {
                    if (i + 1 != ifs.Length)
                    {
                        if (
                            cursor.TryGotoNext(MoveType.After,
                                x => x.MatchLdarg(0),
                                x => x.MatchCall<RoomCamera>("get_room"),
                                x => x.MatchLdfld<Room>("world"),
                                x => x.MatchLdfld<World>("rainCycle"),
                                x => x.MatchLdfld<RainCycle>("dayNightCounter"),
                                x => x.MatchConvR4(),
                                x => x.MatchLdloc(0)
                            ) &&
                            cursor.TryGotoNext(MoveType.Before, x => x.MatchLdarg(0))
                        )
                        {
                            ifs[i] = cursor.Next;
                        }
                        else
                        {
                            BTWPlugin.logger.LogError("Couldn't find IL hook " + i + " :<");
                        }
                    }
                    else
                    {
                        ILLabel mark = null;
                        if (cursor.TryGotoPrev(MoveType.Before, x => x.MatchBleUn(out mark)))
                        {
                            ifs[ifs.Length - 1] = mark.Target;
                        }
                        else
                        {
                            BTWPlugin.logger.LogError("Couldn't find IL hook " + i + " :<");
                        }
                    }
                }
                cursor.Goto(0, MoveType.After); //return to start
                return ifs;
            }
            static bool IsNightOn(RoomCamera self)
            {
                return self.hud != null
                    && self.hud.rainMeter != null
                    && cwtNightTimeRM.TryGetValue(self.hud.rainMeter, out var NTRM)
                    && cwtNightTimeRC.TryGetValue(self.game.world.rainCycle, out var NTRC)
                    && NTRC.outOfNight;
            }
            static int ChangeToOrigBPallete(int OldPalette, RoomCamera self)
            {
                return self.room == null ? OldPalette : GetOrigBPallete(self);
            }
            static int ChangeToOrigBPalleteIfN(int OldPalette, RoomCamera self)
            {
                return IsNightOn(self) ? ChangeToOrigBPallete(OldPalette, self) : OldPalette;
            }
            static int ChangeToDuskPalleteIfN(int OldPalette, RoomCamera self)
            {
                return IsNightOn(self) ? self.room.world.rainCycle.duskPalette : OldPalette;
            }
            static void CheckForOrigPaletteFade(RoomCamera self)
            {
                // Plugin.logger.LogDebug(IsNightOn(self) + "/" + self.paletteBlend + "/" + GetOrigPalleteFade(self));
                if (IsNightOn(self) && self.paletteBlend != GetOrigPalleteFade(self))
                {
                    self.ChangeBothPalettes(GetOrigAPallete(self), GetOrigBPallete(self), GetOrigPalleteFade(self));
                }
            }
            // static void PrintPalette(RoomCamera self)
            // {
            //     Plugin.logger.LogDebug("Counter : " + self.room.world.rainCycle.dayNightCounter + " with palette " + self.paletteA + "/" + self.paletteB + "/" + self.paletteBlend);
            // }

            // Add Out of Night condition
            if (cursor.TryGotoNext(MoveType.Before, x => x.MatchLdcR4(1320f)))
            {
                Mark = cursor.Next;
            }
            else { BTWPlugin.logger.LogError("(RTA:RTN) Couldn't find IL hook 0bis :<"); }
            cursor.Goto(0, MoveType.After);
            if (cursor.TryGotoNext(MoveType.Before, x => x.MatchLdsfld<ModManager>("Expedition")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(IsNightOn);

                cursor.Emit(OpCodes.Brtrue, Mark);
            }
            else { BTWPlugin.logger.LogError("(RTA:RTN) Couldn't find IL hook 0 :<"); }

            // Print
            // Mark = getIfs()[7];
            // if (cursor.TryGotoNext(MoveType.After, x => x == Mark))
            // {
            //     cursor.Emit(OpCodes.Ldarg_0);
            //     cursor.EmitDelegate(PrintPalette);
            // } else { Plugin.logger.LogError("(RTA:RTN) Couldn't find IL hook print :<"); }


            // Origin Palette
            Mark = getIfs()[0];
            if (cursor.TryGotoNext(MoveType.After, x => x == Mark))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(CheckForOrigPaletteFade);
            }
            else { BTWPlugin.logger.LogError("(RTA:RTN) Couldn't find IL hook 1 :<"); }

            // Palette check

            Mark = getIfs()[1];
            if (
                cursor.TryGotoNext(MoveType.After, x => x == Mark) &&
                cursor.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<RoomCamera>("paletteB"))
            )
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ChangeToOrigBPalleteIfN);
            }
            else { BTWPlugin.logger.LogError("(RTA:RTN) Couldn't find IL hook 2 :<"); }
            if (
                cursor.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchCall<RoomCamera>("get_room"),
                    x => x.MatchLdfld<Room>("world"),
                    x => x.MatchLdfld<World>("rainCycle"),
                    x => x.MatchLdfld<RainCycle>("duskPalette"))
            )
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ChangeToDuskPalleteIfN);
            }
            else { BTWPlugin.logger.LogError("(RTA:RTN) Couldn't find IL hook 3 :<"); }

            Mark = getIfs()[2];
            if (
                cursor.TryGotoNext(MoveType.After, x => x == Mark) &&
                cursor.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<RoomCamera>("paletteB"))
            )
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ChangeToOrigBPalleteIfN);
            }
            else { BTWPlugin.logger.LogError("(RTA:RTN) Couldn't find IL hook 4 :<"); }
            if (
                cursor.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchCall<RoomCamera>("get_room"),
                    x => x.MatchLdfld<Room>("world"),
                    x => x.MatchLdfld<World>("rainCycle"),
                    x => x.MatchLdfld<RainCycle>("duskPalette"))
            )
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ChangeToDuskPalleteIfN);
            }
            else { BTWPlugin.logger.LogError("(RTA:RTN) Couldn't find IL hook 5 :<"); }

            BTWPlugin.Log("IL hook of Rain Night Timer Done !");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
    }
    private static void AddBlueCircles(On.HUD.RainMeter.orig_ctor orig, RainMeter self, HUD.HUD hud, FContainer fContainer)
    {
        BTWPlugin.Log("AddBlueCircles start !");
        orig(self, hud, fContainer);
        try
        {
            if (!cwtNightTimeRM.TryGetValue(self, out var _) && hud.owner is Player player && CanNightCycle(player.abstractCreature.world.game))
            {
                cwtNightTimeRM.Add(self, new NightTimeRainMeter(self, hud, fContainer));
                BTWPlugin.Log("Added NightTimeRainMeter");
            }
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("AddBlueCircles success !");
    }
    private static void UpdateBlueCircles(On.HUD.RainMeter.orig_Update orig, RainMeter self)
    {
        orig(self);
        if (cwtNightTimeRM.TryGetValue(self, out var NTRM))
        {
            NTRM.Update();
        }
    }
    private static void DrawBlueCircles(On.HUD.RainMeter.orig_Draw orig, RainMeter self, float timeStacker)
    {
        orig(self, timeStacker);
        if (cwtNightTimeRM.TryGetValue(self, out var NTRM))
        {
            NTRM.Draw(timeStacker);
        }
    }
    
    private static void AddNightCycle(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
    {
        BTWPlugin.Log("AddNightCycle start !");
        orig(self, world, minutes);
        BTWPlugin.Log("RainCycle added");
        try
        {
            if (!cwtNightTimeRC.TryGetValue(self, out _) && CanNightCycle(world?.game))
            {
                BTWPlugin.Log("Adding NightTimeRainCycle");
                NightTimeRainCycle NTRC = new(self, world);
                cwtNightTimeRC.Add(self, NTRC);
                if (Tiredness.TryGet(world.game, out int t))
                {
                    NTRC.tiredness = t;
                    NTRC.lastTiredness = t;
                }
                BTWPlugin.Log("Added NightTimeRainCycle");
            }
            BTWPlugin.Log("AddNightCycle success !");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
    }
    private static void UpdateNightCycle(On.RainCycle.orig_Update orig, RainCycle self)
    {
        orig(self);
        if (cwtNightTimeRC.TryGetValue(self, out var NTRC) && !self.world.game.GamePaused)
        {
            NTRC.Update();
        } 
    }

    private static void ResetExtraCycles(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
    {
        BTWPlugin.Log("ResetExtraCycles start !");
        if (self.world != null && self.world.rainCycle != null)
        {
            if (cwtNightTimeRC.TryGetValue(self.world.rainCycle, out var NTRC))
            {
                (self.session as StoryGameSession).saveState.cycleNumber -= NTRC.ExtraCycles;
                Tiredness.Get(self.world.game).Value = NTRC.lastTiredness;
                BTWPlugin.Log("NTRC reset managed");
            }
        }
        orig(self);
        BTWPlugin.Log("ResetExtraCycles success !");
    }
    private static void OnResting(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
    {
        BTWPlugin.Log("OnResting start !");
        if (self.rainCycle != null)
        {
            if (cwtNightTimeRC.TryGetValue(self.rainCycle, out var NTRC))
            {
                Tiredness.Get(self.rainCycle.world.game).Value = Math.Max(NTRC.tiredness - 1, 0);
                BTWPlugin.Log("NTRC tiredness managed");
            }
        }
        orig(self);
        BTWPlugin.Log("OnResting success !");
    }
}