using System;
using BepInEx;
using BepInEx.Logging;

namespace BeyondTheWest 
{
    [BepInPlugin(MOD_ID, "Beyond The West", MOD_VERSION)]
    [BepInDependency("slime-cubed.slugbase")]
    class BTWPlugin : BaseUnityPlugin
    {
        private const string MOD_ID = "edenstonne.beyondthewest";
        public const string MOD_VERSION = "1.3.7";
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
            // ApplyHooks();

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
                SparkFunc.ApplyHooks();
                TrailseekerFunc.ApplyHooks();

                WIPSlugLock.ApplyHooks();
                BTWPlayerDataHooks.ApplyHooks();

                ArenaAddition.ArenaHookHelper.ApplyHooks();
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
            if (ModManager.MSC)
            {
                ApplyMSCHooks();
            }
            if (ModManager.Watcher)
            {
                ApplyWatcherHooks();
            }
            logger.LogInfo("Soft Hooks initialized !");
        }
        public static void ApplyMeadowHooks()
        {
            Log("Meadow Hooks start !");
            MeadowCompat.MeadowHookHelper.ApplyHooks();
            Log("Meadow Hooks initialized !");
        }
        public static void ApplyMSCHooks()
        {
            Log("MSC Hooks start !");
            MSCCompat.CraftHooks.ApplyHooks();
            MSCCompat.SpawnMSCPool.ApplyHooks();
            Log("MSC Hooks initialized !");
        }
        public static void ApplyWatcherHooks()
        {
            Log("Watcher Hooks start !");
            WatcherCompat.SpawnWatcherPool.ApplyHooks();
            Log("Watcher Hooks initialized !");
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
            BTWPlugin.CheckMods();
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
            ArenaAddition.ArenaHookHelper.ApplyPostHooks();
            logger.LogInfo("Post Mods Load initialized");
        }
        
    }
}