using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BeyondTheWest.Items;

namespace BeyondTheWest 
{
    [BepInPlugin(MOD_ID, "Beyond The West", MOD_VERSION)]
    [BepInDependency("slime-cubed.slugbase")]
    class BTWPlugin : BaseUnityPlugin
    {
        private const string MOD_ID = "edenstonne.beyondthewest";
        public const string MOD_VERSION = "1.4.2";
        private static bool isInit = false;
        private static bool ressourceInit = false;
        public static bool ressourceFullyEnded = false;
        private static bool compatInit = false;
        public static bool compatFullyEnded = false;
        private static bool hooksInit = false;
        public static bool hooksFullyEnded = false;
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
        public static void LogError(object data)
        {
            if (logger != null)
            {
                logger.LogError("["+ DateTime.Now.Hour.ToString() +":"+ DateTime.Now.Minute.ToString() +":"+ DateTime.Now.Second.ToString() +"."+ DateTime.Now.Millisecond.ToString() +"] : "+ data);
            }
        }
        public static void LogAllRegisteredImage()
        {
            Log("Logging all registered images :");
            foreach (KeyValuePair<string, FAtlasElement> keyValuePair in Futile.atlasManager._allElementsByName)
            {
                FAtlasElement value = keyValuePair.Value;
                Log($"    >{value.name}");
            }
        }
        public static string[] GetVersionArray()
        {
            return MOD_VERSION.Split(new char[]{'.'}, 3);
        }
        public static int[] GetVersionIntArray()
        {
            int[] version = new int[3];
            string[] strVersion = GetVersionArray();
            for (int i = 0; i < 3; i++)
            {
                version[i] = int.Parse(strVersion[i]);
            }
            return version;
        }
        
        public void OnEnable()
        {
            Logger.LogInfo("BTW Enabled function ran.");
            if (isInit) { return; }
            isInit = true;
            Logger.LogInfo("BTW initializing...");

            logger = Logger;
            On.RainWorld.OnModsInit += LoadResources;
            On.RainWorld.OnModsInit += ApplyHooks;

            On.RainWorld.OnModsInit += RemixMenuInit;
            On.RainWorld.PostModsInit += PostModsLoad;
        }

        // Load the remix menu
        private void RemixMenuInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            MachineConnector.SetRegisteredOI(MOD_ID, BTWRemix.instance);
        }

        // Load main hooks, when this mod is initialized
        public static void ApplyHooks(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            logger.LogInfo("ApplyHooks starts !");
            if (hooksInit) { Log("ApplyHooks already done !"); return;}

            try
            {
                hooksInit = true;

                BTWMenu.ApplyHooks();

                BTWExtensionsHook.ApplyHooks();
                NewObjectsHooks.ApplyHooks();
                BTWSkins.ApplyHooks();

                CoreFunc.ApplyHooks();
                SparkFunc.ApplyHooks();
                TrailseekerFunc.ApplyHooks();

                WIPSlugLock.ApplyHooks();
                BTWCreatureDataHooks.ApplyHooks();
                BTWPlayerDataHooks.ApplyHooks();

                ArenaAddition.ArenaHookHelper.ApplyHooks();
                
                hooksFullyEnded = true;
            }
            catch (Exception e)
            {
                logger.LogError("Error while starting BTW hooks !\n"+e);
            }

            logger.LogInfo("Hooks initialized !");
        }
        
        // Load any resources, such as sprites or sounds (BEFORE the function)
        private void LoadResources(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            Log("LoadResources starts !");
            if (ressourceInit) { Log("LoadResources already done !"); return;}

            try
            {
                ressourceInit = true;

                BTWSkins.LoadSkins();
                NewObjectsHooks.LoadIcons();

                ressourceFullyEnded = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            } 

            Log("LoadResources initialized !");
        }
        
        
        // Post load for any compat with other mods
        public static void CheckMods()
        {
            Log("Checking Mods starts !");

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

        private void PostModsLoad(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            logger.LogInfo("Post Mods Load starts");

            if (compatInit) { Log("Post Mods already done !"); return;}

            try
            {
                compatInit = true;

                CheckMods();
                ApplySoftDependiesHooks();
                ArenaAddition.ArenaHookHelper.ApplyPostHooks();

                compatFullyEnded = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            } 
            
            logger.LogInfo("Post Mods Load initialized");
        }
    }
}