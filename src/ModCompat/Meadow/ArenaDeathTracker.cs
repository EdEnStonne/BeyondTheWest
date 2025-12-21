using System;
using RainMeadow;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using BeyondTheWest.MSCCompat;

namespace BeyondTheWest.MeadowCompat;

public struct MeadowDeathMessage
{
    public MeadowDeathMessage() { } 
    public MeadowDeathMessage(int contextNum, string deathMessagePre, string deathMessagePost)
    {
        this.contextNum = Mathf.Max(contextNum, 10);
        this.deathMessagePre = deathMessagePre;
        this.deathMessagePost = deathMessagePost;
    }        
    public int contextNum = 0;
    public string deathMessagePre = "was slain by";
    public string deathMessagePost = ".";
}
public class ArenaDeathTracker
{
    public static List<MeadowDeathMessage> customDeathMessagesEnum = new();
    public static ConditionalWeakTable<AbstractCreature, ArenaDeathTracker> arenaDeathTrackers = new();
    public static bool TryGetTracker(AbstractCreature target, out ArenaDeathTracker deathTracker)
    {
        deathTracker = null;
        return arenaDeathTrackers.TryGetValue(target, out deathTracker);
    }
    public static ArenaDeathTracker GetTracker(AbstractCreature target)
    {
        TryGetTracker(target, out ArenaDeathTracker tracker);
        return tracker;
    }
    public static void AddTracker(AbstractCreature target, out ArenaDeathTracker deathTracker)
    {
        deathTracker = new(target);
        arenaDeathTrackers.Add(target, deathTracker);
    }
    public static void AddTracker(AbstractCreature target)
    {
        arenaDeathTrackers.Add(target, new(target));
    }
    
    public static bool TryGetDeathMessage(int enumValue, out MeadowDeathMessage deathMessage)
    {
        deathMessage = new();
        if (customDeathMessagesEnum.Exists(x => x.contextNum == enumValue))
        {
            deathMessage = customDeathMessagesEnum.Find(x => x.contextNum == enumValue);
            return true;
        }
        return false;
    }
    public static MeadowDeathMessage GetDeathMessage(int enumValue)
    {
        TryGetDeathMessage(enumValue, out MeadowDeathMessage deathMessage);
        return deathMessage;
    }
    public static void AddDeathMessage(int contextNum, string deathMessagePre, string deathMessagePost)
    {
        if (TryGetDeathMessage(contextNum, out var deathMessage))
        {
            customDeathMessagesEnum.Remove(deathMessage);
        }
        customDeathMessagesEnum.Add( new(contextNum, deathMessagePre, deathMessagePost) );
    }

