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

        Plugin.Log("TrailseekerFunc ApplyHooks Done !");
    }

    public static bool IsTrailseeker(Player player)
    {
        return player.SlugCatClass.ToString() == TrailseekerID;
    }
    // Hooks
    
    private static void Player_Trailseeker_WallClimbManagerInit(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        bool local = BTWFunc.IsLocal(self);
        if (IsTrailseeker(self) && local)
        {
            if (!WallClimbManager.TryGetManager(self.abstractCreature, out _))
            {
                Plugin.Log("Trailseeker WallClimbManager initiated");
                WallClimbManager.AddManager(abstractCreature);
                Plugin.Log("Trailseeker WallClimbManager created !");
            }
            if (!ModifiedTechManager.TryGetManager(self.abstractCreature, out _))
            {
                Plugin.Log("Trailseeker ModifiedTech initiated");
                ModifiedTechManager.AddManager(abstractCreature);
                Plugin.Log("Trailseeker ModifiedTech created !");
            }
        }
        if (local)
        {
            if (!PoleKickManager.TryGetManager(self.abstractCreature, out _))
            {
                Plugin.Log("PoleKickManager initiated");
                PoleKickManager.AddManager(abstractCreature, out var PKM);
                PKM.kickEnabled = IsTrailseeker(self);
                Plugin.Log("PoleKickManager created !");
            }
        }
    }
}