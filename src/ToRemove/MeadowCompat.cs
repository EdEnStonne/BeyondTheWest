// using System.Collections.Generic;
// using UnityEngine;
// using System;
// using RainMeadow;
// using JetBrains.Annotations;
// using BeyondTheWest;
// using MonoMod.RuntimeDetour;
// using System.Runtime.CompilerServices;
// using MonoMod.Cil;
// using Mono.Cecil.Cil;
// using System.Linq;
// using Menu;
// using ArenaBehaviors;
// using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;

// public class MeadowCompat
// {
//     //---------- Objects
//     public class OnlineStaticChargeManagerData : OnlineEntity.EntityData
//     {
//         [UsedImplicitly]
//         public OnlineStaticChargeManagerData() { }

//         public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
//         {
//             return new State(entity);
//         }

//         //-------- State

//         public class State : EntityDataState
//         {
//             //--------- Variables
//             [OnlineFieldHalf]
//             public float charge;

//             //--------- ctor

//             [UsedImplicitly]
//             public State() { }
//             public State(OnlineEntity onlineEntity)
//             {
//                 if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
//                 {
//                     return;
//                 }

//                 if (!SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM))
//                 {
//                     return;
//                 }

//                 charge = SCM.Charge;
//             }
//             //--------- Functions
//             public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
//             {
//                 var SCM = GetFakePlayerStaticChargeManager(onlineEntity);
//                 if (SCM == null|| !SCM.isMeadowFakePlayer || SCM.active) { return; }

//                 SCM.Charge = this.charge;
//             }
//             public override Type GetDataType()
//             {
//                 return typeof(OnlineStaticChargeManagerData);
//             }

//         }
//     }
//     public class OnlineAbstractCoreData : OnlineEntity.EntityData
//     {
//         [UsedImplicitly]
//         public OnlineAbstractCoreData() { }

//         public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
//         {
//             return new State(entity);
//         }

//         //-------- State

//         public class State : EntityDataState
//         {
//             //--------- Variables
//             [OnlineFieldHalf]
//             public float energy;
//             [OnlineFieldHalf]
//             public float grayScale;
//             [OnlineField]
//             public int boostingCount;
//             [OnlineField]
//             public int antiGravityCount;
//             [OnlineField]
//             public byte state;

//             //--------- ctor

//             [UsedImplicitly]
//             public State() { }
//             public State(OnlineEntity onlineEntity)
//             {
//                 if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Player player)
//                 {
//                     return;
//                 }

//                 if (!CoreFunc.cwtCore.TryGetValue(player.abstractCreature, out var AEC))
//                 {
//                     return;
//                 }

//                 this.energy = AEC.energy;
//                 this.boostingCount = AEC.boostingCount;
//                 this.antiGravityCount = AEC.antiGravityCount;
//                 this.state = AEC.state;
//                 this.grayScale = 0f;
//                 if (AEC.RealizedCore != null)
//                 {
//                     this.grayScale = AEC.RealizedCore.grayScale;
//                 }
//             }
//             //--------- Functions
//             public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
//             {
//                 var AEC = GetFakePlayerAbstractEnergyCore(onlineEntity);
//                 if (AEC == null || !AEC.isMeadowFakePlayer || AEC.active) { return; }

//                 AEC.energy = this.energy;
//                 AEC.boostingCount = this.boostingCount;
//                 AEC.antiGravityCount = this.antiGravityCount;
//                 AEC.state = this.state;
//                 if (AEC.RealizedCore != null)
//                 {
//                     AEC.RealizedCore.grayScale = this.grayScale;
//                 }
//             }
//             public override Type GetDataType()
//             {
//                 return typeof(OnlineAbstractCoreData);
//             }

//         }
//     }
//     public class OnlineArenaLivesData : OnlineEntity.EntityData
//     {
//         [UsedImplicitly]
//         public OnlineArenaLivesData() { }

//         public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
//         {
//             return new State(entity);
//         }

//         //-------- State

//         public class State : EntityDataState
//         {
//             //--------- Variables
//             [OnlineField]
//             public int lifesleft = 1;
//             [OnlineField]
//             public bool countedAlive = true;
//             [OnlineField]
//             public int reviveCounter = 0;
//             [OnlineField]
//             public int livesDisplayCounter = 0;
//             [OnlineField]
//             public int circlesAmount = 0;
//             [OnlineFieldHalf]
//             public float firstPosX;
//             [OnlineFieldHalf]
//             public float firstPosY;

//             //--------- ctor

//             [UsedImplicitly]
//             public State() { }
//             public State(OnlineEntity onlineEntity)
//             {
//                 if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Creature creature)
//                 {
//                     return;
//                 }

//                 if (!CompetitiveAddition.arenaLivesList.TryGetValue(creature.abstractCreature, out var lives) || lives.fake)
//                 {
//                     return;
//                 }
                
//                 this.lifesleft = lives.lifesleft;
//                 this.countedAlive = lives.countedAlive;
//                 this.reviveCounter = lives.reviveCounter;
//                 this.livesDisplayCounter = lives.livesDisplayCounter;
//                 this.circlesAmount = lives.circlesAmount;
//                 this.firstPosX = lives.firstPos.x;
//                 this.firstPosY = lives.firstPos.y;
//             }
//             //--------- Functions
//             public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
//             {
//                 if ((onlineEntity as OnlinePhysicalObject)?.apo.realizedObject is not Creature creature)
//                 {
//                     return;
//                 }

//                 if (!CompetitiveAddition.arenaLivesList.TryGetValue(creature.abstractCreature, out var lives) || !lives.fake)
//                 {
//                     return;
//                 }
//                 if (lives.lifesleft != this.lifesleft || lives.countedAlive != this.countedAlive)
//                 {
//                     lives.karmaSymbolNeedToChange = true;
//                 }
//                 if (lives.countedAlive == false 
//                     && this.countedAlive == true 
//                     && creature is Player player && player != null)
//                 {
//                     ResetDeathMessage(creature.abstractCreature);
//                     ResetIconOnRevival(creature.abstractCreature);
//                 }
//                 if (lives.wasAbstractCreatureDestroyed && this.lifesleft <= 0 && lives.lifesleft > 0)
//                 {
//                     lives.abstractTarget?.Destroy();
//                     lives.Dismiss();
//                 }
//                 else
//                 {
//                     lives.lifesleft = this.lifesleft;
//                     lives.reviveCounter = this.reviveCounter;
//                 }
//                 lives.countedAlive = this.countedAlive;
//                 lives.livesDisplayCounter = this.livesDisplayCounter;
//                 lives.circlesAmount = this.circlesAmount;
//                 lives.firstPos = new Vector2(this.firstPosX, this.firstPosY);

//             }
//             public override Type GetDataType()
//             {
//                 return typeof(OnlineArenaLivesData);
//             }

//         }
//     }

//     public struct CustomDeathMessage
//     {
//         public CustomDeathMessage() { } 
//         public CustomDeathMessage(int contextNum, string deathMessagePre, string deathMessagePost)
//         {
//             this.contextNum = Mathf.Max(contextNum, 10);
//             this.deathMessagePre = deathMessagePre;
//             this.deathMessagePost = deathMessagePost;
//         }        
//         public int contextNum;
//         public string deathMessagePre = "was slain by";
//         public string deathMessagePost = ".";
//     }
//     public static List<CustomDeathMessage> customDeathMessagesEnum = new();
//     public class ArenaCreatureDeathTracker
//     {
//         public ArenaCreatureDeathTracker(Creature creature)
//         {
//             this.creature = creature;
//         }

//         public Creature creature;
//         public int deathMessageCustom = 0;
//     }
//     public static ConditionalWeakTable<AbstractCreature, ArenaCreatureDeathTracker> arenaCreatureDeathTrackers = new();

//     //---------- Functions

//     // Meadow Check
//     public static bool IsMeadowLobby()
//     {
//         // Plugin.Log("Checking if in Meadow lobby");
//         return OnlineManager.lobby is not null;
//     }
//     public static bool IsMeadowArena()
//     {
//         return IsMeadowArena(out _);
//     }
//     public static bool IsMeadowArena(out ArenaOnlineGameMode arenaOnlineGameMode)
//     {
//         arenaOnlineGameMode = null;
//         return IsMeadowLobby() && RainMeadow.RainMeadow.isArenaMode(out arenaOnlineGameMode);
//     }

//     // Fake Player Check
//     public static Player GetPlayerFromOE(OnlineEntity playerOE)
//     {
//         var playerOpo = playerOE as OnlinePhysicalObject;
//         // Plugin.Log(playerOpo);

//         if (playerOpo?.apo?.realizedObject is not Player player)
//         {
//             Plugin.logger.LogError(playerOpo.apo.ToString() + " is not a player !!!");
//             return null;
//         }
//         return player;
//     }
//     public static SparkObject.StaticChargeManager GetFakePlayerStaticChargeManager(OnlineEntity playerOE)
//     {
//         Player player = GetPlayerFromOE(playerOE);
//         if (player == null) { Plugin.logger.LogError(playerOE.ToString() + " player is null !!!"); return null; }

        
//         if (!(SparkFunc.cwtSpark.TryGetValue(player.abstractCreature, out var SCM) && SCM.init))
//         {
//             Plugin.logger.LogError("No StaticChargeManager detected on " + player.ToString());
//             return null;
//         }

