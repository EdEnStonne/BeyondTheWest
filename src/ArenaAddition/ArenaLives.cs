using System;
using BeyondTheWest;
using UnityEngine;
using RWCustom;
using HUD;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using UnityEngine.Assertions.Must;
using System.Linq;
using BeyondTheWest.MeadowCompat;

namespace BeyondTheWest.ArenaAddition;
public class ArenaLives : UpdatableAndDeletable, IDrawable
{
    public static ConditionalWeakTable<AbstractCreature, ArenaLives> arenaLivesList = new();
    public static bool TryGetLives(AbstractCreature creature, out ArenaLives lives)
    {
        return arenaLivesList.TryGetValue(creature, out lives);
    }
    public static ArenaLives GetLives(AbstractCreature creature)
    {
        TryGetLives(creature, out ArenaLives lives);
        return lives;
    }
    public static bool PlayerCountedAsAliveInArena(AbstractCreature abstractPlayer)
    {
        return abstractPlayer != null 
            && (
                (abstractPlayer.state != null && !abstractPlayer.state.alive) 
                || (Plugin.meadowEnabled && !MeadowFunc.IsPlayerAlive(abstractPlayer))
                || (
                    abstractPlayer.realizedCreature != null 
                    && (abstractPlayer.realizedCreature as Player).dangerGrasp != null
                    )
                )
            && ArenaLives.TryGetLives(abstractPlayer, out var arenaLives) 
            && arenaLives.blockArenaOut
            && arenaLives.lifesleft > 0;
    }
    public static int AdditionalPlayerInArenaCount(ArenaGameSession arenaGame)
    {
        int revivingPlayers = 0;
        if (!CompetitiveAddition.ReachedMomentWhenLivesAreSetTo0(arenaGame) && arenaGame != null && arenaGame.Players != null)
        {
            try
            {
                // Plugin.Log(arenaGame);
                // Plugin.Log(arenaGame.Players);
                revivingPlayers = arenaGame.Players?.Count(x => PlayerCountedAsAliveInArena (x)) ?? 0;
            }
            catch (Exception ex)
            {
                Plugin.logger.LogError("Something wrong happened while counting players ! " + ex);
            }
            // for (int i = 0; i < arenaGame.Players.Count; i++)
            // {
            //     // LogLivesState(arenaGame.Players[i]);
            //     if (PlayerCountedAsAliveInArena(arenaGame.Players[i]))
            //     {
            //         revivingPlayers++;
            //         // Plugin.Log($"Hold on ! {arenaGame.Players[i]} is reviving ! Count at {revivingPlayers}");
            //     }
            // }
        }
        // Plugin.Log($"Returning count {revivingPlayers}");
        return revivingPlayers;
    }

    public ArenaLives(AbstractCreature abstractCreature, int lifes, int reviveTime, int reviveAdditionnalTime, bool blockArenaOut, bool fake = false) 
    {
        this.abstractTarget = abstractCreature;
        this.reviveTime = reviveTime;
        this.reviveAdditionnalTime = reviveAdditionnalTime;
        this.lifes = lifes;
        this.lifesleft = lifes;
        this.fake = fake;
        this.blockArenaOut = blockArenaOut;
        if (this.target != null)
        {
            if (this.target is Player player)
            {
                this.baseColor = player.ShortCutColor();
                this.canRespawn = true;
            }
            else
            {
                this.canRespawn = false;
            }
            if (this.CreatureMainChunk != null)
            {
                this.pos = this.CreatureMainChunk.pos;
                this.firstPos = this.pos;
            }
        }
        if (ArenaLives.TryGetLives(abstractCreature, out var arenaLives))
        {
            arenaLives.Destroy();
        }
        arenaLivesList.Add(abstractCreature, this);
        if (Plugin.meadowEnabled)
        {
            MeadowCalls.BTWArena_ArenaLivesInit(this);
        }
        if (lifes > 1)
        {
            DisplayLives();
        }
    }
    public ArenaLives(AbstractCreature abstractCreature, int lifes, int reviveTime, int reviveAdditionnalTime, bool fake = false) 
        : this(abstractCreature, lifes, reviveTime, reviveAdditionnalTime, true, fake) { }
    public ArenaLives(AbstractCreature abstractCreature, int lifes, bool fake = false)
        : this(abstractCreature, lifes, BTWFunc.FrameRate * 15, BTWFunc.FrameRate * 5, fake) { }
    public ArenaLives(AbstractCreature abstractCreature, bool fake = false)
        : this(abstractCreature, 3, fake) { }
    
