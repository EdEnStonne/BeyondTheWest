using Menu;
using Menu.Remix.MixedUI;
using System;
using UnityEngine;
using RWCustom;
using BeyondTheWest;
using RainMeadow;


namespace BeyondTheWest.MeadowCompat.BTWMenu;

// ...I kinda did a whole menu for nothing huh ?
public class BTWUpdateDialog : MenuDialogBox // from the branch of Quaxly in Rain Meadow, big thanks to them !
{
    public static string hostVersion = "Unknown";
    public static string Text
    {
        get
        {
            return BTWFunc.Translate("The lobby you are trying to join doesn't have the same version of Beyond the West !")
                + Environment.NewLine + Environment.NewLine +
                BTWFunc.Translate($"Your version is [{BTWPlugin.MOD_VERSION}] while the host version is [{hostVersion}].")
                + Environment.NewLine + Environment.NewLine +
                BTWFunc.Translate("To avoid crashes, you have been send back to the lobby menu.");
        }
    }
    public static string HelpText
    {
        get
        {
            return BTWFunc.Translate("If you have an older version, you'll need to update Beyond The West")
                + Environment.NewLine + Environment.NewLine +
                BTWFunc.Translate("Restart your game. If Beyond The West doesn't update automatically, resubscribe to the workshop to force an update.")
                + Environment.NewLine + Environment.NewLine +
                BTWFunc.Translate("If you version is newer than the host, you'll need to ask them to update their version of Beyond the West");
        }
    }

    private SimpleButton okButton;
    private SimpleButton helpButton;
    private SimpleButton backButton;

    private bool helpScreen = false;

    public BTWUpdateDialog(global::Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, bool forceWrapping = false) 
        : base(menu, owner, Text, pos, size, forceWrapping)
    {
        Populate();
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "Continue":
                Clear();
                if (menu is LobbySelectMenu lobbySelectMenu)
                {
                    lobbySelectMenu.HideDialog();
                }
                break;
            case "Help":
                Clear();
                descriptionLabel.text = HelpText.WrapText(bigText: false, size.x - 40f, false);
                helpScreen = true;
                Populate();
                break;
            case "Ret":
                Clear();
                descriptionLabel.text = Text.WrapText(bigText: false, size.x - 40f, false);
                helpScreen = false;
                Populate();
                break;
        }
    }

    public void Populate()
    {
        if (helpScreen)
        {
            backButton = new SimpleButton(menu, this, menu.Translate("BACK"), "Ret",
                new Vector2((size.x * 0.5f) - 55f, Mathf.Max(size.y * 0.04f, 7f)), new Vector2(110f, 30f));
            subObjects.Add(backButton);
        }
        else
        {
            okButton = new SimpleButton(menu, this, menu.Translate("CONTINUE"), "Continue",
                new Vector2((size.x * 0.5f) - 55f - 110f, Mathf.Max(size.y * 0.04f, 7f)), new Vector2(110f, 30f));
            helpButton = new SimpleButton(menu, this, menu.Translate("HELP"), "Help",
                new Vector2((size.x * 0.5f) - 55f + 110f, Mathf.Max(size.y * 0.04f, 7f)), new Vector2(110f, 30f));
            subObjects.Add(okButton);
            subObjects.Add(helpButton);
        }
    }

    public void Clear()
    {
        if (okButton != null)
        {
            okButton.RemoveSprites();
            subObjects.Remove(okButton);
            page.selectables.Remove(okButton);
        }
        if (helpButton != null)
        {
            helpButton.RemoveSprites();
            subObjects.Remove(helpButton);
            page.selectables.Remove(helpButton);
        }
        if (backButton != null)
        {
            backButton.RemoveSprites();
            subObjects.Remove(backButton);
            page.selectables.Remove(backButton);
        }
        menu.selectedObject = null;
        page.lastSelectedObject = null;
    }

    public override void RemoveSprites()
    {
        base.RemoveSprites();
        Clear();
    }
}
