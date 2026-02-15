using UnityEngine;
using System;
using RWCustom;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest;

public class BTWCreatureData : BTWManager<BTWCreatureData>
{
    public static void AddManager(AbstractCreature creature, out BTWCreatureData BTWCD)
    {
        BTWCD = new(creature);
        AddNewManager(creature, BTWCD);
    }
    public static void AddManager(AbstractCreature creature)
    {
        AddManager(creature, out _);
    }
    public BTWCreatureData(AbstractCreature abstractCreature) : base(abstractCreature)
    {
        
    }

    public override void Update()
    {
        base.Update();
    }
    

    public bool voidSparkImmune = false;
    public bool crystalInfected = false;
    public bool electricExplosionImmune = false;
}
public static class BTWCreatureDataHooks
{
    public static void ApplyHooks()
    {
        IL.Creature.ctor += Creature_BTWCreatureData_Init; //So it starts first garanteed
        On.Creature.Update += Creature_BTWCreatureData_Update; //Same here
        BTWPlugin.Log("BTWCreatureDataHooks ApplyHooks Done !");
    }
    
    private static void AddNewManager(AbstractCreature abstractCreature)
    {
        if (!BTWCreatureData.TryGetManager(abstractCreature, out _))
        {
            BTWCreatureData.AddManager(abstractCreature);
            BTWPlugin.Log($"BTWCreatureData created for [{abstractCreature}] !");
        }
    }
    private static void Creature_BTWCreatureData_Init(ILContext il)
    {
        BTWPlugin.Log("BTWCreatureData IL 1 starts");
        try
        {
            BTWPlugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            cursor.Goto(il.Body.Instructions.Count - 1, MoveType.After);
            if (cursor.TryGotoPrev(MoveType.Before,  x => x.MatchRet()))
            {
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(AddNewManager);
            }
            else
            {
                BTWPlugin.logger.LogError("Couldn't find IL hook :<");
            }
            BTWPlugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            BTWPlugin.logger.LogError(ex);
        }
        BTWPlugin.Log("BTWCreatureData IL 1 ends");
    }

    private static void Creature_BTWCreatureData_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        orig(self, eu);
        self.GetBTWData()?.Update();
    }
}