//         return SCM;
//     }
//     public static CoreObject.AbstractEnergyCore GetFakePlayerAbstractEnergyCore(OnlineEntity playerOE)
//     {
//         Player player = GetPlayerFromOE(playerOE);
//         if (player == null) { Plugin.logger.LogError(playerOE.ToString() + " player is null !!!"); return null; }

//         if (!CoreFunc.cwtCore.TryGetValue(player.abstractCreature, out var AEC))
//         {
//             Plugin.logger.LogError("No AbstractEnergyCore detected on " + player.ToString());
            
//             return null;
//         }

//         return AEC;
//     }
//     public static CoreObject.EnergyCore GetFakePlayerEnergyCore(OnlineEntity playerOE)
//     {
//         CoreObject.AbstractEnergyCore AEC = GetFakePlayerAbstractEnergyCore(playerOE);
//         if (AEC == null) { return null; }

//         if (AEC.realizedObject == null || AEC.realizedObject is not CoreObject.EnergyCore core)
//         {
//             Plugin.logger.LogError("No EnergyCore detected on " + playerOE.ToString());
//             return null;
//         }
//         return core;
//     }

//     // Creature Check
//     public static bool IsCreatureMine(AbstractCreature abstractCreature)
//     {
//         return IsCreatureMine(abstractCreature, out _);
//     }
//     public static bool IsCreatureMine(AbstractCreature abstractCreature, out OnlineCreature onlineCreature)
//     {
//         onlineCreature = null;
//         if (!IsMeadowLobby())
//         {
//             return true;
//         }
//         onlineCreature = abstractCreature.GetOnlineCreature();
//         return onlineCreature == null || onlineCreature.isMine;
//     }
//     public static bool IsMine(AbstractPhysicalObject abstractPhysicalObject) // From PearlCat, works better than mine
//     {
//         return !IsMeadowLobby() || abstractPhysicalObject.IsLocal();
//     }
//     public static bool IsCreatureFriendlies(Creature creature, Creature friend)
//     {
//         return creature.FriendlyFireSafetyCandidate(friend);
//         // return GameplayExtensions.FriendlyFireSafetyCandidate(creature, friend);
//     }

//     // Arena
//     public static bool ShouldHoldFireFromOnlineArenaTimer()
//     {
//         if (IsMeadowArena(out ArenaOnlineGameMode arenaOnlineGameMode))
//         {
//             return arenaOnlineGameMode.externalArenaGameMode.HoldFireWhileTimerIsActive(arenaOnlineGameMode);
//         }
//         return false;
//     }

//     // RPCs
//     [RPCMethod]
//     public static void Spark_SparkExplosion(RPCEvent _, OnlinePhysicalObject playerOpo, short size, Vector2 position, byte sparks, byte volumeCent)
//     {
//         // Plugin.Log("Opening the RPC : " + playerOpo.ToString() + "/" + size.ToString() + "/" + position.ToString() + "/" + sparks.ToString() + "/" + volumeCent.ToString());
//         var SCM = GetFakePlayerStaticChargeManager(playerOpo);
//         if (SCM == null) { return; }
//         if (SCM.active || !SCM.isMeadowFakePlayer) { return; }
//         // Plugin.Log("Checking some stuff :" + SCM.ToString() + "/" + SCM.active + "/" + SCM.isMeadowFakePlayer);

//         float volume = volumeCent;
//         volume /= 100f;
//         Player player = SCM.Player;
//         Room room = SCM.Room;
//         Color color = player.ShortCutColor();
//         SparkObject.MakeSparkExplosion(room, size, position, sparks, player.bodyMode == Player.BodyModeIndex.Swimming, color);
//         room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, position, 0.5f + Math.Min(1f, volume), UnityEngine.Random.Range(1.1f, 1.5f));
//         room.PlaySound(SoundID.Bomb_Explode, position, volume / 2f, UnityEngine.Random.Range(1.75f, 2.25f));
//         Plugin.Log("Fake player [" + SCM.Player.ToString() + "] did a spark explosion !");
//     }
//     [RPCMethod]
//     public static void Spark_ElectricExplosionSyncRPCEvent(RPCEvent _, RoomSession roomSession, Vector2 pos, byte lifeTime, byte rad, byte backgroundNoiseCent)
//     {
//         if (roomSession == null) { return; }
//         AbstractRoom abstractRoom = roomSession.absroom;
//         if (abstractRoom == null || abstractRoom.realizedRoom == null) { return; }

//         Room room = abstractRoom.realizedRoom;

//         SparkObject.ElectricExplosion electricExplosion = new SparkObject.ElectricExplosion(
//             room, null, pos, lifeTime, rad, 0, 0, 0, null, 0f, backgroundNoiseCent / 100f
//         );
//         room.AddObject(electricExplosion);

//         Plugin.Log("Created Fake Electric Explosion !");
//     }
//     [RPCMethod] // Violence is not synced now ??? Fine, I'll do it myself.
//     public static void Spark_ElectricExplosionHitRPCEvent(RPCEvent _, 
//         OnlineCreature targetOc, byte chuckIndex, OnlinePhysicalObject sourceOpo,
//         OnlineCreature killTagHolderOc, byte killTagHolderDmgFactorCent, ushort damageCent, ushort stun,
//         Color color, bool doSpams)
//     {
//         Creature target = targetOc?.abstractCreature?.realizedCreature;
//         if (target == null) { return; }

//         BodyChunk closestBodyChunk = null;
//         if (target.bodyChunks.Length > chuckIndex) { closestBodyChunk = target.bodyChunks[chuckIndex]; }

//         PhysicalObject sourceObject = sourceOpo?.apo?.realizedObject;
//         Creature killTagHolder = killTagHolderOc?.abstractCreature?.realizedCreature;

//         SparkObject.ShockCreature(
//             target, closestBodyChunk, sourceObject, killTagHolder, 
//             killTagHolderDmgFactorCent / 100f, damageCent / 100f, stun,
//             color, doSpams, false, true, new());
        
//         Plugin.Log($"Creature [{target}] got hit by an electric explosion of damage <{damageCent / 100f}> and stun <{stun}> !");
//     }
//     [RPCMethod]
//     public static void Core_Boost(RPCEvent _, OnlinePhysicalObject playerOpo, byte pow)
//     {
//         var core = GetFakePlayerEnergyCore(playerOpo);
//         if (core == null) { return; }
//         if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

//         core.Boost(pow, false);
//         Plugin.Log("Fake player [" + core.player.ToString() + "] did a leap ! (and maybe a flip)");
//     }
//     [RPCMethod]
//     public static void Core_Shockwave(RPCEvent _, OnlinePhysicalObject playerOpo)
//     {
//         var core = GetFakePlayerEnergyCore(playerOpo);
//         if (core == null) { return; }
//         if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

//         core.ShockWave(false);
//         Plugin.Log("Fake player [" + core.player.ToString() + "] did a shockwave !");
//     }
//     [RPCMethod]
//     public static void Core_Explode(RPCEvent _, OnlinePhysicalObject playerOpo)
//     {
//         var core = GetFakePlayerEnergyCore(playerOpo);
//         if (core == null) { return; }
//         if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

//         core.Explode(false);
//         Plugin.Log("Fake player [" + core.player.ToString() + "] did an explosion ! (ouch)");
//     }
//     [RPCMethod]
//     public static void Core_Pop(RPCEvent _, OnlinePhysicalObject playerOpo)
//     {
//         var core = GetFakePlayerEnergyCore(playerOpo);
//         if (core == null) { return; }
//         if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

//         core.Pop(false);
//         Plugin.Log("Fake player [" + core.player.ToString() + "] did a pop !");
//     }
//     [RPCMethod]
//     public static void Core_Disable(RPCEvent _, OnlinePhysicalObject playerOpo)
//     {
//         var core = GetFakePlayerEnergyCore(playerOpo);
//         if (core == null) { return; }
//         if (!core.AEC.active || core.AEC.isMeadowFakePlayer) { return; }

//         core.Disable();
//         Plugin.Log("Fake player [" + core.player.ToString() + "] got disabled !");
//     }
//     [RPCMethod]
//     public static void Core_GaveOxygenToOthers(RPCEvent _, OnlinePhysicalObject playerOpo, OnlinePhysicalObject otherplayerOpo)
//     {
//         var core = GetFakePlayerEnergyCore(playerOpo);
//         Player otherPlayer = GetPlayerFromOE(otherplayerOpo);
//         if (core == null || otherPlayer == null) { return; }
//         if (core.AEC.active || !core.AEC.isMeadowFakePlayer) { return; }

