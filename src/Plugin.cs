using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BeyondTheWest.Items;

namespace BeyondTheWest 
{
    [BepInPlugin(MOD_ID, "Beyond The West", MOD_VERSION)]
    [BepInDependency("slime-cubed.slugbase")]
    [BepInDependency("rwmodding.coreorg.pom")]
    [BepInDependency("edenstonne.beyondthewest.tutorial")]

    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "edenstonne.beyondthewest";
        public const string MOD_VERSION = "1.5.0";
        private static bool isInit = false;
        private static bool ressourceInit = false;
        private static bool compatInit = false;
        private static bool hooksInit = false;
        internal static List<string> ErrorContext {get; private set;} = new();
        static readonly bool debug = true;
        public static ManualLogSource logger; // Logger from glebi574
        public static bool meadowEnabled = false;
        public static bool oldInputConfigEnabled = false;
        public static bool inputConfigEnabled = false;
        public static bool pushToMeowEnabled = false;
        public static bool simplifiedMovesetEnabled = false;
        public static bool DMSEnabled = false;

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
        }public static void LogAllRegisteredSlugcats()
        {
            Log("Logging all registered slugcat with SlugBase :");
            foreach (var name in SlugBase.SlugBaseCharacter.Registry.Keys)
            {
                Log($"    >{name}");
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
            }
            catch (Exception e)
            {
                logger.LogError("Error while starting BTW hooks !\n"+e);
                ErrorContext.Add("Main hooks");
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
                Tutorial.TutorialHook.LoadTexture();
            }
            catch (Exception e)
            {
                logger.LogError("Error while loading BTW ressources !\n"+e);
                ErrorContext.Add("Ressources");
            } 

            logger.LogInfo("LoadResources initialized !");
        }
        
        
        // Post load for any compat with other mods
        public static void CheckMods()
        {
            Log("Checking Mods starts !");

            foreach (ModManager.Mod mod in ModManager.ActiveMods.FindAll(x => x.enabled))
            {
                if (mod.id == "henpemaz_rainmeadow")
                {
                    Log("Found meadow !");
                    meadowEnabled = true;
                }
                else if (mod.id == "improved-input-config")
                {
                    if (mod.name == "Improved Input Config")
                    {
                        Log("Found old improved input config !");
                        oldInputConfigEnabled = true;
                    }
                    else
                    {
                        Log("Found forked improved input config !");
                        inputConfigEnabled = true;
                    }
                }
                else if (mod.id == "pushtomeow")
                {
                    Log("Found push to meow !");
                    pushToMeowEnabled = true;
                }
                else if (mod.id == "SimplifiedMoveset")
                {
                    Log("Found simplified moveset !");
                    simplifiedMovesetEnabled = true;
                }
                else if (mod.id == "dressmyslugcat")
                {
                    Log("Found DMS !");
                    DMSEnabled = true;
                }
            }

            Log("Checking Mods initialized !");
        }
        public static void ApplySoftDependiesHooks()
        {
            logger.LogInfo("Soft Hooks start !");

            try
            {
                CheckMods();
            
                if (ModManager.MSC) { ApplyMSCHooks(); }
                if (ModManager.Watcher) { ApplyWatcherHooks(); }
                if (meadowEnabled) { ApplyMeadowHooks(); }
                if (pushToMeowEnabled) { ApplyPushToMeowHooks(); }
                if (simplifiedMovesetEnabled) { ApplySimplifiedMovesetHooks(); }
                if (DMSEnabled) { ApplyDMSHooks(); }
            }
            catch (Exception e)
            {
                logger.LogError("Error while applying BTW generic mod compat !\n"+e);
                ErrorContext.Add("Compatibility");
            } 
            // LogAllRegisteredSlugcats();
            logger.LogInfo("Soft Hooks initialized !");
        }
        public static void ApplyMeadowHooks()
        {
            Log("Meadow Hooks start !");

            try
            {
                MeadowCompat.MeadowHookHelper.ApplyHooks();
            }
            catch (Exception e)
            {
                logger.LogError("Error while applying Rain Meadow compat !\n"+e);
                ErrorContext.Add("Meadow Compat");
            } 

            Log("Meadow Hooks initialized !");
        }
        public static void ApplyMSCHooks()
        {
            Log("MSC Hooks start !");

            try
            {
                MSCCompat.CraftHooks.ApplyHooks();
                MSCCompat.SpawnMSCPool.ApplyHooks();
            }
            catch (Exception e)
            {
                logger.LogError("Error while applying MSC compat !\n"+e);
                ErrorContext.Add("MSC Compat");
            } 
            
            Log("MSC Hooks initialized !");
        }
        public static void ApplyWatcherHooks()
        {
            Log("Watcher Hooks start !");

            try
            {
                WatcherCompat.SpawnWatcherPool.ApplyHooks();
            }
            catch (Exception e)
            {
                logger.LogError("Error while applying Watcher compat !\n"+e);
                ErrorContext.Add("Watcher Compat");
            } 
            
            Log("Watcher Hooks initialized !");
        }  
        public static void ApplyPushToMeowHooks()
        {
            Log("PushToMeow Hooks start !");

            try
            {
                PushToMeowCompat.BTWMeow.ApplyHooks();
            }
            catch (Exception e)
            {
                logger.LogError("Error while applying Push To Meow compat !\n"+e);
                ErrorContext.Add("Meow Compat");
            } 
            
            Log("PushToMeow Hooks initialized !");
        } 
        public static void ApplySimplifiedMovesetHooks()
        {
            Log("SimplifiedMoveset Hooks start !");

            try
            {
                SimplifiedMovesetCompat.BTWSimplifiedMoveset.ApplyHooks();
            }
            catch (Exception e)
            {
                logger.LogError("Error while applying Simplified Moveset compat !\n"+e);
                ErrorContext.Add("Moveset Compat");
            } 
            
            Log("SimplifiedMoveset Hooks initialized !");
        }  
        public static void ApplyDMSHooks()
        {
            Log("DMS Hooks start !");

            try
            {
                DressMySlugcatCompat.BTWDMSDressing.ApplyHooks();
            }
            catch (Exception e)
            {
                logger.LogError("Error while applying Dress My Slugcat compat !\n"+e);
                ErrorContext.Add("DMS Compat");
            } 
            
            Log("DMS Hooks initialized !");
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
                Tutorial.TutorialHook.ApplyHooks();
            }
            catch (Exception e)
            {
                logger.LogError("Error while loading BTW post-load hooks !\n"+e);
                ErrorContext.Add("Post Hooks");
            } 
            
            logger.LogInfo("Post Mods Load initialized");
        }
    }
}