using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest;

public abstract class BTWManager<TSelf> where TSelf : BTWManager<TSelf>
{
    public static ConditionalWeakTable<AbstractCreature, TSelf> managers = new();
    public static bool TryGetManager(AbstractCreature creature, out TSelf manager)
    {
        return managers.TryGetValue(creature, out manager);
    }
    public static TSelf GetManager(AbstractCreature creature)
    {
        TryGetManager(creature, out TSelf manager);
        return manager;
    }
    public static void AddNewManager(AbstractCreature creature, TSelf manager)
    {
        RemoveManager(creature);
        managers.Add(creature, manager);
    }
    public static void RemoveManager(AbstractCreature creature)
    {
        if (TryGetManager(creature, out _))
        {
            managers.Remove(creature);
        }
    }
    
    public BTWManager(AbstractCreature abstractCreature)
    {
        this.abstractCreature = abstractCreature;
    }

    public virtual void Update()
    {
        
    }
    
    // ------ Variables

    // Objects
    public AbstractCreature abstractCreature;

    // Basic
    
    // Get - Set
    public Room RealizedRoom => abstractCreature?.realizedCreature?.room;
}