    private void ResetVariablesOnRevival()
    {
        wasAbstractCreatureDestroyed = false;
        if (this.target is Player player) {

            BTWFunc.ResetCore(player);
            BTWFunc.ResetSpark(player);

            if (this.shieldTime > 0)
            {
                player.room?.AddObject( new ArenaShield(player, this.shieldTime) );
            }

            if (Plugin.meadowEnabled)
            {
                MeadowFunc.ResetDeathMessage(this.abstractTarget);
                MeadowFunc.ResetSlugcatIcon(this.abstractTarget);
            }
        }
    }
    public void ReviveCreature() // taken from Mouse drag method of revival, credit to them !
    {
        AbstractCreature ac = this.abstractTarget;
        if (ac == null) { return; }
        if (ac?.state == null) { return; }

        Creature creature = this.target;
        if (creature == null) { return; }

        if (ac.state is HealthState && (ac.state as HealthState).health < 1f)
            (ac.state as HealthState).health = 1f;
        ac.state.alive = true;
        creature.dead = false;
        creature.stun = 10; 
        creature.Hypothermia = 0f;
        creature.HypothermiaExposure = 0f;
        creature.injectedPoison = 0f;

        ac.abstractAI?.SetDestination(ac.pos);

        if (creature is Hazer hazer) {
            hazer.inkLeft = 1f;
            hazer.hasSprayed = false;
            hazer.clds = 0;
        }

        if (creature is Player player) {
            BTWFunc.ResetCore(player);
            BTWFunc.ResetSpark(player);

            for (int i = 0; i < player.room?.game?.cameras?.Length; i++)
                if (player.room.game.cameras[i]?.hud?.textPrompt != null)
                    player.room.game.cameras[i].hud.textPrompt.gameOverMode = false;

            if (player.room?.game?.arenaOverlay != null) {
                player.room.game.arenaOverlay.ShutDownProcess();
                player.room.game.manager?.sideProcesses?.Remove(player.room.game.arenaOverlay);
                player.room.game.arenaOverlay = null;
                if (player.room.game.session is ArenaGameSession arenaGameSession) {
                    arenaGameSession.sessionEnded = false;
                    arenaGameSession.challengeCompleted = false;
                    arenaGameSession.endSessionCounter = -1;
                }
            }

            player.exhausted = false;
            player.lungsExhausted = false;
            player.airInLungs = 1f;
            player.aerobicLevel = 0f;
            if (player.playerState != null) {
                player.playerState.permaDead = false;
                player.playerState.permanentDamageTracking = 0.0;
            }
            player.animation = Player.AnimationIndex.None;
        }
        else
        {
            this.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, CreatureMainChunk, false, 0.65f, 2f + BTWFunc.random * 0.5f);
        }
        ResetVariablesOnRevival();
    }
    public void Respawn() // taken from Dev Console, credit to them !
    {
        if (!this.fake 
            && this.canRespawn
            && !this.CreatureStillValid 
            && this.abstractTarget != null 
            && (this.target == null || this.target.room == null )
            && this.room != null)
        {
            this.abstractTarget.realizedObject?.Destroy();
            this.abstractTarget.Abstractize(this.room.GetWorldCoordinate(this.firstPos));
            if(this.abstractTarget.state is PlayerState state)
            {
                state.alive = true;
                state.permaDead = false;
                state.permanentDamageTracking = 0f;
            }
            this.abstractTarget.RealizeInRoom();
            if (this.target != null)
            {
                foreach (var chunk in this.target.bodyChunks)
                {
                    chunk.pos = this.firstPos;
                }
            }
            if (this.abstractTarget.realizedObject is Player realPlayer)
            {
                realPlayer.leechedOut = false;
            }

            // Reset HUD
            RainWorldGame game = this.abstractTarget.world.game;
            foreach(var cam in game.cameras)
            {
                if (cam.hud != null && (cam.hud.owner as Player)?.abstractCreature == this.abstractTarget)
                {
                    if (cam.hud.textPrompt != null)
                        cam.hud.textPrompt.gameOverMode = false;

                    cam.hud.owner = this.abstractTarget.realizedObject as Player;
                }
            }
            
            this.countedAlive = this.target != null ? this.target.State.alive : false; 
            DisplayLives(false);
            ResetVariablesOnRevival();
        }
    }
    public void TriggerRevive() 
    {
        Plugin.Log("Let's try reviving ["+ this.abstractTarget +"] !\nCounter at : <"
            + this.reviveCounter +">, fake : <"+ this.fake +">, gone : <"+
            (!this.CreatureStillValid 
                && this.abstractTarget != null 
                && (this.target == null || this.target.room == null )
                && this.room != null)+">, here : <"+ this.target 
                +">, canRespawn : <"+ this.canRespawn +">, Room : <"+ this.room +">, Meadow : <"+ this.IsMeadowLobby +">");
        if (!this.fake)
        {
            if (!this.CreatureStillValid 
                && this.abstractTarget != null 
                && (this.target == null || this.target.room == null )
                && this.room != null)
            {
                if (!this.canRespawn){ this.lifesleft = 0; }
                else 
                {
                    Respawn();
                }
                return;
            }

            Creature creature = this.target;
            if (creature == null || creature.room == null) { this.lifesleft = 0; return; }

            ReviveCreature();
            Plugin.Log("Attempted to revive ["+ creature +"].");
            
            this.countedAlive = creature.State.alive; 
            DisplayLives(false);
        }
        
    }
    public void InitRevive()
    {
        this.lifesleft--;
        if (this.lifesleft > 0)
        {
            this.reviveCounter = this.TotalReviveTime;
            if (this.room != null)
            {
                this.firstPos = BTWFunc.RandomExitPos(this.room);
                this.abstractTarget?.Move(this.room.GetWorldCoordinate(this.firstPos));
            }
            DisplayLives();
        }
    }
    public void DisplayLives(bool sound = true)
    {
        this.karmaSymbolNeedToChange = true;
        this.livesDisplayCounter = livesDisplayCounterMax;
        if (sound && this.CreatureMainChunk != null && !this.fake)
        {
            this.room.PlaySound(SoundID.HUD_Exit_Game, CreatureMainChunk, false, 1f, 1.5f + BTWFunc.random * 0.5f);
        }
    }
    public void Dismiss()
    {
        if (this.lifesleft > 1)
        {
            DisplayLives(!this.fake);
        }
        this.lifesleft = 0;
        this.reviveCounter = 0;
        this.karmaSymbolNeedToChange = true;
    }

    public override void Destroy()
    {
        base.Destroy();
        if (this.abstractTarget != null)
        {
            if (!fake && Plugin.meadowEnabled)
            {
                MeadowCalls.BTWArena_RPCArenaLivesDestroy(this);
            }
            if (ArenaLives.TryGetLives(this.abstractTarget, out _))
            {
                arenaLivesList.Remove(this.abstractTarget);
            }
        }
    }
    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.abstractTarget == null) { this.Destroy(); return; }
        
        if (this.livesDisplayCounter > 0) { this.livesDisplayCounter--; }

        if (this.room?.game != null && this.room.game.IsArenaSession && this.room.game.GetArenaGameSession is ArenaGameSession arena)
        {
            if (CompetitiveAddition.ReachedMomentWhenLivesAreSetTo0(arena) && this.lifesleft != 0)
            {
                Dismiss();
            }
        }
        
        if (!this.fake)
        {
            bool alive = this.abstractTarget.state.alive;
            bool creatureValid = this.CreatureStillValid;
            if (this.countedAlive)
            {
                if (!alive)
                {
                    this.countedAlive = false;
                    InitRevive();
                    Plugin.Log("Oh no ! ["+ this.abstractTarget +"] died ! We must revive them, they have "+ this.lifesleft +" lives left.");
                }
            }
            else
            {
                if (alive)
                {
                    Plugin.Log("Seems like ["+ this.abstractTarget +"] revived without us noticing...");
                    this.reviveCounter = 0;
                    if (this.lifesleft <= 0)
                    {
                        if (!this.fake && this.enforceAfterReachingZero && creatureValid)
                        {
                            this.room.AddObject( new ArenaForcedDeath(this.abstractTarget) );
                        }
                    }
                    this.countedAlive = true;
                }
                else if (this.lifesleft > 0)
                {
                    this.reviveCounter--;
                    // Plugin.Log("Seems like ["+ this.abstractTarget +"] is reviving ! Counter at : <"+ this.reviveCounter +">");
                    if (this.reviveCounter <= 0)
                    { TriggerRevive(); }

                    if (!this.canRespawn && (this.target == null || this.target.room == null)) 
                    { this.Destroy(); return; }
                }
                else
                {
                    // Plugin.Log("Seems like ["+ this.abstractTarget +"] ran out of lives ! Welp.");
                }
            }
        }
        else
        {
            if (this.reviveCounter > 0) { this.reviveCounter--; }
        }
        
    }
    
    public float GetCircleFraction(int circle)
    {
        float timePerCircle = (float)this.TotalReviveTime / this.circlesAmount;
        return Mathf.Clamp01((this.reviveCounter - timePerCircle * circle) / timePerCircle);
    }
    public void SetKarmaAccordingToLives(RoomCamera.SpriteLeaser sLeaser)
    {
        int karma = Mathf.Clamp(this.lifesleft - (this.countedAlive ? 1 : 0), 0, 9);
        Color color = this.countedAlive && this.lifesleft > 0 ? Color.white : Color.red;
        sLeaser.sprites[1].SetElementByName(KarmaMeter.KarmaSymbolSprite(false, new IntVector2(karma, karma)));
        sLeaser.sprites[1].color = color;
        sLeaser.sprites[0].color = color;
        this.karmaSymbolNeedToChange = false;
    }
    public void SetCircleCountAccordingToReviveTime()
    {
        this.circlesAmount = Mathf.Clamp(this.TotalReviveTime / (BTWFunc.FrameRate * 3), 6, circlesAmountMax);
    }
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        SetCircleCountAccordingToReviveTime();
        sLeaser.sprites = new FSprite[circlesAmountMax + 2];

        FSprite BubbleLives = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
            color = Color.white,
            alpha = 0f,
            scale = 1f
        };
        sLeaser.sprites[0] = BubbleLives;

        FSprite karmaSprite = new FSprite(KarmaMeter.KarmaSymbolSprite(false, new IntVector2(0, 0)), true)
        {
            color = Color.white,
            alpha = 0f
        };
        sLeaser.sprites[1] = karmaSprite;

        SetKarmaAccordingToLives(sLeaser);

        for (int i = 0; i < circlesAmountMax; i++)
        {
            sLeaser.sprites[i + 2] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
                color = Color.white,
                alpha = 0f,
                scale = 1.0f
            };
        }

        this.AddToContainer(sLeaser, rCam, null);
    }
    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
        {
            sLeaser.CleanSpritesAndRemove();
            return;
        }
        if (this.abstractTarget == null) { sLeaser.CleanSpritesAndRemove(); return; }

        Vector2 position = this.firstPos;
        if (this.CreatureMainChunk != null)
        {
            this.pos = this.CreatureMainChunk.pos;
            position = this.pos;
        }

        if (this.karmaSymbolNeedToChange) 
        { 
            SetCircleCountAccordingToReviveTime();
            SetKarmaAccordingToLives(sLeaser); 
        }

        float easedRevive = BTWFunc.EaseOut(this.FractionRevive, 4);
        float easedDisplay = BTWFunc.EaseIn(this.FractionDisplay, 2);

        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.x = position.x - camPos.x;
            sprite.y = position.y - camPos.y;
            sprite.alpha = 0f;
        }

        if (this.CreatureStillValid && this.livesDisplayCounter > 0)
        {
            float height = 60f;
            if (this.target is Player player && ArenaShield.TryGetShield(player, out var shield) && shield.Shielding)
                { height = 85f; }
            if (this.target is Player && this.IsMeadowLobby)
                { height = 120f; }
            sLeaser.sprites[0].y += height;
            sLeaser.sprites[1].y += height;

            float scale = 1f - 0.5f * easedDisplay;
            sLeaser.sprites[0].scale = 2f * scale;
            sLeaser.sprites[1].scale = 0.5f * scale;
            
            sLeaser.sprites[0].alpha = 0.2f * easedDisplay;
            sLeaser.sprites[1].alpha = easedDisplay;
        }

        if (this.reviveCounter > 0)
        {
            for (int i = 1; i < this.circlesAmount; i++)
            {
                sLeaser.sprites[i + 1].x += Mathf.Sin(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 30f;
                sLeaser.sprites[i + 1].y += Mathf.Cos(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 30f;
                sLeaser.sprites[i + 1].color = this.baseColor;
                sLeaser.sprites[i + 1].alpha = 0.35f + 0.65f * (1 - GetCircleFraction(i - 1));
                sLeaser.sprites[i + 1].scale = 0.5f * BTWFunc.EaseOut(GetCircleFraction(i - 1), 3);
            }
        }

        if (this.target != null && (this.target.inShortcut || this.target.room == null))
        {
            sLeaser.sprites[0].alpha = 0f;
            sLeaser.sprites[1].alpha = 0f;
        }
    }
    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        foreach (FSprite sprite in sLeaser.sprites)
        {
            rCam.ReturnFContainer("HUD").AddChild(sprite);
        }
    }

    public AbstractCreature abstractTarget;
    public Color baseColor = Color.white;
    public int lifes = 1;
    public int lifesleft = 1;
    public int reviveCounter = 0;
    public int livesDisplayCounter = 0;
    public int circlesAmount = 0;
    public const int circlesAmountMax = 15;
    public int shieldTime = BTWFunc.FrameRate * 10;
    public const int livesDisplayCounterMax = BTWFunc.FrameRate * 3;
    public int reviveTime = BTWFunc.FrameRate * 10;
    public int reviveAdditionnalTime = BTWFunc.FrameRate * 5;
    public bool fake = false;
    public bool blockArenaOut = true;
    public bool enforceAfterReachingZero = true;
    public bool countedAlive = true;
    public bool karmaSymbolNeedToChange = false;
    public bool canRespawn = false;
    public bool IsMeadowLobby = false;
    public bool wasAbstractCreatureDestroyed = false;
    // public int respawnPlayerID = -1;
    public Vector2 pos;
    public Vector2 firstPos;
    public int TotalReviveTime
    {
        get
        {
            return Mathf.Max(BTWFunc.FrameRate * 1, reviveTime + reviveAdditionnalTime * (lifes - lifesleft - 1));
        }
    }
    public float FractionRevive
    {
        get
        {
            return Mathf.Clamp01((float)reviveCounter / TotalReviveTime);
        }
    }
    public float FractionDisplay
    {
        get
        {
            return Mathf.Clamp01((float)livesDisplayCounter / livesDisplayCounterMax);
        }
    }
    
    public Creature target
    {
        get
        {
            return abstractTarget?.realizedCreature;
        }
    }
    public BodyChunk CreatureMainChunk
    {
        get
        {
            if (this.room != null && this.target != null && this.target.room != null) 
                { return this.target.mainBodyChunk ?? this.target.firstChunk; }
            return null;
        }
    }
    public bool CreatureStillValid
    {
        get
        {
            return this.room != null && this.target != null && this.target.room != null;
        }
    }
}