    public static void SetDeathTrackerOfCreature(AbstractCreature target, int enumValue)
    {
        if (TryGetTracker(target, out var deathTracker))
        {
            deathTracker.SetDeathTrackerOfCreature(enumValue);
        }
    }
    public static int GetCustomDeathMessageOfViolence(Creature target, ArenaDeathTracker deathTracker, PhysicalObject owner, Creature.DamageType type, float damage, float stunBonus)
    {
        if (target == null || target.dead || target.room == null || deathTracker == null) { return 0; }

        if (owner != null)
        {
            if (owner is Spear spear && spear != null)
            {
                if (spear.thrownBy != null)
                {
                    if ((target.mainBodyChunk.pos - spear.thrownBy.mainBodyChunk.pos).magnitude > 750
                        && BTWFunc.random < 0.25f)
                    {
                        return 17;
                    }
                    else if (BTWFunc.BodyChunkSumberged(spear.thrownBy.mainBodyChunk)
                        && BTWFunc.BodyChunkSumberged(target.mainBodyChunk)
                        && BTWFunc.random < 0.25f)
                    {
                        return 18;
                    }
                }
                if (spear.thrownBy is Player playerkiller
                    && playerkiller != null)
                {
                    if (playerkiller.animation == Player.AnimationIndex.Flip
                        && BTWFunc.random < 0.25f)
                    {
                        return 16;
                    }
                    else if (ModManager.MSC && MSCFunc.IsArtificer(playerkiller)
                        && BTWFunc.BodyChunkSumberged(playerkiller.mainBodyChunk)
                        && BTWFunc.BodyChunkSumberged(target.mainBodyChunk))
                    {
                        return 37;
                    }
                    else if (playerkiller.isSlugpup
                        && BTWFunc.random < 0.2f)
                    {
                        return 38;
                    }
                }

                if (spear is ExplosiveSpear)
                {
                    return 11;
                }
                else if (ModManager.MSC && MSCFunc.IsElectricSpear(spear))
                {
                    return 12;
                }

                if (spear.spearmasterNeedle && spear.spearmasterNeedle_hasConnection)
                {
                    if (BTWFunc.random < 0.05f) { return 15; }
                    return 14;
                }
                return 10;
            }
            else if (owner is Player player && player != null)
            {
                if (type == Creature.DamageType.Bite)
                {
                    if (ModManager.MSC && MSCFunc.IsArtificer(player))
                    {
                        return 36;
                    }
                    return 30;
                }
                else if (type == Creature.DamageType.Blunt)
                {
                    if (CoreFunc.IsCore(player))
                    {
                        return 35;
                    }
                    else
                    {
                        if (damage == 1f && stunBonus == 120f) { return 34; }
                    }
                    return 32;
                }
                else if (type == Creature.DamageType.Electric && SparkFunc.IsSpark(player))
                {
                    if (BTWFunc.BodyChunkSumberged(player.mainBodyChunk)
                        && BTWFunc.BodyChunkSumberged(target.mainBodyChunk))
                    {
                        return 24;
                    }
                    if (damage > 1.5f) { return 23; }
                    return 22;
                }
                else if (type == Creature.DamageType.Explosion && CoreFunc.IsCore(player))
                {
                    if (damage > 2f) { return 21; }
                    return 20;
                }
            }
            else if (owner is EnergyCore energycore && energycore != null)
            {
                if (damage > 2f) { return 21; }
                return 20;
            }
            else if (owner is Rock rock && rock != null)
            {
                return 33;
            }
        }

        if (type == Creature.DamageType.Bite)
        {
            return 52;
        }
        else if (type == Creature.DamageType.Blunt)
        {
            return 51;
        }
        else if (type == Creature.DamageType.Electric)
        {
            return 55;
        }
        else if (type == Creature.DamageType.Explosion)
        {
            return 54;
        }
        else if (type == Creature.DamageType.Stab)
        {
            return 50;
        }
        else if (type == Creature.DamageType.Water)
        {
            return 53;
        }

        return 0;
    }
    
    public ArenaDeathTracker(AbstractCreature creature)
    {
        this.creature = creature;
    }
    public void SetDeathTrackerOfCreature(int enumValue)
    {
        this.deathMessageCustom = enumValue < 10 ? 0 : enumValue;
    }

    public AbstractCreature creature;
    public int deathMessageCustom = 0;
}

public static class ArenaDeathTrackerHooks
{
    public static void ApplyHooks()
    {
        InitDeathMessages();

        On.Creature.ctor += Creature_AddDeathTracker;
        On.Creature.Update += Creature_UpdateDeathTracker;
        On.Creature.Violence += Creature_ViolenceDeathTracker;
        On.Lizard.Violence  += Lizard_ViolenceDeathTracker;
        
        new ILHook(typeof(DeathMessage).GetMethod(nameof(DeathMessage.CreatureDeath)), DeathMessage_ChangeContextFromTracker);
        new ILHook(typeof(DeathMessage).GetMethod(nameof(DeathMessage.PlayerKillPlayer)), DeathMessage_GetNewDeathMessageFromTracker);
        new ILHook(typeof(DeathMessage).GetMethod(nameof(DeathMessage.PlayerKillCreature)), DeathMessage_GetNewDeathMessageFromTracker);
        
        Plugin.Log("ArenaDeathTrackerHooks ApplyHooks Done !");
    }
    
