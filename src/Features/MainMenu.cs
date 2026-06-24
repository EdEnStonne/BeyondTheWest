using System;
using System.Collections.Generic;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace BeyondTheWest;

public static class BTWMenu
{
    public static bool errorDisplayed = false;
    public static void ApplyHooks()
    {
        On.Menu.MainMenu.ctor += MainMenu_OnStart;
        IL.Menu.IntroRoll.ctor += IntroRoll_ctor_AddNewIntroRolls;
    }
    public static void LoadResources()
    {        
        Futile.atlasManager.ActuallyLoadAtlasOrImage("Intro_Roll_C_" + TrailseekerFunc.TrailseekerID, "illustrations/intro_roll_c_trailseeker", "");
    }
    
    private static void AddNewRolls(IntroRoll introRoll, ProcessManager manager)
    {
        string slugcatID = manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat.ToString();
        if (slugcatID == TrailseekerFunc.TrailseekerID
            || slugcatID == CoreFunc.CoreID
            || slugcatID == SparkFunc.SparkID)
        {
            introRoll.illustrations[2] = new MenuIllustration(introRoll, 
                introRoll.pages[0], 
                "", "Intro_Roll_C_" + TrailseekerFunc.TrailseekerID, 
                new Vector2(0f, 0f), true, false);
        }
    }
    private static void IntroRoll_ctor_AddNewIntroRolls(ILContext il)
    {
        BTWPlugin.Log("BTWMenu IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.Before, 
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(5),
                x => x.MatchBr(out _)))
            {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(AddNewRolls);
            }
            else
            {
                BTWPlugin.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.LogError(ex);
        }
        BTWPlugin.Log("BTWMenu IL 1 ends");
    }

    private static void MainMenu_OnStart(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);
        
        List<string> errorContext = new();
        if (!BTWPlugin.ressourceFullyEnded)
        {
            errorContext.Add("ressource");
        }
        if (!BTWPlugin.hooksFullyEnded)
        {
            errorContext.Add("main");
        }
        if (!BTWPlugin.compatFullyEnded)
        {
            errorContext.Add("compatibility");
        }
        
        if (errorContext.Count > 0)
        {
            self.manager.ShowDialog(new DialogNotify(
                self.Translate("Beyond The West failed to load correctly !" 
                    + Environment.NewLine + $"({string.Join(", ",errorContext)})"), 
                self.manager, null));
        }

        if (BTWPlugin.oldInputConfigEnabled)
        {
            self.manager.ShowDialog(new DialogNotify(
                self.Translate("It seems that you have installed the original \"Improved Input Config\" mod." 
                    + Environment.NewLine + $"Please use instead the forked version of Zombieseatflesh7 named :"
                    + Environment.NewLine + $"\"Improved Input Config: Extended\""
                    + Environment.NewLine + $"(it works with every mods than needs input config as a dependency !)"), 
                self.manager, null));
        }
        errorDisplayed = true;
    }
}