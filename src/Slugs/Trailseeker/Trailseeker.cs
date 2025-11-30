using System.Runtime.CompilerServices;
using BeyondTheWest;

public class TrailseekerFunc
{
    public static ConditionalWeakTable<AbstractCreature, WallClimbObject.WallClimbManager> cwtClimb = new();
    public static ConditionalWeakTable<AbstractCreature, ModifiedTech> cwtTech = new();
    public const int Trailseeker_WallClimbBonus = 4;
    public class ModifiedTech
    {
        public ModifiedTech(AbstractCreature abstractCreature)
        {
            this.abstractPlayer = abstractCreature;
            if (Plugin.meadowEnabled)
            {
                MeadowCompat.ModifiedTech_Init(this);
            }
            
        }
        // ------ Variables

        // Objects
        public AbstractCreature abstractPlayer;
        // Basic
        public bool techEnabled = true;
        public int poleBonus = Trailseeker_WallClimbBonus;
        // Get - Set
        public Player RealizedPlayer
        {
            get
            {
                if (this.abstractPlayer != null
                    && this.abstractPlayer.realizedCreature != null
                    && this.abstractPlayer.realizedCreature is Player player)
                {
                    return player;
                }
                return null;
            }
        }
    
    }
    public static void ApplyHooks()
    {
        On.Player.ctor += Player_Trailseeker_WallClimbManagerInit;
        On.Player.Update += Player_Trailseeker_WallClimbManager_Update;
        On.Player.Jump += Wanderer_Tech;
        Plugin.Log("TrailseekerFunc ApplyHooks Done !");
    }

    public static bool IsTrailseeker(Player player)
    {
        return player.SlugCatClass.ToString() == "Trailseeker";
    }
    private static bool MeadowRealPlayer(Player player)
    {
        if (Plugin.meadowEnabled)
        {
            return MeadowCompat.IsCreatureMine(player.abstractCreature);
        }
        return true;
    }

    // Hooks
    private static void Wanderer_Tech(On.Player.orig_Jump orig, Player self)
    {
        // Plugin.Log("Jump ? (Trailseeker)");
        Player.AnimationIndex oldAnim = self.animation;
        Player.BodyModeIndex oldBMode = self.bodyMode;
        int oldSlideUpPoles = self.slideUpPole;

        orig(self);

        if (cwtTech.TryGetValue(self.abstractCreature, out var modifiedTech) && (!Plugin.meadowEnabled || MeadowRealPlayer(self)))
        {
            bool techEnabled = modifiedTech.techEnabled;
            int poleBonus = modifiedTech.poleBonus;

            if (techEnabled)
            {
                if (self.animation == Player.AnimationIndex.Flip && !self.flipFromSlide)
                {
                    for (int i = self.bodyChunks.Length - 1; i >= 0; i--)
                    {
                        self.bodyChunks[i].vel.x *= 0.5f;
                        self.bodyChunks[i].vel.y *= 1.35f;
                    }
                }
                else if (self.animation == Player.AnimationIndex.RocketJump && (oldAnim == Player.AnimationIndex.Roll || oldAnim == Player.AnimationIndex.BellySlide))
                {
                    for (int i = self.bodyChunks.Length - 1; i >= 0; i--)
                    {
                        self.bodyChunks[i].vel.x *= 1.60f;
                        self.bodyChunks[i].vel.y *= 0.85f;
                    }
                }
                else if (oldBMode != Player.BodyModeIndex.Stand && oldBMode != Player.BodyModeIndex.ClimbingOnBeam && oldBMode != Player.BodyModeIndex.WallClimb)
                {
                    for (int i = self.bodyChunks.Length - 1; i >= 0; i--)
                    {
                        self.bodyChunks[i].vel.x *= 1.25f;
                        self.bodyChunks[i].vel.y *= 0.90f;
                    }
                }
            }

            if (self.slideUpPole == 17 && oldSlideUpPoles <= 0)
            {
                self.slideUpPole += poleBonus;
            }
        }
        // Plugin.Log("Jumped ! (Trailseeker)");
    }
    private static void Player_Trailseeker_WallClimbManagerInit(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (IsTrailseeker(self) && !cwtClimb.TryGetValue(self.abstractCreature, out _) && (!Plugin.meadowEnabled || MeadowRealPlayer(self)))
        {
            Plugin.Log("Trailseeker WallClimbManager initiated");
            cwtClimb.Add(abstractCreature, new WallClimbObject.WallClimbManager(self.abstractCreature));
            Plugin.Log("Trailseeker WallClimbManager created !");
        }
        if (IsTrailseeker(self) && !cwtTech.TryGetValue(self.abstractCreature, out _) && (!Plugin.meadowEnabled || MeadowRealPlayer(self)))
        {
            Plugin.Log("Trailseeker ModifiedTech initiated");
            cwtTech.Add(abstractCreature, new ModifiedTech(self.abstractCreature));
            Plugin.Log("Trailseeker ModifiedTech created !");
        }
    }
    private static void Player_Trailseeker_WallClimbManager_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (cwtClimb.TryGetValue(self.abstractCreature, out var WCM))
        {
            WCM.Update();
        }
        orig(self, eu);
    }
}