using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using MonoMod.Cil;
using BeyondTheWest;
using Mono.Cecil.Cil;

public class BTWSkins
{
    public static ConditionalWeakTable<AbstractCreature, List<FSprite>> cwtPlayerSpriteInfo = new();
    private static bool skinloaded = false;
    public static void ApplyHooks()
    {
        On.PlayerGraphics.DrawSprites += Player_Sprite;
        IL.PlayerGraphics.InitiateSprites += Modify_Player_Sprite;
        IL.Player.ctor += Player_ModifyPlayerHeight;
        Plugin.Log("BTWSkins ApplyHooks Done !");
    }

    // Functions
    public static void LoadSkins()
    {
        Futile.atlasManager.ActuallyLoadAtlasOrImage("BodyASpark", "skin/Spark/body", "skin/Spark/body");
        Futile.atlasManager.ActuallyLoadAtlasOrImage("HipsASpark", "skin/Spark/hips", "skin/Spark/hips");

        Futile.atlasManager.ActuallyLoadAtlasOrImage("HipsAWanderer", "skin/Wanderer/hips", "skin/Wanderer/hips");
        
        // Futile.atlasManager.ActuallyLoadAtlasOrImage("TrailseekerScar", "skin/Wanderer/scar", "");
        Futile.atlasManager.ActuallyLoadAtlasOrImage("TrailseekerFaceD", "skin/Wanderer/faceD", "skin/Wanderer/faceD");
        Futile.atlasManager.ActuallyLoadAtlasOrImage("TrailseekerFaceG", "skin/Wanderer/faceG", "skin/Wanderer/faceG");
        
        Futile.atlasManager.ActuallyLoadAtlasOrImage("NoPoleIcon", "icons/nopole", "");
        Futile.atlasManager.ActuallyLoadAtlasOrImage("YesPoleIcon", "icons/pole", "");
        // foreach (KeyValuePair<string, FAtlasElement> keyValuePair in Futile.atlasManager._allElementsByName)
        // {
        //     FAtlasElement value = keyValuePair.Value;
        //     Plugin.Log(value.name);
        // }
        Plugin.Log("BTWSkins LoadSkin Done !");
        skinloaded = true;
    }

    // Hooks
    private static void Player_Sprite(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        // Get Info
        var psl = cwtPlayerSpriteInfo.GetOrCreateValue(self.player.abstractCreature);
        psl.Clear();
        foreach (var s in sLeaser.sprites) { psl.Add(s); }

        // Set Sprites
        if (PoleKickManager.TryGetManager(self.player.abstractCreature, out var PKM))
        {
            if (PKM.bodyPartInMG.Count == 0)
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (sLeaser.sprites[i].container == rCam.ReturnFContainer("Midground"))
                    {
                        PKM.bodyPartInMG.Add(i);
                    }
                }
            }