public static class ArenaLivesHooks
{
    public static void ApplyHooks()
    {
        On.RainCycle.ArenaEndSessionRain += RainCycle_SuddenDeath;
        On.UpdatableAndDeletable.Destroy += Creature_DontDestroyIfReviving;
        On.AbstractWorldEntity.Destroy += AbstractCreature_DontDestroyIfReviving;
        On.KarmaFlower.BitByPlayer += KarmaFlower_AddLifeToPlayer;
        Plugin.Log("CompetitiveAddition ApplyHooks Done !");
    }


    public static void ApplyPostHooks()
    {
        On.ArenaGameSession.PlayersStillActive += ArenaGameSession_AddRevivingPlayers;
        Plugin.Log("CompetitiveAddition ApplyPostHooks Done !");
    }
    
    public static void LogLivesState(AbstractCreature abstractPlayer)
    {
        if (abstractPlayer == null) { return; } 
        Plugin.Log($"Logging lives state of {abstractPlayer} :");
        Plugin.Log($"{abstractPlayer} : "
            + $"\nalive <{abstractPlayer.state.alive }>\n"
            + $"meadowAlive <{Plugin.meadowEnabled && MeadowFunc.IsPlayerAlive(abstractPlayer)}>"
            + $"\nin danger <{abstractPlayer.realizedCreature != null && (abstractPlayer.realizedCreature as Player).dangerGrasp != null}>"
            + $"\nshould count even if dead <" +
                (!abstractPlayer.state.alive 
                || (Plugin.meadowEnabled && !MeadowFunc.IsPlayerAlive(abstractPlayer))
                || (
                    abstractPlayer.realizedCreature != null 
                    && (abstractPlayer.realizedCreature as Player).dangerGrasp != null
                    )
                )+">");
        if (ArenaLives.TryGetLives(abstractPlayer, out var arenaLives))
        {
            Plugin.Log($"Lives state found ! \n"
                + $"Lives <{arenaLives.lifes}>\n"
                + $"Lives Left <{arenaLives.lifesleft}>\n"
                + $"Block Out Arena <{arenaLives.blockArenaOut}>\n"
                + $"Strict <{arenaLives.enforceAfterReachingZero}>\n"
                + $"Reviving block <{ArenaLives.PlayerCountedAsAliveInArena(abstractPlayer)}>\n");
        }
        else
        {
            Plugin.Log("No lives states !");
        }
    }
    

