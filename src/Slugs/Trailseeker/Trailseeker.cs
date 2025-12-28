using System;

namespace BeyondTheWest;
public class TrailseekerFunc
{
    public const string TrailseekerID = "Trailseeker";
    public static void ApplyHooks()
    {
        ModifiedTechHooks.ApplyHooks();
        WallClimbManagerHooks.ApplyHooks();
        PoleKickManagerHooks.ApplyHooks();
        On.Player.ctor += Player_Trailseeker_WallClimbManagerInit;

        BTWPlugin.Log("TrailseekerFunc ApplyHooks Done !");
    }

    public static bool IsTrailseeker(Player player)
    {
        return player.SlugCatClass.ToString() == TrailseekerID;
    }
    // Hooks
    
    private static void Player_Trailseeker_WallClimbManagerInit(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (IsTrailseeker(self) && BTWFunc.IsLocal(self))
        {
            if (!WallClimbManager.TryGetManager(self.abstractCreature, out _))
            {
                BTWPlugin.Log("Trailseeker WallClimbManager initiated");
                WallClimbManager.AddManager(abstractCreature);
                BTWPlugin.Log("Trailseeker WallClimbManager created !");
            }
            if (!ModifiedTechManager.TryGetManager(self.abstractCreature, out _))
            {
                BTWPlugin.Log("Trailseeker ModifiedTech initiated");
                ModifiedTechManager.AddManager(abstractCreature);
                BTWPlugin.Log("Trailseeker ModifiedTech created !");
            }
        }
    }
}