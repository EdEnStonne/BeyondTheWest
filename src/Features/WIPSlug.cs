using UnityEngine;
using System.Linq;
using BeyondTheWest;

public class WIPSlugLock
{
    public static readonly string[] WIPLock = { "Core", "Spark", "Trailseeker"}; // Trailseeker
    public static bool removeWIPSlugPages = false; // Plugin.meadowEnabled;
    public static void ApplyHooks()
    {
        On.Menu.SlugcatSelectMenu.ctor += SlugcatSelectMenu_DisplayWIPSlugs;
        On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_LockStartButton;
        On.Menu.SlugcatSelectMenu.SlugcatUnlocked += SlugcatSelectMenu_LockCampaign;
        BTWPlugin.Log("WIPSlugLock ApplyHooks Done !");
    }



    // Hooks
    private static void SlugcatSelectMenu_DisplayWIPSlugs(On.Menu.SlugcatSelectMenu.orig_ctor orig, Menu.SlugcatSelectMenu self, ProcessManager manager)
    {
        orig(self, manager);
        foreach (Menu.SlugcatSelectMenu.SlugcatPage page in self.slugcatPages)
        {
            BTWPlugin.Log("Page of " + page.slugcatNumber.ToString() + " checked !");
            if (page is Menu.SlugcatSelectMenu.SlugcatPageNewGame newpage && WIPLock.Contains(page.slugcatNumber.ToString()))
            {
                newpage.infoLabel.text = "This campaign is still a work in progress.\nYou can still play this slugcat in arena, with Jolly Co-op or in Meadow.";
                newpage.infoLabel.label.color = Color.red;
                BTWPlugin.Log("Page locked !");
            }
        }
        if (removeWIPSlugPages)
        {
            BTWPlugin.Log("Removing locked pages...");
            self.slugcatPages.RemoveAll(x => WIPLock.Contains(x.slugcatNumber.ToString()));
            self.pages.RemoveAll(x => x is Menu.SlugcatSelectMenu.SlugcatPageNewGame page && WIPLock.Contains(page.slugcatNumber.ToString()));
            self.slugcatPageIndex = 0;
        }
    }
    private static void SlugcatSelectMenu_LockStartButton(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, Menu.SlugcatSelectMenu self)
    {
        if (WIPLock.Contains(self.colorFromIndex(self.slugcatPageIndex).ToString()))
        {
	        self.startButton.menuLabel.text = self.Translate("WORK IN\nPROGRESS");
            self.startButton.GetButtonBehavior.greyedOut = true;
            return;
        }
        orig(self);
    }
    private static bool SlugcatSelectMenu_LockCampaign(On.Menu.SlugcatSelectMenu.orig_SlugcatUnlocked orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name i)
    {
        if (WIPLock.Contains(self.colorFromIndex(self.slugcatPageIndex).ToString()))
        {
            return false;
        }
        return orig(self, i);
    }
}