using System;
using RainMeadow;
using System.Linq;
using UnityEngine;
using BeyondTheWest.MSCCompat;
using BeyondTheWest.ArenaAddition;
using System.Collections.Generic;
using BeyondTheWest.MeadowCompat.Data;

namespace BeyondTheWest.MeadowCompat;
public static class MeadowRPCs
{
    [RPCMethod]
    public static void BTWVersionChecker_VersionMismatch(RPCEvent rpc, string subscribedVersion)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.isOwner && rpc.from != null)
        {
            ChatLogManager.LogSystemMessage(rpc.from.id.GetPersonaName() + " " 
                + BTWFunc.Translate("doesn't have the good version of Beyond the West ! (Player : ") 
                + $"{subscribedVersion} / " + BTWFunc.Translate("Host : " ) + $"{BTWPlugin.MOD_VERSION})");
        }
    }
    [RPCMethod]
    public static void PoleKickManager_Kick(RPCEvent rpc, OnlineCreature playerOpo, 
        OnlineCreature targetOpo, byte chuckIndex, Vector2 knockback, byte kBonusCent)
    {
        AbstractCreature abstractTarget = targetOpo.abstractCreature;
        AbstractCreature abstractPlayer = playerOpo.abstractCreature;
        if (abstractTarget == null || abstractPlayer == null) { return; }

        Player player = abstractPlayer.realizedCreature as Player;
        Creature target = abstractTarget.realizedCreature;
        if (player == null || target == null) { return; }

        BodyChunk chunkHit = target.mainBodyChunk;
        if (target.bodyChunks.Length > chuckIndex) { chunkHit = target.bodyChunks[chuckIndex]; }

        PoleKickManager.HitCreatureWithKick(player, chunkHit, knockback, kBonusCent / 100f);
        
        BTWPlugin.Log($"Player [{player}] did a kick on [{target}] with <{knockback}> and <{kBonusCent / 100f}> knockback bonus !");
    }
    [RPCMethod]
    public static void Spark_SparkExplosion(RPCEvent rpc, OnlinePhysicalObject playerOpo, short size, 
        Vector2 position, byte sparks, byte volumeCent)
    {
        // Plugin.Log("Opening the RPC : " + playerOpo.ToString() + "/" + size.ToString() + "/" + position.ToString() + "/" + sparks.ToString() + "/" + volumeCent.ToString());
        var SCM = MeadowFunc.GetOnlinePlayerStaticChargeManager(playerOpo);
        if (SCM == null) { return; }
        if (SCM.active || !SCM.isMeadowFakePlayer) { return; }
        // Plugin.Log("Checking some stuff :" + SCM.ToString() + "/" + SCM.active + "/" + SCM.isMeadowFakePlayer);

        float volume = volumeCent;
        volume /= 100f;
        Player player = SCM.Player;
        Room room = SCM.Room;
        Color color = player.ShortCutColor();
        ElectricExplosion.MakeSparkExplosion(room, size, position, sparks, player.bodyMode == Player.BodyModeIndex.Swimming, color);
        room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, position, 0.5f + Math.Min(1f, volume), UnityEngine.Random.Range(1.1f, 1.5f));
        room.PlaySound(SoundID.Bomb_Explode, position, volume / 2f, UnityEngine.Random.Range(1.75f, 2.25f));
        BTWPlugin.Log("Fake player [" + SCM.Player.ToString() + "] did a spark explosion !");
    }
    [RPCMethod]
    public static void Spark_ElectricExplosionSync(RPCEvent rpc, RoomSession roomSession, Vector2 pos, 
        byte lifeTime, byte rad, byte backgroundNoiseCent)
    {
        if (roomSession == null) { return; }
        AbstractRoom abstractRoom = roomSession.absroom;
        if (abstractRoom == null || abstractRoom.realizedRoom == null) { return; }

        Room room = abstractRoom.realizedRoom;

        ElectricExplosion electricExplosion = new (
            room, null, pos, lifeTime, rad, 0, 0, 0, null, 0f, backgroundNoiseCent / 100f
        );
        room.AddObject(electricExplosion);

        BTWPlugin.Log("Created Fake Electric Explosion !");
    }
    [RPCMethod] // Violence is not synced now ??? Fine, I'll do it myself.
    public static void Spark_ElectricExplosionHit(RPCEvent rpc, 
        OnlineCreature targetOc, byte chuckIndex, OnlinePhysicalObject sourceOpo,
        OnlineCreature killTagHolderOc, byte killTagHolderDmgFactorCent, ushort damageCent, ushort stun,
        Color color, bool doSpams)
    {
        Creature target = targetOc?.abstractCreature?.realizedCreature;
        if (target == null) { return; }

        BodyChunk closestBodyChunk = null;
        if (target.bodyChunks.Length > chuckIndex) { closestBodyChunk = target.bodyChunks[chuckIndex]; }

        PhysicalObject sourceObject = sourceOpo?.apo?.realizedObject;
        Creature killTagHolder = killTagHolderOc?.abstractCreature?.realizedCreature;

        ElectricExplosion.ShockCreature(
            target, closestBodyChunk, sourceObject, killTagHolder, 
            killTagHolderDmgFactorCent / 100f, damageCent / 100f, stun,
            color, doSpams, false, true, new());
        
        BTWPlugin.Log($"Creature [{target}] got hit by an electric explosion of damage <{damageCent / 100f}> and stun <{stun}> !");
    }
    [RPCMethod]
    public static void Core_Boost(RPCEvent rpc, OnlinePhysicalObject playerOpo, byte pow)
    {
        var core = MeadowFunc.GetOnlinePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        core.Boost(pow, false);
        BTWPlugin.Log("Fake player [" + core.player.ToString() + "] did a leap ! (and maybe a flip)");
    }
    [RPCMethod]
    public static void Core_Shockwave(RPCEvent rpc, OnlinePhysicalObject playerOpo)
    {
        var core = MeadowFunc.GetOnlinePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        core.ShockWave(false);
        BTWPlugin.Log("Fake player [" + core.player.ToString() + "] did a shockwave !");
    }
    [RPCMethod]
    public static void Core_Explode(RPCEvent rpc, OnlinePhysicalObject playerOpo)
    {
        var core = MeadowFunc.GetOnlinePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        core.Explode(false);
        BTWPlugin.Log("Fake player [" + core.player.ToString() + "] did an explosion ! (ouch)");
    }
    [RPCMethod]
    public static void Core_Pop(RPCEvent rpc, OnlinePhysicalObject playerOpo)
    {
        var core = MeadowFunc.GetOnlinePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        core.Pop(false);
        BTWPlugin.Log("Fake player [" + core.player.ToString() + "] did a pop !");
    }
    [RPCMethod]
    public static void Core_Disable(RPCEvent rpc, OnlinePhysicalObject playerOpo)
    {
        var core = MeadowFunc.GetOnlinePlayerEnergyCore(playerOpo);
        if (core == null) { return; }
        if (!core.AEC.active || core.AEC.isMeadowFakePlayer) { return; }

        core.Disable();
        BTWPlugin.Log("Fake player [" + core.player.ToString() + "] got disabled !");
    }
    [RPCMethod]
    public static void Core_GaveOxygenToOthers(RPCEvent rpc, OnlinePhysicalObject playerOpo, OnlinePhysicalObject otherplayerOpo)
    {
        var core = MeadowFunc.GetOnlinePlayerEnergyCore(playerOpo);
        Player otherPlayer = MeadowFunc.GetPlayerFromOE(otherplayerOpo);
        if (core == null || otherPlayer == null) { return; }
        if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

        otherPlayer.airInLungs = Mathf.Max(0.85f, otherPlayer.airInLungs);
        BTWPlugin.Log("Fake player [" + core.player.ToString() + "] gave oxygen to " + otherPlayer.ToString() + " !");
    }
    [RPCMethod]
    public static void BTWFunc_CustomKnockBack(RPCEvent rpc, OnlinePhysicalObject objectOpo, short chunkAffected, Vector2 force)
    {
        AbstractPhysicalObject abstractPhysicalObject = objectOpo.apo;
        if (abstractPhysicalObject == null 
            || abstractPhysicalObject.realizedObject == null
            || !abstractPhysicalObject.IsLocal()) { return; }
        
        PhysicalObject physicalObject = abstractPhysicalObject.realizedObject;

        if (chunkAffected < 0 || chunkAffected > physicalObject.bodyChunks.Length)
        {
            BTWFunc.CustomKnockback(physicalObject, force);
            BTWPlugin.Log("Object "+ physicalObject.ToString() +" was pushed with a force of "+ force.ToString() +" !");
        }
        else
        {
            BTWFunc.CustomKnockback(physicalObject.bodyChunks[chunkAffected], force);
            BTWPlugin.Log("Chuck "+ chunkAffected.ToString() +" of object "+ physicalObject.ToString() +" was pushed with a force of "+ force.ToString() +" !");
        }
    }
    [RPCMethod]
    public static void MSCCompat_Lightning(RPCEvent rpc, OnlineCreature fromOc, OnlineCreature targetOc, byte widthCent, byte intensityCent, byte lifeTime, Color color)
    {
        if (!ModManager.MSC) { return; }

        AbstractCreature abstractFrom = fromOc.abstractCreature;
        AbstractCreature abstractTarget = targetOc.abstractCreature;
        if (abstractFrom == null || abstractTarget == null) { return; }
        
        Creature from = abstractFrom.realizedCreature;
        Creature target = abstractTarget.realizedCreature;
        if (from == null || target == null || from.room == null || target.room == null) { return; }

        LightingArc lightingArc = new (
            from.mainBodyChunk, target.mainBodyChunk,
            widthCent * 100f, intensityCent * 100f, lifeTime, color
        );
        from.room.AddObject(lightingArc);

        BTWPlugin.Log("Added lightning arc from "+ from.ToString() +" to "+ target.ToString() +" !");
    }
    [RPCMethod]
    public static void MSCCompat_LightningPos(RPCEvent rpc, RoomSession roomSession, Vector2 from, Vector2 target, byte widthCent, byte intensityCent, byte lifeTime, Color color)
    {
        if (!ModManager.MSC) { return; }
        if (roomSession == null) { return; }
        AbstractRoom abstractRoom = roomSession.absroom;
        if (abstractRoom == null || abstractRoom.realizedRoom == null) { return; }

        Room room = abstractRoom.realizedRoom;

        LightingArc lightingArc = new (
            from, target,
            widthCent * 100f, intensityCent * 100f, lifeTime, color
        );
        room.AddObject(lightingArc);

        BTWPlugin.Log("Added lightning arc from "+ from.ToString() +" to "+ target.ToString() +" !");
    }
    [RPCMethod]
    public static void BTWArenaAddition_AreneForcedDeathEffect(RPCEvent rpc, OnlineCreature targetOc)
    {
        AbstractCreature abstractTarget = targetOc.abstractCreature;
        if (targetOc.isMine || abstractTarget == null) { return; }
        
        Creature target = abstractTarget.realizedCreature;
        if (target == null || target.room == null) { return; }

        target.room.AddObject( new ArenaForcedDeath(target.abstractCreature, true) );

        BTWPlugin.Log("Added Arena Forced Death Effect to "+ target +" !");
    }
    [RPCMethod]
    public static void BTWArenaAddition_AddArenaShield(RPCEvent rpc, OnlineCreature targetOc, byte shieldTimeSeconds)
    {
        AbstractCreature abstractTarget = targetOc.abstractCreature;
        if (targetOc.isMine || abstractTarget == null) { return; }

        if (abstractTarget.realizedCreature is Player target && target.room != null)
        {
            target.room.AddObject( new ArenaShield(target, shieldTimeSeconds * BTWFunc.FrameRate) );
            BTWPlugin.Log("Added Arena Forcefield to "+ target +" !");
        }
        else
        {
            if (ArenaShield.shieldToAdd.TryGetValue(abstractTarget, out _))
            {
                ArenaShield.shieldToAdd.Remove(abstractTarget);
            }
            ArenaShield.shieldToAdd.Add(abstractTarget, new ArenaShield(shieldTimeSeconds * BTWFunc.FrameRate));
            BTWPlugin.Log($"Arena shield spared for when [{abstractTarget}] realizes !");
        }
    }
    [RPCMethod]
    public static void BTWArenaAddition_BlockArenaShield(RPCEvent rpc, OnlineCreature targetOc)
    {
        AbstractCreature abstractTarget = targetOc.abstractCreature;
        if (targetOc.isMine || abstractTarget == null) { return; }

        if (abstractTarget.realizedCreature is not Player target 
            || target.room == null 
            || !ArenaShield.TryGetShield(target, out var shield)) { return; }

        shield.Block(false);

        BTWPlugin.Log("Arena Forcefield Block sync from "+ target +" !");
    }
    [RPCMethod]
    public static void BTWArenaAddition_DismissArenaShield(RPCEvent rpc, OnlineCreature targetOc)
    {
        AbstractCreature abstractTarget = targetOc.abstractCreature;
        // BTWPlugin.Log($"Dismiss is bugged ? From : [{rpc.from}], onlineTarget : [{targetOc}], IsMine [{targetOc.isMine}], abstractTarget : [{abstractTarget}], target : [{abstractTarget?.realizedCreature}], isPlayer : [{abstractTarget?.realizedCreature is Player}], room : [{abstractTarget?.realizedCreature?.room}], hasShield : [{(abstractTarget?.realizedCreature is Player ? ArenaShield.TryGetShield(abstractTarget.realizedCreature as Player, out _) : false)}]");;
        if (targetOc.isMine || abstractTarget == null) { return; }

        if (abstractTarget.realizedCreature is not Player target || target.room == null) { return; }
        
        if (!ArenaShield.TryGetShield(target, out var shield)) { return; }
        
        shield.Dismiss(false);

        BTWPlugin.Log("Arena Forcefield Dismiss sync from "+ target +" !");
    }
    [RPCMethod]
    public static void BTWArenaAddition_DestroyArenaLifes(RPCEvent rpc, OnlineCreature targetOc)
    {
        AbstractCreature abstractTarget = targetOc.abstractCreature;
        if (targetOc.isMine || abstractTarget == null) { return; }

        if (ArenaLives.TryGetLives(abstractTarget, out var lives) && lives.fake)
        {
            lives.Destroy();
        }

        BTWPlugin.Log("Arena Lifes detroyed for "+ abstractTarget +" !");
    }
    [RPCMethod]
    public static void BTWArenaAddition_AddArenaLifes(RPCEvent rpc, OnlineCreature targetOc)
    {
        if (targetOc == null) { return; }
        AbstractCreature abstractTarget = targetOc.abstractCreature;
        if (targetOc.isMine || abstractTarget == null) { return; }
        
        Creature target = abstractTarget.realizedCreature;
        if (target == null || target.room == null) { return; }

        target.room.AddObject( new ArenaLives(target.abstractCreature, true) );

        BTWPlugin.Log("(fake) Arena Lifes added for "+ abstractTarget +" !");
    }
    [RPCMethod]
    public static void BTWArenaAddition_AddItemSpawn(RPCEvent rpc, RoomSession roomSession, Vector2 position, 
        ushort spawnTime, ushort spawnCount, OnlineObjectDataList onlineObjectList)
    {
        if (roomSession == null) { return; }
        AbstractRoom abstractRoom = roomSession.absroom;
        if (abstractRoom == null || abstractRoom.realizedRoom == null) { return; }
        Room room = abstractRoom.realizedRoom;

        ArenaItemSpawn arenaItemSpawn = new(position, spawnTime, onlineObjectList.objectList.ToList(), false, true)
        {
            spawnCount = spawnCount
        };
        room.AddObject( arenaItemSpawn );

        BTWPlugin.Log($"(fake) Arena ItemSpawn added in [{room}]!");
    }
    [RPCMethod]
    public static void BTWArenaAddition_RequestAllItemSpawn(RPCEvent rpc, RoomSession roomSession)
    {
        if (!MeadowFunc.IsMeadowArena()) { return; }
        if (!MeadowFunc.IsMeadowHost()) { return; }
        if (roomSession == null) { return; }
        AbstractRoom abstractRoom = roomSession.absroom;
        if (abstractRoom == null || abstractRoom.realizedRoom == null) { return; }
        Room room = abstractRoom.realizedRoom;
        
        foreach (ArenaItemSpawn itemSpawner in room.updateList.FindAll(x => x is ArenaItemSpawn).Cast<ArenaItemSpawn>())
        {
            if (!(itemSpawner.spawnTime <= itemSpawner.spawnCount))
            {
                MeadowCalls.BTWArena_RPCAddItemSpawnerToRequested(rpc.from, itemSpawner);
            }
        }

        BTWPlugin.Log($"Send all itemSpawner as requested !");
    }

    public static bool CheckIfRPCTypesMatch(Delegate del, params object[] args)
    {
        Type[] types = del.Method.GetParameters().Select(p => p.ParameterType).ToArray();
        object[] obj = args.ToArray();
        bool match = true;
        for (int i = 1; i < types.Length; i++)
        {
            if (obj.Length < i)
            {
                BTWPlugin.logger.LogError($"ARGUMENT MISSING ON RPC [{del.Method.Name}] ! Expecting [{types[i]}], got <{obj.Length}/{types.Length - 1}> arguments.");
                match = false;
            }
            else if (obj[i - 1] != null && (types[i].IsEquivalentTo(obj[i - 1].GetType()) || types[i].IsInstanceOfType(obj[i - 1]) || types[i].IsAssignableFrom(obj[i - 1].GetType())))
            {
                BTWPlugin.logger.LogError($"TYPE MISMATCH ON RPC [{del.Method.Name}] ! Type [{types[i]}] is not [{obj[i - 1]}] type [{obj[i - 1].GetType()}]");
                match = false;
            }
        }
        return match;
    }
    public static void InvokeAllOtherPlayerWithRPC(Delegate del, params object[] args)
    {
        // if (!CheckIfRPCTypesMatch(del, args)) { return; }
        foreach (var player in OnlineManager.players)
        {
            if (!player.isMe)
            {
                player.InvokeRPC(del, args);
            }
        }
    }
    public static void InvokeAllOtherPlayerWithRPCOnce(Delegate del, params object[] args)
    {
        // if (!CheckIfRPCTypesMatch(del, args)) { return; }
        foreach (var player in OnlineManager.players)
        {
            if (!player.isMe)
            {
                player.InvokeOnceRPC(del, args);
            }
        }
    }
}