//         otherPlayer.airInLungs = Mathf.Max(0.85f, otherPlayer.airInLungs);
//         Plugin.Log("Fake player [" + core.player.ToString() + "] gave oxygen to " + otherPlayer.ToString() + " !");
//     }
//     [RPCMethod]
//     public static void BTWFunc_CustomKnockBackRPCEvent(RPCEvent _, OnlinePhysicalObject objectOpo, short chunkAffected, Vector2 force)
//     {
//         AbstractPhysicalObject abstractPhysicalObject = objectOpo.apo;
//         if (abstractPhysicalObject == null 
//             || abstractPhysicalObject.realizedObject == null
//             || !IsMine(abstractPhysicalObject)) { return; }
        
//         PhysicalObject physicalObject = abstractPhysicalObject.realizedObject;

//         if (chunkAffected < 0 || chunkAffected > physicalObject.bodyChunks.Length)
//         {
//             BTWFunc.CustomKnockback(physicalObject, force);
//             Plugin.Log("Object "+ physicalObject.ToString() +" was pushed with a force of "+ force.ToString() +" !");
//         }
//         else
//         {
//             BTWFunc.CustomKnockback(physicalObject.bodyChunks[chunkAffected], force);
//             Plugin.Log("Chuck "+ chunkAffected.ToString() +" of object "+ physicalObject.ToString() +" was pushed with a force of "+ force.ToString() +" !");
//         }
//     }
//     [RPCMethod]
//     public static void MSCCompat_LightningRPCEvent(RPCEvent _, OnlineCreature fromOc, OnlineCreature targetOc, byte widthCent, byte intensityCent, byte lifeTime, Color color)
//     {
//         if (!ModManager.MSC) { return; }

//         AbstractCreature abstractFrom = fromOc.abstractCreature;
//         AbstractCreature abstractTarget = targetOc.abstractCreature;
//         if (abstractFrom == null || abstractTarget == null) { return; }
        
//         Creature from = abstractFrom.realizedCreature;
//         Creature target = abstractTarget.realizedCreature;
//         if (from == null || target == null || from.room == null || target.room == null) { return; }

//         MoreSlugcatCompat.LightingArc lightingArc = new MoreSlugcatCompat.LightingArc(
//             from.mainBodyChunk, target.mainBodyChunk,
//             widthCent * 100f, intensityCent * 100f, lifeTime, color
//         );
//         from.room.AddObject(lightingArc);

//         Plugin.Log("Added lightning arc from "+ from.ToString() +" to "+ target.ToString() +" !");
//     }
//     [RPCMethod]
//     public static void MSCCompat_LightningPosRPCEvent(RPCEvent _, RoomSession roomSession, Vector2 from, Vector2 target, byte widthCent, byte intensityCent, byte lifeTime, Color color)
//     {
//         if (!ModManager.MSC) { return; }
//         if (roomSession == null) { return; }
//         AbstractRoom abstractRoom = roomSession.absroom;
//         if (abstractRoom == null || abstractRoom.realizedRoom == null) { return; }

//         Room room = abstractRoom.realizedRoom;

//         MoreSlugcatCompat.LightingArc lightingArc = new MoreSlugcatCompat.LightingArc(
//             from, target,
//             widthCent * 100f, intensityCent * 100f, lifeTime, color
//         );
//         room.AddObject(lightingArc);

//         Plugin.Log("Added lightning arc from "+ from.ToString() +" to "+ target.ToString() +" !");
//     }
//     [RPCMethod]
//     public static void BTWArenaAddition_AreneForcedDeathEffectRPCEvent(RPCEvent _, OnlineCreature targetOc)
//     {
//         AbstractCreature abstractTarget = targetOc.abstractCreature;
//         if (targetOc.isMine || abstractTarget == null) { return; }
        
//         Creature target = abstractTarget.realizedCreature;
//         if (target == null || target.room == null) { return; }

//         target.room.AddObject( new CompetitiveAddition.ArenaForcedDeath(target.abstractCreature, true) );

//         Plugin.Log("Added Arena Forced Death Effect to "+ target +" !");
//     }
//     [RPCMethod]
//     public static void BTWArenaAddition_AddArenaShieldRPCEvent(RPCEvent _, OnlineCreature targetOc, byte shieldTimeSeconds)
//     {
//         AbstractCreature abstractTarget = targetOc.abstractCreature;
//         if (targetOc.isMine || abstractTarget == null) { return; }

//         if (abstractTarget.realizedCreature is not Player target || target.room == null) { return; }

//         target.room.AddObject( new CompetitiveAddition.ArenaShield(target, shieldTimeSeconds * BTWFunc.FrameRate) );

//         Plugin.Log("Added Arena Forcefield to "+ target +" !");
//     }
//     [RPCMethod]
//     public static void BTWArenaAddition_BlockArenaShieldRPCEvent(RPCEvent _, OnlineCreature targetOc)
//     {
//         AbstractCreature abstractTarget = targetOc.abstractCreature;
//         if (targetOc.isMine || abstractTarget == null) { return; }

//         if (abstractTarget.realizedCreature is not Player target 
//             || target.room == null 
//             || CompetitiveAddition.arenaShields.TryGetValue(target, out var shield)) { return; }

//         shield.Block(false);

//         Plugin.Log("Arena Forcefield Block sync from "+ target +" !");
//     }
//     [RPCMethod]
//     public static void BTWArenaAddition_DismissArenaShieldRPCEvent(RPCEvent _, OnlineCreature targetOc)
//     {
//         AbstractCreature abstractTarget = targetOc.abstractCreature;
//         if (targetOc.isMine || abstractTarget == null) { return; }

//         if (abstractTarget.realizedCreature is not Player target || target.room == null) { return; }
        
//         if (!CompetitiveAddition.arenaShields.TryGetValue(target, out var shield)) { return; }
        
//         shield.Dismiss(false);

//         Plugin.Log("Arena Forcefield Dismiss sync from "+ target +" !");
//     }
//     [RPCMethod]
//     public static void BTWArenaAddition_DestroyArenaLifesRPCEvent(RPCEvent _, OnlineCreature targetOc)
//     {
//         AbstractCreature abstractTarget = targetOc.abstractCreature;
//         if (targetOc.isMine || abstractTarget == null) { return; }

//         if (CompetitiveAddition.arenaLivesList.TryGetValue(abstractTarget, out var lives) && lives.fake)
//         {
//             lives.Destroy();
//         }

//         Plugin.Log("Arena Lifes detroyed for "+ abstractTarget +" !");
//     }
//     [RPCMethod]
//     public static void BTWArenaAddition_AddArenaLifesRPCEvent(RPCEvent _, OnlineCreature targetOc)
//     {
//         if (targetOc == null) { return; }
//         AbstractCreature abstractTarget = targetOc.abstractCreature;
//         if (targetOc.isMine || abstractTarget == null) { return; }
        
//         Creature target = abstractTarget.realizedCreature;
//         if (target == null || target.room == null) { return; }

//         target.room.AddObject( new CompetitiveAddition.ArenaLives(target.abstractCreature, true) );

//         Plugin.Log("(fake) Arena Lifes added for "+ abstractTarget +" !");
//     }

//     public static bool CheckIfRPCTypesMatch(Delegate del, params object[] args)
//     {
//         Type[] types = del.Method.GetParameters().Select(p => p.ParameterType).ToArray();
//         object[] obj = args.ToArray();
//         bool match = true;
//         for (int i = 1; i < types.Length; i++)
//         {
//             if (obj.Length < i)
//             {
//                 Plugin.logger.LogError($"ARGUMENT MISSING ON RPC [{del.Method.Name}] ! Expecting [{types[i]}], got <{obj.Length}/{types.Length - 1}> arguments.");
//                 match = false;
//             }
//             else if (obj[i - 1] != null && (types[i].IsEquivalentTo(obj[i - 1].GetType()) || types[i].IsInstanceOfType(obj[i - 1]) || types[i].IsAssignableFrom(obj[i - 1].GetType())))
//             {
//                 Plugin.logger.LogError($"TYPE MISMATCH ON RPC [{del.Method.Name}] ! Type [{types[i]}] is not [{obj[i - 1]}] type [{obj[i - 1].GetType()}]");
//                 match = false;
//             }
//         }
//         return match;
//     }
//     public static void InvokeAllOtherPlayerWithRPC(Delegate del, params object[] args)
//     {
//         // if (!CheckIfRPCTypesMatch(del, args)) { return; }
//         foreach (var player in OnlineManager.players)
//         {
//             if (!player.isMe)
//             {
//                 player.InvokeRPC(del, args);
//             }
//         }
//     }
//     public static void InvokeAllOtherPlayerWithRPCOnce(Delegate del, params object[] args)
//     {
//         // if (!CheckIfRPCTypesMatch(del, args)) { return; }
//         foreach (var player in OnlineManager.players)
//         {
//             if (!player.isMe)
//             {
//                 player.InvokeOnceRPC(del, args);
//             }
//         }
//     }

//     // Trailseeker
//     public static void WallClimbMeadow_Init(WallClimbObject.WallClimbManager wallClimbManager)
//     {
//         if (MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var meadowArenaSettings))
//         {
//             wallClimbManager.MaxWallClimb = meadowArenaSettings.Trailseeker_MaxWallClimb;
//             wallClimbManager.MaxWallGripCount = meadowArenaSettings.Trailseeker_WallGripTimer * BTWFunc.FrameRate;
//         }
//     }
//     public static void ModifiedTech_Init(TrailseekerFunc.ModifiedTech modifiedTech)
//     {
//         if (MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var settings))
//         {
//             modifiedTech.techEnabled = settings.Trailseeker_AlteredMovementTech;
//             modifiedTech.poleBonus = settings.Trailseeker_PoleClimbBonus;
//         }
//     }