    public static void LogAllDeathMessages()
    {
        Plugin.Log("Here's all custom death messages :");
        foreach (var m in ArenaDeathTracker.customDeathMessagesEnum)
        {
            Plugin.Log($"   > [{m.contextNum}] : \"target {m.deathMessagePre} killer{m.deathMessagePost}\"");
        }
    }
    public static void InitDeathMessages()
    {
        ArenaDeathTracker.AddDeathMessage(10, "was speared by", ".");
        ArenaDeathTracker.AddDeathMessage(11, "was speared by", " using an explosive spear.");
        ArenaDeathTracker.AddDeathMessage(12, "was speared by", " using an electric spear.");
        ArenaDeathTracker.AddDeathMessage(13, "was speared by", " using a poisonous spear.");
        ArenaDeathTracker.AddDeathMessage(14, "was reduced into a single food pip to", ".'");
        ArenaDeathTracker.AddDeathMessage(15, "was given an involuntary umbilical by", ".'");
        ArenaDeathTracker.AddDeathMessage(16, "was 360 no scoped by", ".");
        ArenaDeathTracker.AddDeathMessage(17, "was sniped by", ".");
        ArenaDeathTracker.AddDeathMessage(18, "was spear-fished by", ".");
 
        ArenaDeathTracker.AddDeathMessage(20, "was blown apart by", ".");
        ArenaDeathTracker.AddDeathMessage(21, "was obliterated by", ".");
        ArenaDeathTracker.AddDeathMessage(22, "was zip-zapped by", ".");
        ArenaDeathTracker.AddDeathMessage(23, "was given a sudden cardiac arrest by", ".");
        ArenaDeathTracker.AddDeathMessage(24, "took a bath with a toaster named", ".");
        ArenaDeathTracker.AddDeathMessage(25, "was clawed to death by", ".");
        ArenaDeathTracker.AddDeathMessage(26, "was dropkicked by", ".");

        ArenaDeathTracker.AddDeathMessage(30, "was mauled to death by", ".");
        ArenaDeathTracker.AddDeathMessage(31, "was thrown into the void by", ".");
        ArenaDeathTracker.AddDeathMessage(32, "didn't stand a chance against", "'s sheer momentum.");
        ArenaDeathTracker.AddDeathMessage(33, "was bonked to death by", ".");
        ArenaDeathTracker.AddDeathMessage(34, "was slugrolled by", ".");
        ArenaDeathTracker.AddDeathMessage(35, "was flatened by", "'s sheer momentum.");
        ArenaDeathTracker.AddDeathMessage(36, "was brutally mauled into pieces by", ".");
        ArenaDeathTracker.AddDeathMessage(37, "was still not safe from", "'s bloodlust underwater.");
        ArenaDeathTracker.AddDeathMessage(38, "clearly under-estimated", "'s ability to kill.");

        ArenaDeathTracker.AddDeathMessage(40, "was doomed to die by", ".");

        ArenaDeathTracker.AddDeathMessage(50, "was stabbed by", ".");
        ArenaDeathTracker.AddDeathMessage(51, "was crushed by", ".");
        ArenaDeathTracker.AddDeathMessage(52, "was brutally crushed in the jaws of", ".");
        ArenaDeathTracker.AddDeathMessage(53, "took water damage from", ".");
        ArenaDeathTracker.AddDeathMessage(54, "was exploded by", ".");
        ArenaDeathTracker.AddDeathMessage(55, "was zapped by", ".");

        LogAllDeathMessages();

        Plugin.Log("MeadowCompat custom kill messages init !");
    }
    public static void Creature_AddDeathTracker(On.Creature.orig_ctor orig, Creature self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (world.game.IsArenaSession && !ArenaDeathTracker.TryGetTracker(abstractCreature, out _))
        {
            ArenaDeathTracker.AddTracker(abstractCreature);
            Plugin.Log($"DeathTracker added to [{self}] !");
        }
    }
    public static void Creature_UpdateDeathTracker(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        // Plugin.Log($"Update detected ! Attemping to set DeathTracker of [{self}]...");
        orig(self, eu);
        if (ArenaDeathTracker.TryGetTracker(self.abstractCreature, out var deathTracker))
        {
            if (deathTracker.deathMessageCustom != 0 && self.killTagCounter <= 0)
            {
                deathTracker.deathMessageCustom = 0;
                Plugin.Log($"DeathTracker of [{self}] set back to 0.");
            }
        }
    }
    
