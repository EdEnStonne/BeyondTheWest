using System;
using System.Collections.Generic;
using Menu;

namespace BeyondTheWest;

public static class BTWMenu
{
    public static bool errorDisplayed = false;
    public static void ApplyHooks()
    {
        On.Menu.MainMenu.ctor += MainMenu_OnStart;
    }
    private static void MainMenu_OnStart(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);
        
        if (Plugin.ErrorContext.Count > 0)
        {
            self.manager.ShowDialog(new DialogNotify(
                self.Translate("Beyond The West failed to load correctly !" 
                    + Environment.NewLine + $"({string.Join(", ", Plugin.ErrorContext)})"), 
                self.manager, null));
        }

        if (Plugin.oldInputConfigEnabled)
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