//     // Core
//     public static OnlineCreature CoreMeadow_OnlineCreature(CoreObject.AbstractEnergyCore abstractEnergyCore)
//     {
//         bool IsMeadowLobby = MeadowCompat.IsMeadowLobby();
//         return IsMeadowLobby ? abstractEnergyCore.abstractPlayer.GetOnlineCreature() : null;
//     }
//     public static void CoreMeadow_Init(CoreObject.AbstractEnergyCore abstractEnergyCore)
//     {
//         OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
//         bool IsMine = MeadowCompat.IsMine(abstractEnergyCore.abstractPlayer);

//         abstractEnergyCore.active = IsMine;
//         abstractEnergyCore.isMeadow = IsMeadowLobby() && onlineCreature != null;
//         abstractEnergyCore.isMeadowFakePlayer = !IsMine && onlineCreature != null;
        
//         if (MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var meadowArenaSettings))
//         {
//             abstractEnergyCore.CoreMaxEnergy = meadowArenaSettings.Core_MaxEnergy;
//             abstractEnergyCore.CoreEnergyRecharge = meadowArenaSettings.Core_RegenEnergy;
//             abstractEnergyCore.CoreOxygenEnergyUsage = meadowArenaSettings.Core_OxygenEnergyUsage;
//             abstractEnergyCore.CoreAntiGravity = meadowArenaSettings.Core_AntiGravityCent / 100f;
//             abstractEnergyCore.CoreMaxBoost = meadowArenaSettings.Core_MaxLeap;
//             abstractEnergyCore.isShockwaveEnabled = meadowArenaSettings.Core_Shockwave;

//             abstractEnergyCore.energy = abstractEnergyCore.CoreMaxEnergy;
//             abstractEnergyCore.coreBoostLeft = abstractEnergyCore.CoreMaxBoost;
//         }
//         if (ShouldHoldFireFromOnlineArenaTimer())
//         {
//             abstractEnergyCore.isMeadowArenaTimerCountdown = true;
//             Plugin.Log(abstractEnergyCore.abstractPlayer +" In Timer !");
//         }
//         if (IsMine && abstractEnergyCore.isMeadow)
//         {
//             onlineCreature.AddData(new OnlineAbstractCoreData());
//         }
//     }
//     public static void CoreMeadow_Update(CoreObject.AbstractEnergyCore abstractEnergyCore)
//     {
//         OnlinePhysicalObject onlinePhysicalObject = abstractEnergyCore.GetOnlineObject();
//         if (onlinePhysicalObject != null) { onlinePhysicalObject.Deregister(); }

//         // if (abstractEnergyCore.realizedObject != null && !onlinePhysicalObject.realized)
//         // {
//         //     onlinePhysicalObject.realized = true;
//         // }
//         // else if (abstractEnergyCore.realizedObject == null && onlinePhysicalObject.realized)
//         // {
//         //     onlinePhysicalObject.realized = false;
//         // }
//     }
//     public static void CoreMeadow_BoostRPC(CoreObject.AbstractEnergyCore abstractEnergyCore, byte pow)
//     {
//         OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
//         if (onlineCreature == null) { return; }

//         InvokeAllOtherPlayerWithRPCOnce(
//             typeof(MeadowCompat).GetMethod(nameof(Core_Boost)).CreateDelegate(
//                 typeof(Action<RPCEvent, OnlinePhysicalObject, byte>)), onlineCreature, 
//                 pow
//         );
//     }
//     public static void CoreMeadow_ShockwaveRPC(CoreObject.AbstractEnergyCore abstractEnergyCore)
//     {
//         OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
//         if (onlineCreature == null) { return; }

//         InvokeAllOtherPlayerWithRPC(
//             typeof(MeadowCompat).GetMethod(nameof(Core_Shockwave)).CreateDelegate(
//                 typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
//         );
//     }
//     public static void CoreMeadow_ExplodeRPC(CoreObject.AbstractEnergyCore abstractEnergyCore)
//     {
//         OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
//         if (onlineCreature == null) { return; }

//         InvokeAllOtherPlayerWithRPC(
//             typeof(MeadowCompat).GetMethod(nameof(Core_Explode)).CreateDelegate(
//                 typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
//         );
//     }
//     public static void CoreMeadow_PopRPC(CoreObject.AbstractEnergyCore abstractEnergyCore)
//     {
//         OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
//         if (onlineCreature == null) { return; }

//         InvokeAllOtherPlayerWithRPCOnce(
//             typeof(MeadowCompat).GetMethod(nameof(Core_Pop)).CreateDelegate(
//                 typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
//         );
//     }
//     public static void CoreMeadow_DisableRPC(CoreObject.AbstractEnergyCore abstractEnergyCore)
//     {
//         OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
//         if (onlineCreature == null) { return; }

//         OnlinePlayer onlinePlayer = onlineCreature.owner;
//         onlinePlayer.InvokeRPC(
//             typeof(MeadowCompat).GetMethod(nameof(Core_Disable)).CreateDelegate(
//                 typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineCreature
//         );
//     }
//     public static void CoreMeadow_OxygenGiveRPC(CoreObject.AbstractEnergyCore abstractEnergyCore, Player target)
//     {
//         OnlineCreature onlineCreature = CoreMeadow_OnlineCreature(abstractEnergyCore);
//         OnlineCreature targetOnlineCreature = target.abstractCreature.GetOnlineCreature();
//         if (onlineCreature == null || targetOnlineCreature == null) { return; }

//         InvokeAllOtherPlayerWithRPCOnce(
//             typeof(MeadowCompat).GetMethod(nameof(Core_GaveOxygenToOthers)).CreateDelegate(
//                 typeof(Action<RPCEvent, OnlinePhysicalObject, OnlinePhysicalObject>)), onlineCreature,
//                 targetOnlineCreature
//         );
//     }

//     // Spark
//     public static OnlineCreature SparkMeadow_OnlineCreature(SparkObject.StaticChargeManager staticChargeManager)
//     {
//         bool IsMeadowLobby = MeadowCompat.IsMeadowLobby();
//         return IsMeadowLobby ? staticChargeManager.AbstractPlayer.GetOnlineCreature() : null;
//     }
//     public static void SparkMeadow_Init(SparkObject.StaticChargeManager staticChargeManager)
//     {
//         bool IsMine = !Plugin.meadowEnabled || MeadowCompat.IsMine(staticChargeManager.AbstractPlayer);
//         bool IsMeadowLobby = Plugin.meadowEnabled && MeadowCompat.IsMeadowLobby();
//         OnlineCreature onlineCreature = SparkMeadow_OnlineCreature(staticChargeManager);

//         staticChargeManager.particles = true;
//         staticChargeManager.active = !staticChargeManager.Player.dead && IsMine;
//         staticChargeManager.isMeadow = IsMeadowLobby && onlineCreature != null;
//         staticChargeManager.isMeadowFakePlayer = !IsMine && onlineCreature != null;

//         if (IsMeadowArena())
//         {
//             staticChargeManager.isMeadowArena = true;
//             if (MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var meadowArenaSettings))
//             {
//                 staticChargeManager.FullECharge = meadowArenaSettings.Spark_MaxCharge;
//                 staticChargeManager.MaxECharge = meadowArenaSettings.Spark_MaxCharge + meadowArenaSettings.Spark_AdditionnalOvercharge;
//                 staticChargeManager.RechargeMult = meadowArenaSettings.Spark_ChargeRegenerationMult;
//                 staticChargeManager.MaxEBounce = meadowArenaSettings.Spark_MaxElectricBounce;
//                 staticChargeManager.DoDischargeDamagePlayers = meadowArenaSettings.Spark_DoDischargeDamage;
//                 staticChargeManager.RiskyOvercharge = meadowArenaSettings.Spark_RiskyOvercharge;
//                 staticChargeManager.DeathOvercharge = meadowArenaSettings.Spark_DeadlyOvercharge;

//                 staticChargeManager.eBounceLeft = staticChargeManager.MaxEBounce;
//             }
//             if (ShouldHoldFireFromOnlineArenaTimer())
//             {
//                 staticChargeManager.dischargeCooldown = 10;
//                 staticChargeManager.isMeadowArenaTimerCountdown = true;
//                 Plugin.Log(staticChargeManager.AbstractPlayer + " In Timer !");
//             }
//         }
//         if (IsMine && staticChargeManager.isMeadow)
//         {
//             onlineCreature.AddData(new OnlineStaticChargeManagerData());
//         }
//     }
//     public static void SparMeadow_ElectricExplosionRPC(SparkObject.ElectricExplosion electricExplosion)
//     {
//         if (electricExplosion == null || electricExplosion.room == null) { return; }

//         RoomSession roomSession = electricExplosion.room.abstractRoom.GetResource();
//         if (roomSession == null) { return; }