    private static void ViolenceCheck(Creature self, BodyChunk source, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self != null 
            && self.abstractCreature != null 
            && ArenaDeathTracker.TryGetTracker(self.abstractCreature, out var deathTracker))
        {
            int newContext = ArenaDeathTracker.GetCustomDeathMessageOfViolence(self, deathTracker, source?.owner, type, damage, stunBonus);
            deathTracker.deathMessageCustom = newContext;
            Plugin.Log($"DeathTracker of [{self}] set to <{newContext}> !");
        }
    }
    private static void Lizard_ViolenceDeathTracker(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
    {
        Plugin.Log($"Violence (on lizard) detected ! Attemping to set DeathTracker of [{self}]...");
        ViolenceCheck(self, source, type, damage, stunBonus);
        orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
    }
    public static void Creature_ViolenceDeathTracker(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        Plugin.Log($"Violence detected ! Attemping to set DeathTracker of [{self}]...");
        ViolenceCheck(self, source, type, damage, stunBonus);
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }

    private static int ChangeContext(int orig, Creature creature)
    {
        if (ArenaDeathTracker.TryGetTracker(creature.abstractCreature, out var deathTracker) 
            && deathTracker.deathMessageCustom >= 10)
        {
            Plugin.Log($"[{creature}] death context changed to <{deathTracker.deathMessageCustom}> !");
            return deathTracker.deathMessageCustom;
        }
        return orig;
    }
    private static void DeathMessage_ChangeContextFromTracker(ILContext il)
    {
        Plugin.Log("MeadowCompat IL 1 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Creature>(nameof(Creature.killTag)),
                x => x.MatchCallOrCallvirt(typeof(AbstractCreature).GetProperty(nameof(AbstractCreature.realizedCreature)).GetGetMethod()),
                x => x.MatchIsinst(typeof(Player)),
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0)
            ))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(ChangeContext);
            }
            else
            {
                Plugin.logger.LogError("Couldn't find IL hook :<");
                Plugin.Log(il);
            }
            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("MeadowCompat IL 1 ends");
    }

    private static string ChangeContextPre(string orig, int context)
    {
        Plugin.Log($"[{orig}] death message detected (pre), with context <{context}>.");
        if (ArenaDeathTracker.TryGetDeathMessage(context, out var deathMessage))
        { 
            string newText = deathMessage.deathMessagePre;
            Plugin.Log($"[{orig}] death message changed to <{newText}> !");
            return newText; 
        }
        else if (context >= 10)
        {
            Plugin.Log("Couldn't find the custom message...?");
            LogAllDeathMessages();
        }
        return orig;
    }
    private static string ChangeContextPost(string orig, int context)
    {
        Plugin.Log($"[{orig}] death message detected (pos), with context <{context}>.");
        if (ArenaDeathTracker.TryGetDeathMessage(context, out var deathMessage))
        { 
            string newText = deathMessage.deathMessagePost;
            Plugin.Log($"[{orig}] death message changed to <{newText}> !");
            return newText; 
        }
        return orig;
    }
    private static void DeathMessage_GetNewDeathMessageFromTracker(ILContext il)
    {
        Plugin.Log("MeadowCompat IL 2 starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);

            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdstr("was slain by")
            ))
            {
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate(ChangeContextPre);
            }
            else
            {
                Plugin.logger.LogError("Couldn't find IL hook 1 :<");
                Plugin.Log(il);
            }
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdstr(".")
            ))
            {
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate(ChangeContextPost);
            }
            else
            {
                Plugin.logger.LogError("Couldn't find IL hook 2 :<");
                Plugin.Log(il);
            }

            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        Plugin.Log("MeadowCompat IL 2 ends");
    }
}