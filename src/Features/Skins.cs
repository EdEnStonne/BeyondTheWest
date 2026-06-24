using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using MonoMod.Cil;
using BeyondTheWest;
using Mono.Cecil.Cil;

public class BTWSkins
{
    public static void ApplyHooks()
    {
        On.PlayerGraphics.DrawSprites += Player_Sprite;
        BTWPlugin.Log("BTWSkins ApplyHooks Done !");
    }

    // Functions
    public static void LoadSkins()
    {
        Futile.atlasManager.ActuallyLoadAtlasOrImage("BodyASpark", "skin/Spark/body", "skin/Spark/body");
        Futile.atlasManager.ActuallyLoadAtlasOrImage("HipsASpark", "skin/Spark/hips", "skin/Spark/hips");
        Futile.atlasManager.ActuallyLoadAtlasOrImage("HeadASpark", "skin/Spark/head", "skin/Spark/head");

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
        BTWPlugin.Log("BTWSkins LoadSkin Done !");
    }

    // Hooks
    private static void Player_Sprite(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

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
            // if (!BTWPlugin.DMSEnabled)
            {
                if (!sLeaser.sprites[3].element.name.Contains("HeadASpark"))
                {
                    // BTWPlugin.Log($"Changing Spark head [{sLeaser.sprites[3].element.name}] to [{headname}]<{Futile.atlasManager.DoesContainElementWithName(headname)}>");
                    string headname = $"HeadASpark{sLeaser.sprites[3].element.name.Substring("HeadA".Length)}";
                    sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName(headname);
                }
                if (!sLeaser.sprites[0].element.name.Contains("BodyASpark"))
                {
                    sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName("BodyASpark");
                }
                if (!sLeaser.sprites[1].element.name.Contains("HipsASpark"))
                {
                    sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("HipsASpark");
                }
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
                if (SCM.endlessCharge > 0)
                {
                    bonusfluff = 2;
                }
            }
            sLeaser.sprites[0].scaleX += -0.15f + 0.2f * bonusfluff;
            sLeaser.sprites[1].scaleX += -0.15f + 0.25f * bonusfluff;

        }
        else if (TrailseekerFunc.IsTrailseeker(self.player)) //&& !BTWPlugin.DMSEnabled
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
} 