//         InvokeAllOtherPlayerWithRPC(
//             typeof(MeadowCompat).GetMethod(nameof(Spark_ElectricExplosionSyncRPCEvent)).CreateDelegate(
//                 typeof(Action<RPCEvent, RoomSession, Vector2, byte, byte, byte>)),
//                 roomSession, electricExplosion.pos, 
//                 (byte)electricExplosion.lifeTime, (byte)electricExplosion.rad, (byte)(electricExplosion.backgroundNoise * 100f)
//         );
//     }
//     public static void SparMeadow_ShockCreatureRPC(
//         Creature target, BodyChunk closestBodyChunk, PhysicalObject sourceObject, 
//         Creature killTagHolder, float killTagHolderDmgFactor, float damage, float stun, 
//         Color color, bool doSpams = false)
//     {
//         if (target == null || target.abstractCreature == null) { return; }
//         OnlineCreature onlineTarget = target.abstractCreature.GetOnlineCreature();
//         if (onlineTarget == null || onlineTarget.owner == null) { return; }

//         byte chuckIndex = 0;
//         if (closestBodyChunk != null) { chuckIndex = (byte)closestBodyChunk.index; }

//         InvokeAllOtherPlayerWithRPC(
//             typeof(MeadowCompat).GetMethod(nameof(Spark_ElectricExplosionHitRPCEvent)).CreateDelegate(
//                 typeof(Action<RPCEvent, OnlineCreature, byte, OnlinePhysicalObject, OnlineCreature, byte, ushort, ushort, Color, bool>)),
//                 onlineTarget, chuckIndex, sourceObject?.abstractPhysicalObject?.GetOnlineObject(), 
//                 killTagHolder?.abstractCreature?.GetOnlineCreature(), (byte)(killTagHolderDmgFactor * 100f),
//                 (ushort)(damage * 100f), (ushort)stun, color, doSpams
//         );
//     }

//     // BTWFunction
//     public static void BTWFuncMeadow_RPCCustomKnockBack(PhysicalObject physicalObject, short chunkAffected, Vector2 force)
//     {
//         OnlinePhysicalObject onlinePhysicalObject = physicalObject.abstractPhysicalObject.GetOnlineObject();
//         if (onlinePhysicalObject == null || force == Vector2.zero || IsMine(physicalObject.abstractPhysicalObject)) { return; }

//         OnlinePlayer onlinePlayer = onlinePhysicalObject.owner;
//         onlinePlayer.InvokeRPC(
//             typeof(MeadowCompat).GetMethod(nameof(BTWFunc_CustomKnockBackRPCEvent)).CreateDelegate(
//                 typeof(Action<RPCEvent, OnlinePhysicalObject, short, Vector2>)), 
//                 onlinePhysicalObject, chunkAffected, force
//         );
//     }

//     // MSCCompat
//     public static void MSCCompat_RPCSyncLightnightArc(UpdatableAndDeletable lightnightArc)
//     {
//         if (lightnightArc == null || !ModManager.MSC) { return; }

//         if (lightnightArc is MoreSlugcatCompat.LightingArc arc)
//         {
//             if (arc.from?.owner == null || arc.target?.owner == null) { 
                
//             }
//             else
//             {
//                 if (arc.from.owner is not Creature from || arc.target.owner  is not Creature target) { return; }

//                 OnlineCreature onlineFrom = from.abstractCreature.GetOnlineCreature();
//                 OnlineCreature onlineTarget = target.abstractCreature.GetOnlineCreature();
//                 if (onlineFrom == null || onlineTarget == null) { return; }

//                 InvokeAllOtherPlayerWithRPCOnce(
//                 typeof(MeadowCompat).GetMethod(nameof(MSCCompat_LightningRPCEvent)).CreateDelegate(
//                     typeof(Action<RPCEvent, OnlineCreature, OnlineCreature, byte, byte, byte, Color>)),
//                     onlineFrom, onlineTarget, (byte)(arc.width / 100f), (byte)(arc.intensity / 100f), (byte)arc.lifeTime, arc.color
//                 );
//             }
            
//         }
//     }

//     // Arena Additions
//     public static void BTWArena_RPCArenaForcedDeathEffect(CompetitiveAddition.ArenaForcedDeath forcedDeath)
//     {
//         if (forcedDeath == null || forcedDeath.abstractTarget == null) { return; }
//         OnlineCreature onlineCreature = forcedDeath.abstractTarget.GetOnlineCreature();

//         if (onlineCreature == null) { return; }
//         InvokeAllOtherPlayerWithRPC(
//         typeof(MeadowCompat).GetMethod(nameof(BTWArenaAddition_AreneForcedDeathEffectRPCEvent)).CreateDelegate(
//             typeof(Action<RPCEvent, OnlineCreature>)),
//             onlineCreature
//         );
//     }
//     public static void BTWArena_RPCArenaForcefieldAdded(CompetitiveAddition.ArenaShield shield)
//     {
//         if (shield == null || shield.target == null || !IsMine(shield.target.abstractCreature)) { return; }
//         OnlineCreature onlineCreature = shield.target.abstractCreature.GetOnlineCreature();

//         if (onlineCreature == null) { return; }
//         InvokeAllOtherPlayerWithRPC(
//         typeof(MeadowCompat).GetMethod(nameof(BTWArenaAddition_AddArenaShieldRPCEvent)).CreateDelegate(
//             typeof(Action<RPCEvent, OnlineCreature, byte>)),
//             onlineCreature, (byte)(shield.shieldTime / BTWFunc.FrameRate)
//         );
//     }
//     public static void BTWArena_RPCArenaForcefieldBlock(CompetitiveAddition.ArenaShield shield)
//     {
//         if (shield == null || shield.target == null) { return; }
//         OnlineCreature onlineCreature = shield.target.abstractCreature.GetOnlineCreature();

//         if (onlineCreature == null) { return; }
//         InvokeAllOtherPlayerWithRPC(
//         typeof(MeadowCompat).GetMethod(nameof(BTWArenaAddition_BlockArenaShieldRPCEvent)).CreateDelegate(
//             typeof(Action<RPCEvent, OnlineCreature>)),
//             onlineCreature
//         );
//     }
//     public static void BTWArena_RPCArenaForcefieldDismiss(CompetitiveAddition.ArenaShield shield)
//     {
//         if (shield == null || shield.target == null || !IsMine(shield.target.abstractCreature)) { return; }
//         OnlineCreature onlineCreature = shield.target.abstractCreature.GetOnlineCreature();

//         if (onlineCreature == null) { return; }
//         InvokeAllOtherPlayerWithRPC(
//         typeof(MeadowCompat).GetMethod(nameof(BTWArenaAddition_DismissArenaShieldRPCEvent)).CreateDelegate(
//             typeof(Action<RPCEvent, OnlineCreature>)),
//             onlineCreature
//         );
//     }
//     public static void SetDeathTrackerOfCreature(Creature target, int EnumValue)
//     {
//         if (arenaCreatureDeathTrackers.TryGetValue(target.abstractCreature, out var deathTracker))
//         {
//             deathTracker.deathMessageCustom = EnumValue < 10 ? 0 : EnumValue;
//         }
//     }
//     public static int GetCustomDeathMessageOfViolence(Creature target, ArenaCreatureDeathTracker deathTracker, PhysicalObject owner, Creature.DamageType type, float damage, float stunBonus)
//     {
//         if (target == null || target.dead || target.room == null || deathTracker == null) { return 0; }

//         if (owner != null)
//         {
//             if (owner is Spear spear && spear != null)
//             {
//                 if (spear.thrownBy != null)
//                 {
//                     if ((target.mainBodyChunk.pos - spear.thrownBy.mainBodyChunk.pos).magnitude > 750
//                         && BTWFunc.random < 0.25f)
//                     {
//                         return 17;
//                     }
//                     else if (BTWFunc.BodyChunkSumberged(spear.thrownBy.mainBodyChunk)
//                         && BTWFunc.BodyChunkSumberged(target.mainBodyChunk)
//                         && BTWFunc.random < 0.25f)
//                     {
//                         return 18;
//                     }
//                 }
//                 if (spear.thrownBy is Player playerkiller
//                     && playerkiller != null)
//                 {
//                     if (playerkiller.animation == Player.AnimationIndex.Flip
//                         && BTWFunc.random < 0.25f)
//                     {
//                         return 16;
//                     }
//                     else if (ModManager.MSC && MoreSlugcatCompat.IsArtificer(playerkiller)
//                         && BTWFunc.BodyChunkSumberged(playerkiller.mainBodyChunk)
//                         && BTWFunc.BodyChunkSumberged(target.mainBodyChunk))
//                     {
//                         return 37;
//                     }
//                     else if (playerkiller.isSlugpup
//                         && BTWFunc.random < 0.2f)
//                     {
//                         return 38;
//                     }
//                 }

//                 if (spear is ExplosiveSpear)
//                 {
//                     return 11;
//                 }
//                 else if (ModManager.MSC && MoreSlugcatCompat.IsElectricSpear(spear))
//                 {
//                     return 12;
//                 }

