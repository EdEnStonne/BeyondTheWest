using UnityEngine;
using BeyondTheWest;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using DressMySlugcat;

namespace BeyondTheWest.DressMySlugcatCompat;
public static class BTWDMSDressing
{
    // Hooks
    public static void ApplyHooks()
    {
        new Hook(typeof(SpriteDefinitions).GetMethod(nameof(SpriteDefinitions.Init)), SetCustomDMSSkin);
        BTWPlugin.Log("BTWDMSDressing ApplyHooks Done !");
    }

    private static void SetCustomDMSSkin(Action orig)
    {
        orig();
        foreach (SpriteDefinitions.AvailableSprite availableSprite in SpriteDefinitions.AvailableSprites)
        {
            if (availableSprite.Name == "HEAD")
            {
                for (int i = 0; i <= 17; i++)
                {
                    availableSprite.SlugcatSpecificReplacements.Add(new()
                    {
                        Slugcat = "Spark",
                        GenericName = "HeadA" + i,
                        SpecificName = "HeadASpark" + i
                    });
                }
            }
            else if (availableSprite.Name == "FACE")
            {
                for (int i = 0; i <= 8; i++)
                {
                    availableSprite.SlugcatSpecificReplacements.Add(new()
                    {
                        Slugcat = "Trailseeker",
                        GenericName = "FaceA" + i,
                        SpecificName = "TrailseekerD" + i
                    });

                    availableSprite.SlugcatSpecificReplacements.Add(new()
                    {
                        Slugcat = "Trailseeker",
                        GenericName = "FaceB" + i,
                        SpecificName = "TrailseekerG" + i
                    });
                }
            }
            else if (availableSprite.Name == "BODY")
            {
                availableSprite.SlugcatSpecificReplacements.Add(new()
                {
                    Slugcat = "Spark",
                    GenericName = "BodyA",
                    SpecificName = "BodyASpark"
                });
            }
            else if (availableSprite.Name == "HIPS")
            {
                availableSprite.SlugcatSpecificReplacements.Add(new()
                {
                    Slugcat = "Spark",
                    GenericName = "HipsA",
                    SpecificName = "HipsASpark"
                });
            }
        }
    }
}