            if (PKM.bodyInFrontOfPole && !PKM.lastBodyInFrontOfPole)
            {
                foreach (int i in PKM.bodyPartInMG)
                {
                    rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
                }
            }
            else if (!PKM.bodyInFrontOfPole && PKM.lastBodyInFrontOfPole)
            {
                foreach (int i in PKM.bodyPartInMG)
                {
                    rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[i]);
                }
            }
            PKM.lastBodyInFrontOfPole = PKM.bodyInFrontOfPole;
        }
        if (CoreFunc.IsCore(self.player))
        {
            sLeaser.sprites[0].scaleX += 0.1f;
            sLeaser.sprites[1].scaleX += 0.15f;
        }
        else if (SparkFunc.IsSpark(self.player))
        {
            if (skinloaded)
            {
                if (Futile.atlasManager.DoesContainAtlas("BodyASpark") && !sLeaser.sprites[0].element.name.Contains("BodyASpark"))
                {
                    sLeaser.sprites[0].SetElementByName("BodyASpark");
                }
                if (Futile.atlasManager.DoesContainAtlas("HipsASpark") && !sLeaser.sprites[1].element.name.Contains("HipsASpark"))
                {
                    sLeaser.sprites[1].SetElementByName("HipsASpark");
                }
            }

            if (ModManager.MSC && !sLeaser.sprites[3].element.name.Contains("HeadB"))
            {
                sLeaser.sprites[3].SetElementByName($"HeadB{sLeaser.sprites[3].element.name.Substring("HeadA".Length)}");
            }

            float bonusfluff = 1f;
            const float radxAnim = 1f;
            if (self.player != null && StaticChargeManager.TryGetManager(self.player.abstractCreature, out var SCM))
            {
                bonusfluff = Mathf.Clamp01(SCM.FullECharge > 0 ? SCM.Charge / SCM.FullECharge : 1) 
                    + Mathf.Clamp01(SCM.MaxECharge > 0 && SCM.MaxECharge > SCM.FullECharge ? (SCM.Charge - SCM.FullECharge) / (SCM.MaxECharge - SCM.FullECharge) : 1);
                
                if (SCM.CrawlChargeConditionMet)
                {
                    float freqAnim = 8f * 2f * Mathf.PI / BTWFunc.FrameRate;
                    bonusfluff = Mathf.Max(Mathf.Pow(SCM.CrawlChargeRatio, 2) * 2, bonusfluff);
                    sLeaser.sprites[0].x += Mathf.Cos(SCM.crawlCharge * freqAnim) * Mathf.Max(0.25f, SCM.CrawlChargeRatio) * radxAnim;

                    sLeaser.sprites[1].x += Mathf.Cos(SCM.crawlCharge * freqAnim + Mathf.PI) * Mathf.Max(0.25f, SCM.CrawlChargeRatio) * radxAnim;
                }
            }
            sLeaser.sprites[0].scaleX += -0.1f + 0.15f * bonusfluff;
            sLeaser.sprites[1].scaleX += -0.1f + 0.2f * bonusfluff;

        }
        else if (TrailseekerFunc.IsTrailseeker(self.player))
        {
            if (sLeaser.sprites[9].scaleX > 0f)
            {
                sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("TrailseekerD" + sLeaser.sprites[9].element.name);
            }
            else
            {
                sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("TrailseekerG" + sLeaser.sprites[9].element.name);
            }
        }
    }

    private static float ChangeSlugHeight(float orig, Player player)
    {
        if (SparkFunc.IsSpark(player))
        {
            return 15f;
        }
        return orig;
    }
    private static void Player_ModifyPlayerHeight(ILContext il)
    {
        Plugin.Log("BTWSkins IL 2 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(1),
                x => x.MatchNewarr<PhysicalObject.BodyChunkConnection>(),
                x => x.MatchStfld<PhysicalObject>(nameof(PhysicalObject.bodyChunkConnections)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PhysicalObject>(nameof(PhysicalObject.bodyChunkConnections)),
                x => x.MatchLdcI4(0),
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.bodyChunks)).GetGetMethod()),
                x => x.MatchLdcI4(0),
                x => x.MatchLdelemRef(),
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.bodyChunks)).GetGetMethod()),
                x => x.MatchLdcI4(1),
                x => x.MatchLdelemRef(),
                x => x.MatchLdcI4(17)
            ))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ChangeSlugHeight);
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
            Plugin.Log(il);
        }
        Plugin.Log("BTWSkins IL 2 ends");
    }
    private static void Modify_Player_Sprite(ILContext il) // blatandly copied from MagicaJaphet : Extended Slugbase Features. Sorry I really don't get IL atm...
    {
        Plugin.Log("BTWSkins IL 1 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            // gown.InitiateSprite(this.gownIndex, sLeaser, rCam);
            if (cursor.TryGotoNext(MoveType.Before, 
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(1),
                x => x.MatchLdarg(2),
                x => x.MatchCallOrCallvirt<GraphicsModule>(nameof(GraphicsModule.AddToContainer)),
                x => x.MatchBr(out _)
                ))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldarg_2);
                static void InitiateSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
                {
                    // Load skin if failed
                    if (!skinloaded)
                    {
                        Plugin.logger.LogError("Skin not loaded ! Loading them now...");
                        LoadSkins();
                    }
                    if (SparkFunc.IsSpark(self.player))
                    {
                        if (Futile.atlasManager.DoesContainAtlas("BodyASpark") 
                            && Futile.atlasManager.DoesContainAtlas("HipsASpark"))
                        {
                            sLeaser.sprites[0].SetElementByName("BodyASpark");
                            sLeaser.sprites[1].SetElementByName("HipsASpark");
                        }
                        if (ModManager.MSC)
                        {
                            sLeaser.sprites[3].SetElementByName("HeadB0");
                        }
                    }
                    else if (TrailseekerFunc.IsTrailseeker(self.player))
                    {
                        
                    }
                }
                cursor.EmitDelegate(InitiateSprites);
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("BTWSkins IL 1 ends");
    }
} 