//                 if (spear.spearmasterNeedle && spear.spearmasterNeedle_hasConnection)
//                 {
//                     if (BTWFunc.random < 0.05f) { return 15; }
//                     return 14;
//                 }
//                 return 10;
//             }
//             else if (owner is Player player && player != null)
//             {
//                 if (type == Creature.DamageType.Bite)
//                 {
//                     if (ModManager.MSC && MoreSlugcatCompat.IsArtificer(player))
//                     {
//                         return 36;
//                     }
//                     return 30;
//                 }
//                 else if (type == Creature.DamageType.Blunt)
//                 {
//                     if (CoreFunc.IsCore(player))
//                     {
//                         return 35;
//                     }
//                     else
//                     {
//                         if (damage == 1f && stunBonus == 120f) { return 34; }
//                     }
//                     return 32;
//                 }
//                 else if (type == Creature.DamageType.Electric && SparkFunc.IsSpark(player))
//                 {
//                     if (BTWFunc.BodyChunkSumberged(player.mainBodyChunk)
//                         && BTWFunc.BodyChunkSumberged(target.mainBodyChunk))
//                     {
//                         return 24;
//                     }
//                     if (damage > 1.5f) { return 23; }
//                     return 22;
//                 }
//                 else if (type == Creature.DamageType.Explosion && CoreFunc.IsCore(player))
//                 {
//                     if (damage > 2f) { return 21; }
//                     return 20;
//                 }
//             }
//             else if (owner is CoreObject.EnergyCore energycore && energycore != null)
//             {
//                 if (damage > 2f) { return 21; }
//                 return 20;
//             }
//             else if (owner is Rock rock && rock != null)
//             {
//                 return 33;
//             }
//         }

//         if (type == Creature.DamageType.Bite)
//         {
//             return 52;
//         }
//         else if (type == Creature.DamageType.Blunt)
//         {
//             return 51;
//         }
//         else if (type == Creature.DamageType.Electric)
//         {
//             return 55;
//         }
//         else if (type == Creature.DamageType.Explosion)
//         {
//             return 54;
//         }
//         else if (type == Creature.DamageType.Stab)
//         {
//             return 50;
//         }
//         else if (type == Creature.DamageType.Water)
//         {
//             return 53;
//         }

//         return 0;
//     }
//     public static void BTWArena_ArenaLivesInit(CompetitiveAddition.ArenaLives arenaLives)
//     {
//         if (arenaLives.abstractTarget == null) { return; }
//         OnlineCreature onlineCreature = arenaLives.abstractTarget.GetOnlineCreature();
//         if (onlineCreature == null) { return; }

//         bool IsMine = MeadowCompat.IsMine(arenaLives.abstractTarget);

//         arenaLives.fake = !IsMine;

//         if (IsMeadowLobby())
//         {
//             arenaLives.IsMeadowLobby = true;
//             arenaLives.canRespawn = false;

//             if (arenaLives.target is Player player && MeadowBTWArenaMenu.TryGetBTWArenaSettings(out var meadowArenaSettings))
//             {
//                 // arenaLives.canRespawn = meadowArenaSettings.ArenaLives_ReviveFromAbyss;
//                 arenaLives.lifes = meadowArenaSettings.ArenaLives_Amount;
//                 arenaLives.lifesleft = meadowArenaSettings.ArenaLives_Amount;
//                 arenaLives.reviveAdditionnalTime = meadowArenaSettings.ArenaLives_AdditionalReviveTime * BTWFunc.FrameRate;
//                 arenaLives.blockArenaOut = meadowArenaSettings.ArenaLives_BlockWin;
//                 arenaLives.reviveTime = meadowArenaSettings.ArenaLives_ReviveTime * BTWFunc.FrameRate;
//                 arenaLives.enforceAfterReachingZero = meadowArenaSettings.ArenaLives_Strict;
//                 arenaLives.shieldTime = meadowArenaSettings.ArenaLives_RespawnShieldDuration * BTWFunc.FrameRate;
//             }
//             if (IsMine && !arenaLives.fake)
//             {
//                 if (!onlineCreature.TryGetData<OnlineArenaLivesData>(out _))
//                 {
//                     onlineCreature.AddData(new OnlineArenaLivesData());
//                 }
//                 InvokeAllOtherPlayerWithRPCOnce(
//                     typeof(MeadowCompat).GetMethod(nameof(BTWArenaAddition_AddArenaLifesRPCEvent)).CreateDelegate(
//                         typeof(Action<RPCEvent, OnlineCreature>)),
//                         onlineCreature
//                 );
//             }
//         }
//     }    
//     public static void BTWArena_RPCArenaLivesDestroy(CompetitiveAddition.ArenaLives arenaLives)
//     {
//         if (arenaLives.abstractTarget == null) { return; }
//         OnlineCreature onlineCreature = arenaLives.abstractTarget.GetOnlineCreature();
//         if (onlineCreature == null) { return; }

//         if (!arenaLives.fake)
//         {
//             InvokeAllOtherPlayerWithRPCOnce(
//                 typeof(MeadowCompat).GetMethod(nameof(BTWArenaAddition_DestroyArenaLifesRPCEvent)).CreateDelegate(
//                     typeof(Action<RPCEvent, OnlineCreature>)),
//                     onlineCreature
//             );
//         }
//     }
//     public static bool IsPlayerAlive(AbstractCreature abstractPlayer)
//     {
//         if (abstractPlayer?.realizedCreature?.State != null)
//         {
//             return abstractPlayer.realizedCreature.State.alive;
//         }
//         else if (abstractPlayer?.state != null)
//         {
//             return abstractPlayer.state.alive;
//         }
//         return false;
//     }
//     public static void ResetDeathMessage(AbstractCreature abstractPlayer)
//     {
//         if (abstractPlayer.world?.game != null 
//             && abstractPlayer.GetOnlineObject() is OnlinePhysicalObject onlinePhysicalObject 
//             && onlinePhysicalObject != null)
//         {
//             var onlineHuds = abstractPlayer.world.game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();
//             foreach (var onlineHud in onlineHuds)
//             {
//                 onlineHud.killFeed.RemoveAll(x => x == onlinePhysicalObject.id);
//             }
//         }
//     }
//     public static void ResetIconOnRevival(AbstractCreature abstractPlayer)
//     {
//         if (abstractPlayer.world?.game != null 
//             && abstractPlayer.GetOnlineObject() is OnlinePhysicalObject onlinePhysicalObject 
//             && onlinePhysicalObject != null)
//         {
//             var onlineHuds = abstractPlayer.world.game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();
//             foreach (var onlineHud in onlineHuds)
//             {
//                 if (onlineHud.abstractPlayer == abstractPlayer)
//                 {
//                     onlineHud.playerDisplay.slugIcon.SetElementByName(onlineHud.playerDisplay.iconString);
//                 }
//             }
//         }
//     }
    
//     //----------- Hooks
//     public static void ApplyHooks()
//     {
//         InitDeathMessages();

//         On.Creature.ctor += Creature_AddDeathTracker;
//         On.Creature.Update += Creature_UpdateDeathTracker;
//         On.Creature.Violence += Creature_ViolenceDeathTracker;
//         On.Lizard.Violence  += Lizard_ViolenceDeathTracker;

//         foreach (var gamemodeDict in OnlineGameMode.gamemodes)
//         {
//             Type gameModeType = gamemodeDict.Value;
//             new Hook(gameModeType.GetMethod(nameof(OnlineGameMode.ShouldRegisterAPO)), OnlineGameMode_DoNotRegister);
//             new Hook(gameModeType.GetMethod(nameof(OnlineGameMode.ShouldSyncAPOInWorld)), OnlineGameMode_DoNotSyncInWorld);
//             new Hook(gameModeType.GetMethod(nameof(OnlineGameMode.ShouldSyncAPOInRoom)), OnlineGameMode_DoNotSyncInRoom);
//         }
//         new Hook(typeof(WorldSession).GetMethod(nameof(WorldSession.ApoLeavingWorld)), WorldSession_DoNotRegisterExit);
//         new Hook(typeof(RoomSession).GetMethod(nameof(RoomSession.ApoLeavingRoom)), RoomSession_DoNotRegisterExit);
//         new ILHook(typeof(DeathMessage).GetMethod(nameof(DeathMessage.CreatureDeath)), DeathMessage_ChangeContextFromTracker);
//         new ILHook(typeof(DeathMessage).GetMethod(nameof(DeathMessage.PlayerKillPlayer)), DeathMessage_GetNewDeathMessageFromTracker);
//         new ILHook(typeof(DeathMessage).GetMethod(nameof(DeathMessage.PlayerKillCreature)), DeathMessage_GetNewDeathMessageFromTracker);
        
//         new ILHook(typeof(FFA).GetMethod(nameof(FFA.IsExitsOpen)), FFA_DontOpenExitIfPlayerIsReviving);
//         new ILHook(typeof(TeamBattleMode).GetMethod(nameof(TeamBattleMode.IsExitsOpen)), TeamBattleMode_DontOpenExitIfPlayerIsReviving);
        
//         new Hook(typeof(ArenaOnlineGameMode).GetConstructor(new[] { typeof(Lobby) }), SetUpArenaDescription);

