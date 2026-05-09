using System;
using System.Security.Permissions;
using BeyondTheWest;
using UnityEngine;

/*
 * This file contains fixes to some common problems when modding Rain World.
 * Unless you know what you're doing, you shouldn't modify anything here.
 */

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


internal static class Extras
{
    private static bool _initialized;

    // Ensure resources are only loaded once and that failing to load them will not break other mods
    public static On.RainWorld.hook_OnModsInit WrapInit(Action<RainWorld> loadResources)
    {
        return (orig, self) =>
        {
            orig(self);

            try
            {
                if (!_initialized)
                {
                    Plugin.Log("BTW OnModsInit initializing...");
                    _initialized = true;
                    loadResources(self);
                    Plugin.Log("BTW OnModsInit Done !");
                }
            }
            catch (Exception e)
            {
                Plugin.logger.LogError(e);
            }
        };
    }
}