    // Hooks  
    private static void KarmaFlower_AddLifeToPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (self.bites < 1
            && grasp.grabber is Player player
            && (grasp.grabber as Player).room.game.session is ArenaGameSession
            && player.abstractCreature != null
            && ArenaLives.TryGetLives(player.abstractCreature, out var lives))
        {
            lives.lifesleft += 1;
            lives.DisplayLives();
        }
    }
    private static void Creature_DontDestroyIfReviving(On.UpdatableAndDeletable.orig_Destroy orig, UpdatableAndDeletable self)
    {
        if (self.room != null
            && self is Creature creature
            && creature != null
            && creature.abstractCreature != null
            && ArenaLives.TryGetLives(creature.abstractCreature, out var lives)
            && lives.canRespawn
            && lives.lifesleft > 0)
        {
            if (creature == creature.abstractCreature.realizedCreature)
            {
                creature.Die();
                creature.abstractCreature.Abstractize(self.room.GetWorldCoordinate(lives.firstPos));
                Plugin.Log($"Creature [{creature}] was destroyed while having some lives left !");
                // return;
            }
            else
            {
                Plugin.Log($"Seems like [{creature}] is not the same as [{creature.abstractCreature}]'s [{creature.abstractCreature.realizedCreature}]. Destroying it.");
            }
        }
        orig(self);
    }
    private static void AbstractCreature_DontDestroyIfReviving(On.AbstractWorldEntity.orig_Destroy orig, AbstractWorldEntity self)
    {
        if (self.Room != null
            && self.Room.realizedRoom != null
            && self is AbstractCreature abstractCreature
            && abstractCreature != null
            && ArenaLives.TryGetLives(abstractCreature, out var lives))
        {
            if (lives.canRespawn && lives.lifesleft > 0)
            {
                abstractCreature.Die();
                lives.wasAbstractCreatureDestroyed = true;
                abstractCreature.Abstractize(self.Room.realizedRoom.GetWorldCoordinate(lives.firstPos));
                Plugin.Log($"Stopped Abstract Creature [{abstractCreature}] from being destroyed, so they can revive in peace.");
                return;
            }
            else
            {
                lives.Dismiss();
                Plugin.Log($"Abstract Creature [{abstractCreature}] is being destroyed and cannot revive, dismissing the resting lives.");
            }
        }
        orig(self);
    }
    private static void RainCycle_SuddenDeath(On.RainCycle.orig_ArenaEndSessionRain orig, RainCycle self)
    {
        orig(self);
        if (self?.world?.game?.GetArenaGameSession is ArenaGameSession arena && arena != null && arena.room != null)
        {
            foreach (AbstractCreature abstractCreature in arena.room.abstractRoom.creatures)
            {
                if (ArenaLives.TryGetLives(abstractCreature, out var arenaLives))
                {
                    arenaLives.Dismiss();
                }
            }
        }
    }  
    private static int ArenaGameSession_AddRevivingPlayers(On.ArenaGameSession.orig_PlayersStillActive orig, ArenaGameSession self, bool addToAliveTime, bool dontCountSandboxLosers)
    {
        return orig(self, addToAliveTime, dontCountSandboxLosers) + ArenaLives.AdditionalPlayerInArenaCount(self);
    }
}