//         Plugin.Log("MeadowCompat ApplyHooks Done !");
//     }


//     public static HashSet<AbstractPhysicalObject.AbstractObjectType> deniedSyncedObjects = new()
//     {
//         CoreObject.EnergyCoreType
//     };

//     private static bool OnlineGameMode_DoNotRegister(Func<OnlineGameMode, OnlineResource, AbstractPhysicalObject, bool> orig, OnlineGameMode self, OnlineResource resource, AbstractPhysicalObject apo)
//     {
//         if (deniedSyncedObjects.Contains(apo.type)) { 
//             Plugin.Log(apo.ToString() + " shall not be replicated !");
//             return false; 
//         }
//         return orig(self, resource, apo);
//     }
//     private static bool OnlineGameMode_DoNotSyncInWorld(Func<OnlineGameMode, WorldSession, AbstractPhysicalObject, bool> orig, OnlineGameMode self, WorldSession ws, AbstractPhysicalObject apo)
//     {
//         if (deniedSyncedObjects.Contains(apo.type)) { 
//             Plugin.Log(apo.ToString() + " shall not be sync (world) !");
//             return false; 
//         }
//         return orig(self, ws, apo);
//     }
//     private static bool OnlineGameMode_DoNotSyncInRoom(Func<OnlineGameMode, RoomSession, AbstractPhysicalObject, bool> orig, OnlineGameMode self, RoomSession rs, AbstractPhysicalObject apo)
//     {
//         if (deniedSyncedObjects.Contains(apo.type)) { 
//             Plugin.Log(apo.ToString() + " shall not be sync (room) !");
//             return false; 
//         }
//         return orig(self, rs, apo);
//     }
//     private static void WorldSession_DoNotRegisterExit(Action<WorldSession, AbstractPhysicalObject> orig, WorldSession self, AbstractPhysicalObject apo)
//     {
//         if (deniedSyncedObjects.Contains(apo.type)) { 
//             Plugin.Log(apo.ToString() + " shall not be accounted in deletion (world) !");
//             return; 
//         }
//         orig(self, apo);
//     }
//     private static void RoomSession_DoNotRegisterExit(Action<RoomSession, AbstractPhysicalObject> orig, RoomSession self, AbstractPhysicalObject apo)
//     {
//         if (deniedSyncedObjects.Contains(apo.type)) { 
//             Plugin.Log(apo.ToString() + " shall not be accounted in deletion (room) !");
//             return; 
//         }
//         orig(self, apo);
//     }

//     public static void LogAllDeathMessages()
//     {
//         Plugin.Log("Here's all custom death messages :");
//         foreach (var m in customDeathMessagesEnum)
//         {
//             Plugin.Log($"   Custom death messsage {m.contextNum} \"target {m.deathMessagePre} killer{m.deathMessagePost}\"");
//         }
//     }
//     public static void InitDeathMessages()
//     {
//         customDeathMessagesEnum.Add( new(10, "was speared by", ".") );
//         customDeathMessagesEnum.Add( new(11, "was speared by", " using an explosive spear.") );
//         customDeathMessagesEnum.Add( new(12, "was speared by", " using an electric spear.") );
//         customDeathMessagesEnum.Add( new(13, "was speared by", " using a poisonous spear.") );
//         customDeathMessagesEnum.Add( new(14, "was reduced into a single food pip to", ".'") );
//         customDeathMessagesEnum.Add( new(15, "was given an involuntary umbilical by", ".'") );
//         customDeathMessagesEnum.Add( new(16, "was 360 no scoped by", ".") );
//         customDeathMessagesEnum.Add( new(17, "was sniped by", ".") );
//         customDeathMessagesEnum.Add( new(18, "was spear-fished by", ".") );
 
//         customDeathMessagesEnum.Add( new(20, "was blown apart by", ".") );
//         customDeathMessagesEnum.Add( new(21, "was obliterated by", ".") );
//         customDeathMessagesEnum.Add( new(22, "was zip-zapped by", ".") );
//         customDeathMessagesEnum.Add( new(23, "was given a sudden cardiac arrest by", ".") );
//         customDeathMessagesEnum.Add( new(24, "took a bath with a toaster named", ".") );

//         customDeathMessagesEnum.Add( new(30, "was mauled to death by", ".") );
//         customDeathMessagesEnum.Add( new(31, "was thrown into the void by", ".") );
//         customDeathMessagesEnum.Add( new(32, "didn't stand a chance against", "'s sheer momentum.") );
//         customDeathMessagesEnum.Add( new(33, "was bonked to death by", ".") );
//         customDeathMessagesEnum.Add( new(34, "was slugrolled by", ".") );
//         customDeathMessagesEnum.Add( new(35, "was flatened by", "'s sheer momentum.") );
//         customDeathMessagesEnum.Add( new(36, "was brutally mauled into pieces by", ".") );
//         customDeathMessagesEnum.Add( new(37, "was still not safe from", "'s bloodlust underwater.") );
//         customDeathMessagesEnum.Add( new(38, "clearly under-estimated", "'s ability to kill.") );

//         customDeathMessagesEnum.Add( new(40, "was doomed to die by", ".") );

//         customDeathMessagesEnum.Add( new(50, "was stabbed by", ".") );
//         customDeathMessagesEnum.Add( new(51, "was crushed by", ".") );
//         customDeathMessagesEnum.Add( new(52, "was brutally crushed in the jaws of", ".") );
//         customDeathMessagesEnum.Add( new(53, "took water damage from", ".") );
//         customDeathMessagesEnum.Add( new(54, "was exploded by", ".") );
//         customDeathMessagesEnum.Add( new(55, "was zapped by", ".") );

//         LogAllDeathMessages();

//         Plugin.Log("MeadowCompat custom kill messages init !");
//     }
//     public static void Creature_AddDeathTracker(On.Creature.orig_ctor orig, Creature self, AbstractCreature abstractCreature, World world)
//     {
//         orig(self, abstractCreature, world);
//         if (world.game.IsArenaSession && !arenaCreatureDeathTrackers.TryGetValue(self.abstractCreature, out _))
//         {
//             arenaCreatureDeathTrackers.Add(self.abstractCreature, new(self));
//             Plugin.Log($"DeathTracker added to [{self}] !");
//         }
//     }
//     public static void Creature_UpdateDeathTracker(On.Creature.orig_Update orig, Creature self, bool eu)
//     {
//         // Plugin.Log($"Update detected ! Attemping to set DeathTracker of [{self}]...");
//         orig(self, eu);
//         if (arenaCreatureDeathTrackers.TryGetValue(self.abstractCreature, out var deathTracker))
//         {
//             if (deathTracker.deathMessageCustom != 0 && self.killTagCounter <= 0)
//             {
//                 deathTracker.deathMessageCustom = 0;
//                 Plugin.Log($"DeathTracker of [{self}] set back to 0.");
//             }
//         }
//     }

//     private static void SetUpArenaDescription(Action<ArenaOnlineGameMode, Lobby> orig, ArenaOnlineGameMode self, Lobby lobby)
//     {
//         orig(self, lobby);

//         self.slugcatSelectMenuScenes.Remove("Trailseeker");
//         self.slugcatSelectMenuScenes.Add("Trailseeker", MenuScene.SceneID.Landscape_SI);
//         self.slugcatSelectMenuScenes.Remove("Core");
//         self.slugcatSelectMenuScenes.Add("Core", MenuScene.SceneID.Landscape_SS);
//         self.slugcatSelectMenuScenes.Remove("Spark");
//         self.slugcatSelectMenuScenes.Add("Spark", MenuScene.SceneID.Landscape_UW);

//         self.slugcatSelectDisplayNames.Remove("Trailseeker");
//         self.slugcatSelectDisplayNames.Add("Trailseeker", "THE TRAILSEEKER");
//         self.slugcatSelectDisplayNames.Remove("Core");
//         self.slugcatSelectDisplayNames.Add("Core", "THE CORE");
//         self.slugcatSelectDisplayNames.Remove("Spark");
//         self.slugcatSelectDisplayNames.Add("Spark", "THE SPARK");

//         self.slugcatSelectDescriptions.Remove("Trailseeker");
//         self.slugcatSelectDescriptions.Add("Trailseeker", "Your journey gave you the experience to deal with that threat.<LINE>Attack from angles they can't reach.");
//         self.slugcatSelectDescriptions.Remove("Core");
//         self.slugcatSelectDescriptions.Add("Core", "A last threat between you and your mission.<LINE>Leap yourself to victory.");
//         self.slugcatSelectDescriptions.Remove("Spark");
//         self.slugcatSelectDescriptions.Add("Spark", "Cornered, by not powerless.<LINE>Zap them with agility.");
//     }

