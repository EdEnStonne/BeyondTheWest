using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Runtime.CompilerServices;
using System.Linq;
using IL;
using System.Drawing.Text;
using HUD;
using MonoMod.Cil;
using IL.Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using IL.Menu.Remix.MixedUI;
using IL.HUD;
using System.CodeDom;
using BepInEx.Logging;
using DevInterface;
using Mono.Cecil.Cil;
using On;

namespace BeyondTheWest 
{
    [BepInPlugin(MOD_ID, "Beyond The West", "1.0.9")]
    [BepInDependency("slime-cubed.slugbase")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "edenstonne.beyondthewest";
        private static bool isInit = false;
        private static bool ressourceInit = false;
        private static bool modcheckInit = false;
        private static bool hooksInit = false;
        static readonly bool debug = true;
        public static ManualLogSource logger; // Logger from glebi574
        public static bool meadowEnabled = false;

        public static void Log(object data)
        {
            if (logger != null && debug)
            {
                logger.LogDebug("[BTWDebug "+ DateTime.Now.Hour.ToString() +":"+ DateTime.Now.Minute.ToString() +":"+ DateTime.Now.Second.ToString() +"."+ DateTime.Now.Millisecond.ToString() +"] : "+ data);
            }
        }
        
        public void OnEnable()
        {
            Logger.LogInfo("BTW Enabled function ran.");
            if (isInit) { return; }
            isInit = true;
            Logger.LogInfo("BTW initializing...");

            logger = Logger;
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.RainWorld.OnModsInit += Extras.WrapInit(ApplyHooks);
            ApplyHooks();

            On.RainWorld.OnModsInit += RemixMenuInit;
            On.RainWorld.PostModsInit += PostModsLoad;
        }

        public static void ApplyHooks(RainWorld rainWorld)
        {
            ApplyHooks();
        }
        public static void ApplyHooks()
        {
            logger.LogInfo("ApplyHooks starts !");
            if (hooksInit) { Log("ApplyHooks already done !"); return;}

            try
            {
                hooksInit = true;
                CoreFunc.ApplyHooks();
                CoreObject.ApplyHooks();

                SparkFunc.ApplyHooks();
                SparkObject.ApplyHooks();

                TrailseekerFunc.ApplyHooks();
                WallClimbObject.ApplyHooks();

                WIPSlugLock.ApplyHooks();

                CompetitiveAddition.ApplyHooks();
                BTWSkins.ApplyHooks();
                // RainTimerAddition.ApplyHooks();
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }

            logger.LogInfo("Hooks initialized !");
        }
        public static void ApplySoftDependiesHooks()
        {
            logger.LogInfo("Soft Hooks start !");
            if (meadowEnabled)
            {
                ApplyMeadowHooks();
            }
            logger.LogInfo("Soft Hooks initialized !");
        }
        public static void ApplyMeadowHooks()
        {
            Log("Meadow Hooks start !");
            MeadowBTWArenaMenu.ApplyHooks();
            MeadowCompat.ApplyHooks();
            Log("Meadow Hooks initialized !");
        }

        public static void CheckMods()
        {
            Log("Checking Mods starts !");
            if (modcheckInit) { Log("Checking Mods already done !"); return;}
            modcheckInit = true;

            foreach (ModManager.Mod mod in ModManager.ActiveMods)
            {
                if (mod.id == "henpemaz_rainmeadow" && mod.enabled)
                {
                    Log("Found meadow !");
                    meadowEnabled = true;
                }
            }

            Log("Checking Mods initialized !");
        }

        // Load any resources, such as sprites or sounds (BEFORE the function)
        private void LoadResources(RainWorld rainWorld)
        {
            LoadResources();
        }
        private void LoadResources()
        {
            Log("LoadResources starts !");
            if (ressourceInit) { Log("LoadResources already done !"); return;}

            try
            {
                BTWSkins.LoadSkins();
                ressourceInit = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            } 

            Log("LoadResources initialized !");
        }
        
        // Welcome to tha hook
        private void RemixMenuInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            MachineConnector.SetRegisteredOI(MOD_ID, BTWRemix.instance);
        }

        // After mod loads
        private void PostModsLoad(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            logger.LogInfo("Post Mods Load starts");
            Plugin.CheckMods();
            if (!ressourceInit)
            {
                logger.LogError("Mod didn't load properly, loading missing ressources...");
                LoadResources(self);
            }
            if (!modcheckInit)
            {
                logger.LogError("Mod didn't load properly, loading missing mod checks...");
                CheckMods();
            }
            if (!hooksInit)
            {
                logger.LogError("Mod didn't load properly, loading missing hooks...");
                ApplyHooks();
            }
            ApplySoftDependiesHooks();
            logger.LogInfo("Post Mods Load initialized");
        }
        
    }
}