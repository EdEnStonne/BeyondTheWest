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
    public static bool IsPlayerRevivingInArena(AbstractCreature abstractPlayer)
    {
        return abstractPlayer != null 
            && (
                (abstractPlayer.state != null && !abstractPlayer.state.alive) 
                || (BTWPlugin.meadowEnabled && !MeadowFunc.IsPlayerAlive(abstractPlayer))
                || (
                    abstractPlayer.realizedCreature != null 
                    && (abstractPlayer.realizedCreature as Player).dangerGrasp != null
                    )
                )
            && ArenaLives.TryGetLives(abstractPlayer, out var arenaLives) 
            && arenaLives.blockArenaOut
            && arenaLives.lifesleft > 0
            && !(BTWPlugin.meadowEnabled && !MeadowFunc.IsOwnerInSession(abstractPlayer));
    }
    public static int AdditionalPlayerInArenaCount(ArenaGameSession arenaGame)
    {
        int revivingPlayers = 0;
        if (!CompetitiveAddition.ReachedMomentWhenLivesAreSetTo0(arenaGame) && arenaGame != null && arenaGame.Players != null)
        {
            try
            {
                revivingPlayers = arenaGame.Players?.Count(x => IsPlayerRevivingInArena(x)) ?? 0;
            }
            catch (Exception ex)
            {
                BTWPlugin.logger.LogError("Something wrong happened while counting players ! " + ex);
            }
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
                this.respawnPos = this.pos;
            }
        }
        if (ArenaLives.TryGetLives(abstractCreature, out var arenaLives))
        {
            arenaLives.Destroy();
        }
        arenaLivesList.Add(abstractCreature, this);
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
    
    public void ResetVariablesOnRevival()
    {
        wasAbstractCreatureDestroyed = false;
        if (this.target is Player player) 
        {
            if (player.Local())
            {
                BTWFunc.ResetCore(player);
                BTWFunc.ResetSpark(player);

                if (this.shieldTime > 0)
                {
                    player.room?.AddObject( new ArenaShield(player, this.shieldTime) );
                }
            }
            if (player.room.game.session is ArenaGameSession arenaGameSession) {
                // Thanks to Alex for pointing this out, check also his SimpleRespawn Mod for Meadow !
                arenaGameSession.arenaSitting.players[BTWFunc.GetPlayerArenaNumber(player)].alive = true; 
                arenaGameSession.arenaSitting.players[BTWFunc.GetPlayerArenaNumber(player)].deaths++;
            }

            if (BTWPlugin.meadowEnabled)
            {
                MeadowFunc.ResetDeathMessage(this.abstractTarget);
                MeadowFunc.ResetSlugcatIcon(this.abstractTarget);
            }

            foreach (ArenaForcedDeath forcedDeath in room.updateList.FindAll(x => x is ArenaForcedDeath death && death.target == player).Cast<ArenaForcedDeath>())
            {
                forcedDeath.Destroy();
            }

            if (this.target != null)
            {
                if (this.target.Local())
                {
                    foreach (var chunk in this.target.bodyChunks)
                    {
                        chunk.pos = this.respawnPos;
                    }
                    this.abstractTarget.LoseAllStuckObjects();
                }

                Spear[] stuckSpears = BTWFunc.GetAllObjects(room).FindAll(
                    x => x is Spear spear && spear.stuckInObject == this.target)
                    .Cast<Spear>().ToArray();
                for (int i = 0; i < stuckSpears.Length; i++)
                {
                    stuckSpears[i].PulledOutOfStuckObject();
                    stuckSpears[i].ChangeMode(Weapon.Mode.Free);
                }
            }

            BTWPlugin.Log($"Reset stat of [{player}] for revival !");
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
        
        if (this.target?.room is not null)
        {
            this.abstractTarget.LoseAllStuckObjects();
            Spear[] stuckSpears = BTWFunc.GetAllObjects(this.target.room).FindAll(
                x => x is Spear spear && spear.stuckInObject == this.target)
                .Cast<Spear>().ToArray();
            for (int i = 0; i < stuckSpears.Length; i++)
            {
                stuckSpears[i].PulledOutOfStuckObject();
                stuckSpears[i].ChangeMode(Weapon.Mode.Free);
            }
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
            // this.abstractTarget.realizedObject?.Destroy();
            this.abstractTarget.slatedForDeletion = false;
            this.abstractTarget.Abstractize(this.room.GetWorldCoordinate(this.respawnPos));
            if(this.abstractTarget.state is PlayerState state)
            {
                state.alive = true;
                state.permaDead = false;
                state.permanentDamageTracking = 0f;
            }
            if (!this.room.abstractRoom.creatures.Exists(x => x == this.abstractTarget))
            {
                BTWPlugin.Log($"[{this.abstractTarget}] was removed from the creature list ! Adding it back"); 
                this.room.abstractRoom.creatures.Add(this.abstractTarget);
            }

            if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
            {
                MeadowFunc.ReviveOnlinePlayer(this.room.game.session as ArenaGameSession, this.abstractTarget, this.respawnExit);
            }
            else
            {
                this.abstractTarget.RealizeInRoom();
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
            }
            if (this.target != null)
            {
                this.target.slatedForDeletetion = false;
            }
            this.countedAlive = this.target != null ? this.target.State.alive : false; 
            DisplayLives(false);
            ResetVariablesOnRevival();
        }
    }
    public void TriggerRevive() 
    {
        BTWPlugin.Log("Let's try reviving ["+ this.abstractTarget +"] !\nCounter at : <"
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
            BTWPlugin.Log("Attempted to revive ["+ creature +"].");
            
            this.countedAlive = creature.State.alive; 
            this.reinforced = false;
            DisplayLives(false);
        }
        
    }
    public void InitRevive()
    {
        if (!this.reinforced)
        {
            this.lifesleft--;
        }
        if (this.lifesleft > 0)
        {
            this.reviveCounter = this.TotalReviveTime;
            if (this.room != null)
            {
                this.respawnExit = BTWFunc.RandomExit(this.room);
                this.respawnPos = BTWFunc.ExitPos(this.room, this.respawnExit);
                this.abstractTarget?.Move(this.room.GetWorldCoordinate(this.respawnPos));
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
            this.room.PlaySound(SoundID.HUD_Exit_Game, this.respawnPos, 1f, 1.5f + BTWFunc.random * 0.5f);
        }
    }
    public void Dismiss()
    {
        if (this.lifesleft > 1)
        {
            DisplayLives(!this.fake);
        }
        this.lifesleft = 0;
        this.reinforced = false;
        this.reviveCounter = 0;
        this.karmaSymbolNeedToChange = true;
    }

    public override void Destroy()
    {
        BTWPlugin.Log($"Live destroyed for player $[{this.abstractTarget}]");
        if (this.abstractTarget != null)
        {
            if (!fake && BTWPlugin.meadowEnabled)
            {
                MeadowCalls.BTWArena_RPCArenaLivesDestroy(this);
            }
            if (ArenaLives.TryGetLives(this.abstractTarget, out _))
            {
                arenaLivesList.Remove(this.abstractTarget);
            }
            if (this.wasAbstractCreatureDestroyed)
            {
                this.abstractTarget.Destroy();
            }
        }
        base.Destroy();
    }
    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.abstractTarget == null) { this.Destroy(); return; }

        if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowLobby() && !this.meadowInit)
        {
            if (BTWPlugin.meadowEnabled)
            {
                MeadowCalls.BTWArena_ArenaLivesInit(this);
            }
        }
        
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
                    this.killChain = 0;
                    InitRevive();
                    BTWPlugin.Log("Oh no ! ["+ this.abstractTarget +"] died ! We must revive them, they have "+ this.lifesleft +" lives left.");
                }
            }
            else
            {
                if (alive)
                {
                    BTWPlugin.Log("Seems like ["+ this.abstractTarget +"] revived without us noticing...");
                    this.reinforced = false;
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
        int karma = Mathf.Clamp(this.lifesleft - (this.countedAlive || this.reinforced ? 1 : 0), 0, 9);
        Color color = this.countedAlive && this.lifesleft > 0 ? Color.white : Color.red;
        sLeaser.sprites[1].SetElementByName(KarmaMeter.KarmaSymbolSprite(false, new IntVector2(karma, 9)));
        sLeaser.sprites[1].color = color;
        sLeaser.sprites[0].color = color;
        sLeaser.sprites[2].color = color;
        this.karmaSymbolNeedToChange = false;
    }
    public void SetCircleCountAccordingToReviveTime()
    {
        this.circlesAmount = Mathf.Clamp(this.TotalReviveTime / (BTWFunc.FrameRate * 3), 6, circlesAmountMax);
    }
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        SetCircleCountAccordingToReviveTime();
        sLeaser.sprites = new FSprite[circlesAmountMax + 4];

        FSprite BubbleLives = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["VectorCircle"],
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

        FSprite ringSprite = new FSprite("smallKarmaRingReinforced", true)
        {
            color = Color.white,
            alpha = 0f
        };
        sLeaser.sprites[2] = ringSprite;

        SetKarmaAccordingToLives(sLeaser);

        for (int i = 0; i < circlesAmountMax; i++)
        {
            sLeaser.sprites[i + 3] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
                color = Color.white,
                alpha = 0f,
                scale = 1.0f
            };
        }

        FSprite Glow = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["FlatLight"],
            alpha = 0f,
            color = this.baseColor
        };
        sLeaser.sprites[circlesAmountMax + 3] = Glow;

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

        Vector2 position = this.respawnPos;
        if (this.CreatureMainChunk != null)
        {
            this.pos = this.CreatureMainChunk.pos;
            position = this.pos;
        }
        if (this.target != null && this.target is Player pl)
        {
            this.baseColor = pl.ShortCutColor();
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
            sLeaser.sprites[2].y += height;

            float scale = 1f - 0.5f * easedDisplay;
            sLeaser.sprites[0].scale = 2f * scale;
            sLeaser.sprites[1].scale = 0.5f * scale;
            sLeaser.sprites[2].scale = 0.75f * scale;
            
            sLeaser.sprites[0].alpha = 0.2f * easedDisplay * (this.reinforced ? 0 : 1);
            sLeaser.sprites[1].alpha = easedDisplay;
            sLeaser.sprites[2].alpha = easedDisplay * (this.reinforced ? 1 : 0);
        }

        if (this.reviveCounter > 0)
        {
            for (int i = 0; i < this.circlesAmount; i++)
            {
                sLeaser.sprites[i + 3].x = this.respawnPos.x - camPos.x + Mathf.Sin(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 30f;
                sLeaser.sprites[i + 3].y = this.respawnPos.y - camPos.y + Mathf.Cos(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 30f;
                sLeaser.sprites[i + 3].color = Color.white;
                sLeaser.sprites[i + 3].alpha = 0.35f + 0.65f * (1 - GetCircleFraction(i));
                sLeaser.sprites[i + 3].scale = 0.5f * BTWFunc.EaseOut(GetCircleFraction(i), 3);
                if (this.fake)
                {
                    sLeaser.sprites[i + 3].color = Color.Lerp(sLeaser.sprites[i + 3].color, Color.black, 0.5f);
                    sLeaser.sprites[i + 3].x = this.respawnPos.x - camPos.x + Mathf.Sin(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 20f;
                    sLeaser.sprites[i + 3].y = this.respawnPos.y - camPos.y + Mathf.Cos(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 20f;
                }
            }

            sLeaser.sprites[circlesAmountMax + 3].x = this.respawnPos.x - camPos.x;
            sLeaser.sprites[circlesAmountMax + 3].y = this.respawnPos.y - camPos.y;
            sLeaser.sprites[circlesAmountMax + 3].alpha = 0.25f + Mathf.Cos((1 / (BTWFunc.FrameRate * 2.5f)) * Mathf.PI * 2f * this.reviveCounter) * 0.2f;
            sLeaser.sprites[circlesAmountMax + 3].scale = 4f + Mathf.Cos((1 / (BTWFunc.FrameRate * 2.5f)) * Mathf.PI * 2f * this.reviveCounter) * 3f;
            sLeaser.sprites[circlesAmountMax + 3].color = this.baseColor;
            if (this.fake)
            {
                sLeaser.sprites[circlesAmountMax + 3].alpha = Mathf.Lerp(sLeaser.sprites[circlesAmountMax + 3].alpha, 0, 0.5f);
                sLeaser.sprites[circlesAmountMax + 3].scale /= 2;
            }
        }

        if (this.target != null && (this.target.inShortcut || this.target.room == null))
        {
            sLeaser.sprites[0].alpha = 0f;
            sLeaser.sprites[1].alpha = 0f;
            sLeaser.sprites[2].alpha = 0f;
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
    public int killChain = 0;
    public bool fake = false;
    public bool reinforced = false;
    public bool blockArenaOut = true;
    public bool enforceAfterReachingZero = true;
    public bool countedAlive = true;
    public bool karmaSymbolNeedToChange = false;
    public bool canRespawn = false;
    public bool IsMeadowLobby = false;
    public bool wasAbstractCreatureDestroyed = false;
    public bool meadowInit = false;
    // public int respawnPlayerID = -1;
    public Vector2 pos;
    public Vector2 respawnPos;
    public int respawnExit;
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
        On.KarmaFlower.BitByPlayer += KarmaFlower_AddLifeToPlayer;
        BTWPlugin.Log("CompetitiveAddition ApplyHooks Done !");
    }


    public static void ApplyPostHooks()
    {
        On.ArenaGameSession.PlayersStillActive += ArenaGameSession_AddRevivingPlayers;
        On.ArenaBehaviors.ExitManager.PlayerTryingToEnterDen += ArenaGameSession_RemoveLifeFromPlayerInDen;
        On.AbstractWorldEntity.Destroy += AbstractCreature_DontDestroyIfReviving;
        BTWPlugin.Log("CompetitiveAddition ApplyPostHooks Done !");
    }

    private static bool ArenaGameSession_RemoveLifeFromPlayerInDen(On.ArenaBehaviors.ExitManager.orig_PlayerTryingToEnterDen orig, ArenaBehaviors.ExitManager self, ShortcutHandler.ShortCutVessel shortcutVessel)
    {
        if (orig(self, shortcutVessel))
        {
            if (ArenaLives.TryGetLives(shortcutVessel?.creature?.abstractCreature, out var arenaLives))
            {
                BTWPlugin.Log($"[{shortcutVessel?.creature?.abstractCreature}] entered a den, removing its lifes");
                arenaLives.Destroy();
            }
            return true;
        }
        return false;
    }

    public static void LogLivesState(AbstractCreature abstractPlayer)
    {
        if (abstractPlayer == null) { return; } 
        BTWPlugin.Log($"Logging lives state of {abstractPlayer} :");
        BTWPlugin.Log($"{abstractPlayer} : "
            + $"\nalive <{abstractPlayer.state.alive }>\n"
            + $"meadowAlive <{BTWPlugin.meadowEnabled && MeadowFunc.IsPlayerAlive(abstractPlayer)}>"
            + $"\nin danger <{abstractPlayer.realizedCreature != null && (abstractPlayer.realizedCreature as Player).dangerGrasp != null}>"
            + $"\nshould count even if dead <" +
                (!abstractPlayer.state.alive 
                || (BTWPlugin.meadowEnabled && !MeadowFunc.IsPlayerAlive(abstractPlayer))
                || (
                    abstractPlayer.realizedCreature != null 
                    && (abstractPlayer.realizedCreature as Player).dangerGrasp != null
                    )
                )+">");
        if (ArenaLives.TryGetLives(abstractPlayer, out var arenaLives))
        {
            BTWPlugin.Log($"Lives state found ! \n"
                + $"Lives <{arenaLives.lifes}>\n"
                + $"Lives Left <{arenaLives.lifesleft}>\n"
                + $"Block Out Arena <{arenaLives.blockArenaOut}>\n"
                + $"Strict <{arenaLives.enforceAfterReachingZero}>\n"
                + $"Reviving block <{ArenaLives.IsPlayerRevivingInArena(abstractPlayer)}>\n");
        }
        else
        {
            BTWPlugin.Log("No lives states !");
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
            if (BTWPlugin.meadowEnabled)
            {
                MeadowFunc.HandleKarmaFlowerInArena(lives);
            }
            else if (!lives.reinforced)
            {
                lives.reinforced = true;
                lives.DisplayLives();
            }
        }
    }
    private static void Creature_DontDestroyIfReviving(On.UpdatableAndDeletable.orig_Destroy orig, UpdatableAndDeletable self)
    {
        if (self.room != null
            && self is Creature creature
            && creature != null
            && creature.abstractCreature != null
            && BTWFunc.IsLocal(creature.abstractCreature)
            && ArenaLives.TryGetLives(creature.abstractCreature, out var lives)
            && lives.canRespawn
            && lives.lifesleft > 0
            && !(BTWPlugin.meadowEnabled && !MeadowFunc.IsOwnerInSession(creature.abstractCreature)))
        {
            if (creature == creature.abstractCreature.realizedCreature)
            {
                creature.Die();
                creature.abstractCreature.Abstractize(self.room.GetWorldCoordinate(lives.respawnPos));
                BTWPlugin.Log($"Creature [{creature}] was destroyed while having some lives left !");
                // return;
            }
            else
            {
                BTWPlugin.Log($"Seems like [{creature}] is not the same as [{creature.abstractCreature}]'s [{creature.abstractCreature.realizedCreature}]. Destroying it.");
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
            if (lives.canRespawn && lives.lifesleft > 0 && !(BTWPlugin.meadowEnabled && !MeadowFunc.IsOwnerInSession(abstractCreature)))
            {
                abstractCreature.destroyOnAbstraction = false;
                abstractCreature.Die();
                lives.wasAbstractCreatureDestroyed = true;
                abstractCreature.Abstractize(self.Room.realizedRoom.GetWorldCoordinate(lives.respawnPos));
                abstractCreature.realizedCreature?.Destroy();
                abstractCreature.realizedCreature = null;
                BTWPlugin.Log($"Stopped Abstract Creature [{abstractCreature}] from being destroyed, so they can revive in peace (lives : <{lives.lifesleft}>).");
                return;
            }
            else
            {
                lives.Dismiss();
                BTWPlugin.Log($"Abstract Creature [{abstractCreature}] is being destroyed and cannot revive (Reason : <{lives.canRespawn}>,<{lives.lifesleft > 0}>,<{!BTWPlugin.meadowEnabled || MeadowFunc.HasOwner(abstractCreature)}>), dismissing the resting lives.");
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