//     private static void ViolenceCheck(Creature self, BodyChunk source, Creature.DamageType type, float damage, float stunBonus)
//     {
//         if (self != null 
//             && self.abstractCreature != null 
//             && arenaCreatureDeathTrackers.TryGetValue(self.abstractCreature, out var deathTracker))
//         {
//             int newContext = GetCustomDeathMessageOfViolence(self, deathTracker, source?.owner, type, damage, stunBonus);
//             deathTracker.deathMessageCustom = newContext;
//             Plugin.Log($"DeathTracker of [{self}] set to <{newContext}> !");
//         }
//     }
//     private static void Lizard_ViolenceDeathTracker(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
//     {
//         Plugin.Log($"Violence (on lizard) detected ! Attemping to set DeathTracker of [{self}]...");
//         ViolenceCheck(self, source, type, damage, stunBonus);
//         orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
//     }
//     public static void Creature_ViolenceDeathTracker(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
//     {
//         Plugin.Log($"Violence detected ! Attemping to set DeathTracker of [{self}]...");
//         ViolenceCheck(self, source, type, damage, stunBonus);
//         orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
//     }

//     private static int ChangeContext(int orig, Creature creature)
//     {
//         if (arenaCreatureDeathTrackers.TryGetValue(creature.abstractCreature, out var deathTracker) && deathTracker.deathMessageCustom >= 10)
//         {
//             Plugin.Log($"[{creature}] death context changed to <{deathTracker.deathMessageCustom}> !");
//             return deathTracker.deathMessageCustom;
//         }
//         return orig;
//     }
//     private static void DeathMessage_ChangeContextFromTracker(ILContext il)
//     {
//         Plugin.Log("MeadowCompat IL 1 starts");
//         try
//         {
//             Plugin.Log("Trying to hook IL");
//             ILCursor cursor = new(il);
//             if (cursor.TryGotoNext(MoveType.After,
//                 x => x.MatchLdarg(0),
//                 x => x.MatchLdfld<Creature>(nameof(Creature.killTag)),
//                 x => x.MatchCallOrCallvirt(typeof(AbstractCreature).GetProperty(nameof(AbstractCreature.realizedCreature)).GetGetMethod()),
//                 x => x.MatchIsinst(typeof(Player)),
//                 x => x.MatchLdarg(0),
//                 x => x.MatchLdcI4(0)
//             ))
//             {
//                 cursor.Emit(OpCodes.Ldarg_0);
//                 cursor.EmitDelegate(ChangeContext);
//             }
//             else
//             {
//                 Plugin.logger.LogError("Couldn't find IL hook :<");
//                 Plugin.Log(il);
//             }
//             Plugin.Log("IL hook ended");
//         }
//         catch (Exception ex)
//         {
//             Plugin.logger.LogError(ex);
//         }
//         Plugin.Log("MeadowCompat IL 1 ends");
//     }

//     private static string ChangeContextPre(string orig, int context)
//     {
//         Plugin.Log($"[{orig}] death message detected (pre), with context <{context}>.");
//         if (customDeathMessagesEnum.Exists(x => x.contextNum == context))
//         { 
//             string newText = customDeathMessagesEnum.Find(x => x.contextNum == context).deathMessagePre;
//             Plugin.Log($"[{orig}] death message changed to <{newText}> !");
//             return newText; 
//         }
//         else if (context >= 10)
//         {
//             Plugin.Log("Couldn't find the custom message...?");
//             LogAllDeathMessages();
//         }
//         return orig;
//     }
//     private static string ChangeContextPost(string orig, int context)
//     {
//         Plugin.Log($"[{orig}] death message detected (pos), with context <{context}>.");
//         if (customDeathMessagesEnum.Exists(x => x.contextNum == context))
//         { 
//             string newText = customDeathMessagesEnum.Find(x => x.contextNum == context).deathMessagePost;
//             Plugin.Log($"[{orig}] death message changed to <{newText}> !");
//             return newText; 
//         }
//         return orig;
//     }
//     private static void DeathMessage_GetNewDeathMessageFromTracker(ILContext il)
//     {
//         Plugin.Log("MeadowCompat IL 2 starts");
//         try
//         {
//             Plugin.Log("Trying to hook IL");
//             ILCursor cursor = new(il);

//             if (cursor.TryGotoNext(MoveType.After,
//                 x => x.MatchLdstr("was slain by")
//             ))
//             {
//                 cursor.Emit(OpCodes.Ldarg_2);
//                 cursor.EmitDelegate(ChangeContextPre);
//             }
//             else
//             {
//                 Plugin.logger.LogError("Couldn't find IL hook 1 :<");
//                 Plugin.Log(il);
//             }
//             if (cursor.TryGotoNext(MoveType.After,
//                 x => x.MatchLdstr(".")
//             ))
//             {
//                 cursor.Emit(OpCodes.Ldarg_2);
//                 cursor.EmitDelegate(ChangeContextPost);
//             }
//             else
//             {
//                 Plugin.logger.LogError("Couldn't find IL hook 2 :<");
//                 Plugin.Log(il);
//             }

//             Plugin.Log("IL hook ended");
//         }
//         catch (Exception ex)
//         {
//             Plugin.logger.LogError(ex);
//         }
//         Plugin.Log("MeadowCompat IL 2 ends");
//     }
    
//     private static int ChangePlayerCount(int orig, ExitManager exitManager)
//     {
//         // Plugin.Log($"The current player count is <{orig}>, with exit manager [{exitManager}] of arena [{exitManager?.gameSession}]."); 
//         if (exitManager?.gameSession != null)
//         {
//             int addcount = CompetitiveAddition.AdditionalPlayerInArenaCount(exitManager.gameSession);
//             // if (addcount > 0) { Plugin.Log($"Hold on ! They say there's {orig} player but I say there's {orig + addcount} actually !"); }
//             // else { Plugin.Log($"The current player count is {orig}, and no one else is reviving (count = {addcount}).");  }
//             return orig + addcount;
//         }
//         return orig;
//     }
//     private static void FFA_DontOpenExitIfPlayerIsReviving(ILContext il)
//     {
//         Plugin.Log("MeadowCompat IL 3 starts");
//         try
//         {
//             Plugin.Log("Trying to hook IL");
//             ILCursor cursor = new(il);

//             if (cursor.TryGotoNext(MoveType.Before,
//                 x => x.MatchStloc(0),
//                 x => x.MatchLdloc(0),
//                 x => x.MatchLdcI4(1),
//                 x => x.MatchBneUn(out _)
//             ))
//             {
//                 cursor.GotoNext(MoveType.After, x => x.MatchLdloc(0));
//                 cursor.Emit(OpCodes.Ldarg_3);
//                 cursor.EmitDelegate(ChangePlayerCount);
//             }
//             else
//             {
//                 Plugin.logger.LogError("Couldn't find IL hook :<");
//                 Plugin.Log(il);
//             }

//             Plugin.Log("IL hook ended");
//         }
//         catch (Exception ex)
//         {
//             Plugin.logger.LogError(ex);
//         }
//         Plugin.Log("MeadowCompat IL 3 ends");
//     }
//     private static bool CheckPlayerAsAlive(bool orig, AbstractCreature abstractCreature, ExitManager exitManager)
//     {
//         if (!CompetitiveAddition.ReachedMomentWhenLivesAreSetTo0(exitManager?.gameSession))
//         {
//             return orig || CompetitiveAddition.PlayerCountedAsAliveInArena(abstractCreature);
//         }
//         return orig;
//     }
//     private static void TeamBattleMode_DontOpenExitIfPlayerIsReviving(ILContext il)
//     {
//         Plugin.Log("MeadowCompat IL 4 starts");
//         try
//         {
//             Plugin.Log("Trying to hook IL");
//             ILCursor cursor = new(il);

//             if (cursor.TryGotoNext(MoveType.Before,
//                 x => x.MatchStloc(0),
//                 x => x.MatchLdloc(0),
//                 x => x.MatchLdcI4(1),
//                 x => x.MatchBneUn(out _)
//             ))
//             {
//                 cursor.GotoNext(MoveType.After, x => x.MatchLdloc(0));
//                 cursor.Emit(OpCodes.Ldarg_3);
//                 cursor.EmitDelegate(ChangePlayerCount);
//             }
//             else
//             {
//                 Plugin.logger.LogError("Couldn't find IL hook 1 :<");
//                 Plugin.Log(il);
//             }

//             if (cursor.TryGotoNext(MoveType.After,
//                 x => x.MatchLdloc(8),
//                 x => x.MatchCallOrCallvirt(typeof(AbstractCreature).GetProperty(nameof(AbstractCreature.realizedCreature)).GetGetMethod()),
//                 x => x.MatchCallOrCallvirt(typeof(Creature).GetProperty(nameof(Creature.State)).GetGetMethod()),
//                 x => x.MatchCallOrCallvirt(typeof(CreatureState).GetProperty(nameof(CreatureState.alive)).GetGetMethod())
//             ))
//             {
//                 cursor.Emit(OpCodes.Ldloc_S, (byte)8);
//                 cursor.Emit(OpCodes.Ldarg_2);
//                 cursor.EmitDelegate(CheckPlayerAsAlive);
//             }
//             else
//             {
//                 Plugin.logger.LogError("Couldn't find IL hook 2 :<");
//                 Plugin.Log(il);
//             }

//             Plugin.Log("IL hook ended");
//         }
//         catch (Exception ex)
//         {
//             Plugin.logger.LogError(ex);
//         }
//         Plugin.Log("MeadowCompat IL 4 ends");
//     }
// }
