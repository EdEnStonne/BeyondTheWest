using UnityEngine;
using BeyondTheWest;
using System.Collections.Generic;
using BeyondTheWest.MeadowCompat;
using System;
using System.Linq;
using System.Globalization;

namespace BeyondTheWest.Items;

public class AbstractCrystalSpear : AbstractSpear
{
    public static MultiplayerUnlocks.SandboxUnlockID CrystalSpearUnlock;
    public static IconSymbol.IconSymbolData CrystalSpearIconData;
    public static string CrystalSpearIconName = "CrystalSpearIcon";
    public static Color CrystalSpearIconColor = new Color(1f, 0.1f, 0.8f);
    public AbstractCrystalSpear(World world, CrystalSpear realizedObject, WorldCoordinate pos, EntityID ID) : base(world, realizedObject, pos, ID, false)
    {
        this.realizedObject = realizedObject;
        this.appearance = new(new AbstractVoidCrystal.VoidCrystalAppearance.Settings(5, 0, 0, 1));
        for (int i = 1; i < this.appearance.layerRotation.Length; i++)
        {
            this.appearance.layerRotation[i] = this.appearance.layerRotation[i] <= 1 ? 2 : 3;
        }
        this.blue = BTWFunc.Random(0.3f, 0.8f);
        this.red = Mathf.Clamp01(BTWFunc.Random(0.5f, 2f));
    }

    public bool isMeadowInit = false;
    public float blue = 0.5f;
    public float red = 1f;
    public AbstractVoidCrystal.VoidCrystalAppearance appearance;
    public override void Update(int time)
    {
        base.Update(time);
        
        if (BTWPlugin.meadowEnabled && !this.isMeadowInit)
        {
            MeadowCalls.BTWItems_AbstractCrystalSpearInit(this);
        }
    }
    public override void Realize()
    {
        if (this.realizedObject != null) { return; }
        realizedObject = new CrystalSpear(this)
        {
            baseColor = new(red, 0.2f, blue)
        };

        for (int i = 0; i < this.stuckObjects.Count; i++)
        {
            if (this.stuckObjects[i].A.realizedObject == null && this.stuckObjects[i].A != this)
            {
                this.stuckObjects[i].A.Realize();
            }
            if (this.stuckObjects[i].B.realizedObject == null && this.stuckObjects[i].B != this)
            {
                this.stuckObjects[i].B.Realize();
            }
        }
        base.Realize();
    }


    public override string ToString()
    {
        string text = string.Format(CultureInfo.InvariantCulture, "{0}<oA>{1}<oA>{2}<oA>{3}<oA>{4}<oA>{5}<oA>{6}<oA>{7}", new object[]
        {
            this.IDAndRippleLayerString,
            this.type.ToString(),
            this.pos.SaveToString(),
            this.stuckInWallCycles,
            "BTWC",
            appearance.ToString(), 
            ((int)(blue * 100)).ToString(), 
            ((int)(red * 100)).ToString(), 
        });
        return text;
    }
}

public static class CrystalSpearHooks
{
    public static void Register()
    {
        Futile.atlasManager.ActuallyLoadAtlasOrImage(AbstractCrystalSpear.CrystalSpearIconName, "icons/icon_SpearCrystal", "");

        AbstractCrystalSpear.CrystalSpearUnlock = new("CrystalSpear", true);
        AbstractCrystalSpear.CrystalSpearIconData = new(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 5);
        MultiplayerUnlocks.ItemUnlockList.Add(AbstractCrystalSpear.CrystalSpearUnlock);
        
        BTWPlugin.Log($"Registered AbstractCrystalSpear ! Type : [{AbstractPhysicalObject.AbstractObjectType.Spear}], Unlock [{AbstractCrystalSpear.CrystalSpearUnlock}]");   
    }
    public static void ApplyHooks()
    {
        BTWPlugin.Log("VoidCrystalHooks ApplyHooks Done !");    
    }
}