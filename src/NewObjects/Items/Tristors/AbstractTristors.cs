using UnityEngine;
using BeyondTheWest;
using System.Collections.Generic;
using BeyondTheWest.MeadowCompat;
using System;
using System.Linq;

namespace BeyondTheWest.Items;

public class AbstractTristor : AbstractPhysicalObject
{
    public static List<IconLayerCount> TristorLayersCount = new()
    {
        new() {name = "Core", count = 7},
        new() {name = "Rock", count = 7},
        new() {name = "Shard", count = 7}
    };
    public static AbstractObjectType TristorType;
    public static MultiplayerUnlocks.SandboxUnlockID TristorUnlock;
    public static IconSymbol.IconSymbolData TristorIconData;
    public static string TristorIconName = "TristorIcon";
    public static Color TristorIconColor = new Color(0.4f, 0.8f, 1f);
    public AbstractTristor(World world, Tristor realizedObject, WorldCoordinate pos, EntityID ID) : base(world, TristorType, null, pos, ID)
    {
        this.realizedObject = realizedObject;
        this.charge = (int)BTWFunc.RandomWeighted(2, 5, 2f);

        this.mainHue = BTWFunc.Random(0.3f, 0.7f);
        this.secHue = this.mainHue + BTWFunc.Random(-0.1f, 0.1f);

        this.shard1Type = BTWFunc.RandInt(1, TristorLayersCount.Find(x => x.name == "Shard").count);
        this.shard2Type = BTWFunc.RandInt(1, TristorLayersCount.Find(x => x.name == "Shard").count);
        this.rockType = BTWFunc.RandInt(1, TristorLayersCount.Find(x => x.name == "Rock").count);
        this.coreType = BTWFunc.RandInt(1, TristorLayersCount.Find(x => x.name == "Core").count);

        this.rotationOffset = new float[2];
        for (int i = 0; i < this.rotationOffset.Length; i++)
        {
            this.rotationOffset[i] = BTWFunc.Random(360);
        }
    }

    public int charge = 0;
    public float mainHue = 0.5f;
    public float secHue = 0.5f;
    public int shard1Type = 1;
    public int shard2Type = 1;
    public int rockType = 1;
    public int coreType = 1;
    public float[] rotationOffset;
    public bool isMeadowInit = false;
    public bool Charged
    {
        get
        {
            return charge > 0;
        }
    }
    public override void Update(int time)
    {
        base.Update(time);
        
        if (BTWPlugin.meadowEnabled && !this.isMeadowInit)
        {
            MeadowCalls.BTWItems_AbstractTristorInit(this);
        }
    }
    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new Tristor(this);
    }


    public override string ToString()
    {
        return SaveHelper.ToSaveString(this, 
            charge, 
            mainHue, secHue, 
            shard1Type, shard2Type, rockType, coreType,
            rotationOffset[0], rotationOffset[1]);
    }
}

public static class TristorHooks
{
    public static void Register()
    {
        Futile.atlasManager.ActuallyLoadAtlasOrImage(AbstractTristor.TristorIconName, "icons/icon_Tristor", "");
        foreach (IconLayerCount iconLayer in AbstractTristor.TristorLayersCount)
        {
            for (int i = 1; i <= iconLayer.count; i++)
            {
                Futile.atlasManager.ActuallyLoadAtlasOrImage(
                    $"Tristor{iconLayer.name}{i}", $"assets/Tristor/{iconLayer.name}_Tristor_{i}", "");
            }
        }

        AbstractTristor.TristorType = new("Tristor", true);
        AbstractTristor.TristorUnlock = new("Tristor", true);
        AbstractTristor.TristorIconData = new(CreatureTemplate.Type.StandardGroundCreature, AbstractTristor.TristorType, 0);
        MultiplayerUnlocks.ItemUnlockList.Add(AbstractTristor.TristorUnlock);

        Tristor.State.Idle = new("Idle", true);
        Tristor.State.Searching = new("Searching", true);
        Tristor.State.Positioning = new("Positioning", true);
        Tristor.State.Static = new("Static", true);
        Tristor.State.Colapse = new("Colapse", true);
        
        BTWPlugin.Log($"Registered AbstractTristor ! Type : [{AbstractTristor.TristorType}], Unlock [{AbstractTristor.TristorUnlock}]");   
    }
    public static void ApplyHooks()
    {
        BTWPlugin.Log("TristorHooks ApplyHooks Done !");    
    }
}