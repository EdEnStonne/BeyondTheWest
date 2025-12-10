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

public class CompetitiveAddition
{
    // Objects
    public static ConditionalWeakTable<Player, ArenaShield> arenaShields = new();
    public static ConditionalWeakTable<AbstractCreature, ArenaLives> arenaLivesList = new();
    public class ArenaForcedDeath : UpdatableAndDeletable, IDrawable
    {
        public ArenaForcedDeath(AbstractCreature abstractCreature, int killingTime, bool stunlockCreature, Creature killTagHolder, bool fake = false) 
        {
            this.stunlockCreature = stunlockCreature;
            this.killTagHolder = killTagHolder;
            this.killingTime = killingTime;
            this.abstractTarget = abstractCreature;
            this.fake = fake;
            if (this.target != null)
            {
                if (this.target is Player player)
                {
                    this.baseColor = player.ShortCutColor();
                }
                if (this.CreatureMainChunk != null)
                {
                    this.pos = this.CreatureMainChunk.pos;
                }
            }
            if (!fake && Plugin.meadowEnabled && MeadowCompat.IsMine(abstractCreature) && MeadowCompat.IsMeadowLobby())
            {
                MeadowCompat.BTWArena_RPCArenaForcedDeathEffect(this);
            }
        }
        public ArenaForcedDeath(AbstractCreature abstractCreature, int killingTime, bool fake = false) 
            : this(abstractCreature, killingTime, true, null, fake) { }
        public ArenaForcedDeath(AbstractCreature abstractCreature, bool fake = false)
            : this(abstractCreature, BTWFunc.FrameRate * 1, true, null, fake) { }
        
        

        public void CancelKill()
        {
            this.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, CreatureMainChunk, false, 0.35f, 0.35f + BTWFunc.random * 0.35f);
        }
        public void TriggerKillScene()
        {
            if (CreatureStillValid)
            {
                Creature creature = this.target;
                BodyChunk body = CreatureMainChunk;
                this.room.PlaySound(SoundID.Firecracker_Bang, body, false, 0.5f, 0.75f + BTWFunc.random);
                this.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, body, false, 0.5f, 0.4f + BTWFunc.random * 0.35f);
                for (int i = (int)(BTWFunc.random * 10) + 10; i > 0; i--)
                {
                    this.room.AddObject( new WaterDrip(body.pos, BTWFunc.RandomCircleVector(20f), false) );
                }
                if (!fake)
                {
                    if (DoesKnockback)
                    {
                        BTWFunc.CustomKnockback(creature, BTWFunc.RandomCircleVector(20f));
                    }
                    if (this.killTagHolder != null) { creature.SetKillTag(this.killTagHolder.abstractCreature); }
                    if (Plugin.meadowEnabled)
                    {
                        MeadowCompat.SetDeathTrackerOfCreature(creature, 40);
                    }
                    creature.Die();
                }

                this.jobDone = true;
            }
        }
        public void StunCreature()
        {
            if (CreatureStillValid && !fake)
            {
                Creature creature = this.target;
                creature.LoseAllGrasps();
                creature.Stun(20);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.abstractTarget == null) { this.Destroy(); return; }
            if (CreatureStillValid && !this.jobDone)
            {
                this.destruction = 0;
                this.pos = this.CreatureMainChunk.pos;
                if (this.room != this.target.room)
                {
                    this.RemoveFromRoom();
                    this.target.room.AddObject( this );
                }
                if (this.FractionLife >= 1)
                {
                    this.TriggerKillScene();
                }
                if (this.stunlockCreature)
                {
                    StunCreature();
                }
                this.life++;
            }
            else
            {
                if (this.destruction == 0 && !this.jobDone)
                {
                    CancelKill();
                }
                if (this.FractionDestruct >= 1)
                {
                    this.Destroy();
                    return;
                }
                this.destruction++;
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];

            FSprite karmaSprite = new FSprite(KarmaMeter.KarmaSymbolSprite(false, new IntVector2(9, 9)), true);
		    karmaSprite.color = this.baseColor;
            karmaSprite.alpha = 0f;
            sLeaser.sprites[1] = karmaSprite;

            sLeaser.sprites[0] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["FlatLight"],
                alpha = 0f,
                color = this.baseColor
            };

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

            if (this.CreatureMainChunk != null && !this.jobDone)
            {
                this.pos = this.CreatureMainChunk.pos;
            }

            float easedLife = BTWFunc.EaseOut(1 - this.FractionLife, 4);
            float easedDesc = BTWFunc.EaseIn(1 - this.FractionDestruct, 2);
            Vector2 shakeFactor = this.destruction > 0 ? Vector2.zero : BTWFunc.RandomCircleVector(2.5f) * this.FractionLife;

            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.x = pos.x - camPos.x + shakeFactor.x;
                sprite.y = pos.y - camPos.y + shakeFactor.y;
                sprite.alpha = 0f;
            }

            sLeaser.sprites[1].scale = (0.5f + easedLife * 1f) * easedDesc;
            sLeaser.sprites[1].alpha = easedDesc;
            sLeaser.sprites[1].color = Color.Lerp(this.baseColor, Color.red, this.FractionLife);
            
            sLeaser.sprites[0].scale = easedLife * 15f;
            sLeaser.sprites[0].alpha = this.FractionLife;
            if (this.destruction > 0)
            {
                sLeaser.sprites[1].alpha /= 2;
                sLeaser.sprites[0].alpha = easedDesc;
            }
            if (this.target != null && this.target.inShortcut)
            {
                foreach (FSprite sprite in sLeaser.sprites)
                {
                    sprite.alpha = 0f;
                }
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
        public Creature killTagHolder;
        public Color baseColor = Color.white;
        public int life = 0;
        public int killingTime = BTWFunc.FrameRate * 1;
        public int destruction = 0;
        private const int destructTime = BTWFunc.FrameRate * 3;
        public bool stunlockCreature = true;
        private bool jobDone = false;
        public bool fake = false;
        public Vector2 pos;

        public Creature target
        {
            get
            {
                return abstractTarget?.realizedCreature;
            }
        }
        public float FractionLife
        {
            get
            {
                return Mathf.Clamp01((float)life / killingTime);
            }
        }
        public float FractionDestruct
        {
            get
            {
                return Mathf.Clamp01((float)destruction / destructTime);
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
                return this.room != null && this.target != null && this.target.room != null && !this.target.dead;
            }
        }
        public bool DoesKnockback
        {
            get
            {
                if (this.room != null && this.target != null && this.target.room != null)
                {
                    if (this.target.grabbedBy != null && this.target.grabbedBy.Count > 0)
                    {
                        return false;
                    }
                    if (this.target is Player player && player.onBack != null)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
    public class ArenaLives : UpdatableAndDeletable, IDrawable
    {
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
            if (arenaLivesList.TryGetValue(abstractCreature, out var arenaLives))
            {
                arenaLives.Destroy();
            }
            arenaLivesList.Add(abstractCreature, this);
            if (Plugin.meadowEnabled)
            {
                MeadowCompat.BTWArena_ArenaLivesInit(this);
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
                    MeadowCompat.ResetDeathMessage(this.abstractTarget);
                    MeadowCompat.ResetIconOnRevival(this.abstractTarget);
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
                    MeadowCompat.BTWArena_RPCArenaLivesDestroy(this);
                }
                if (arenaLivesList.TryGetValue(this.abstractTarget, out _))
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
                if (ReachedMomentWhenLivesAreSetTo0(arena) && this.lifesleft != 0)
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
                if (this.target is Player player && arenaShields.TryGetValue(player, out var shield) && shield.Shielding)
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
    public class ArenaShield : UpdatableAndDeletable, IDrawable
    {
        public ArenaShield(Player player, int shieldTime)
        {
            this.shieldTime = shieldTime;
            this.target = player;
            this.baseColor = player.ShortCutColor();
            if (this.CreatureMainChunk != null)
            {
                this.pos = this.CreatureMainChunk.pos;
            }
            if (arenaShields.TryGetValue(player, out var arenaShield))
            {
                arenaShield.Destroy();
            }
            arenaShields.Add(player, this);
            if (Plugin.meadowEnabled && MeadowCompat.IsMeadowLobby())
            {
                this.isMine = MeadowCompat.IsMine(player.abstractCreature);
                this.meadowSync = true;
                if (this.isMine)
                {
                    MeadowCompat.BTWArena_RPCArenaForcefieldAdded(this);
                }
            }
        }
        public ArenaShield(Player player) : this(player, BTWFunc.FrameRate * 10) { }

        public void Block(bool callForSync = true)
        {
            if (this.CreatureMainChunk != null && this.room != null && this.blockAnim <= 0)
            {
                this.blockAnim = blockAnimMax;
                this.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, CreatureMainChunk, false, 0.65f, 2.5f + BTWFunc.random * 0.5f);
                this.life += this.shieldTime/8; 
            }
            if (Plugin.meadowEnabled && callForSync && this.meadowSync)
            {
                MeadowCompat.BTWArena_RPCArenaForcefieldBlock(this);
            }
        }
        public void Dismiss(bool callForSync = true)
        {
            if (this.CreatureMainChunk != null && this.room != null)
            {
                this.room.PlaySound(SoundID.HUD_Pause_Game, this.CreatureMainChunk, false, 0.75f, 0.4f + BTWFunc.random * 0.2f);
                this.life = this.shieldTime;
            }
            if (Plugin.meadowEnabled && callForSync && this.meadowSync && this.isMine)
            {
                MeadowCompat.BTWArena_RPCArenaForcefieldDismiss(this);
            }
        }
        public override void Destroy()
        {
            base.Destroy();
            if (this.target != null)
            {
                Dismiss();
                if (arenaShields.TryGetValue(this.target, out _))
                {
                    arenaShields.Remove(this.target);
                }
            }
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.target == null) { this.Destroy(); return; }
            if (this.blockAnim > 0) { this.blockAnim--; }
            if (CreatureStillValid && this.Shielding)
            {
                if (this.CreatureMainChunk != null)
                {
                    this.pos = this.CreatureMainChunk.pos;
                }
                if (this.target.room != null && this.room != this.target.room)
                {
                    this.RemoveFromRoom();
                    this.target.room.AddObject( this );
                }
                if (this.FractionLife == 0)
                {
                    Block();
                    this.life = 0;
                }
                this.life++;
            }
            else
            {
                if (this.destruction == 0)
                {
                    Dismiss();
                }
                if (this.FractionDestruct >= 1)
                {
                    this.Destroy();
                    return;
                }
                this.destruction++;
            }
        }

        public float GetCircleFraction(int circle)
        {
            float timePerCircle = (float)this.shieldTime / this.circlesAmount;
            return 1 - Mathf.Clamp01((this.life - timePerCircle * (circle - 1)) / timePerCircle);
        }
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            this.circlesAmount = Mathf.Clamp(this.shieldTime / BTWFunc.FrameRate, 6, 20);
            sLeaser.sprites = new FSprite[this.circlesAmount + 1];

            FSprite Shield = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
                color = this.baseColor,
                alpha = 0f,
                scale = 5f
            };
            sLeaser.sprites[0] = Shield;

            for (int i = 1; i <= this.circlesAmount; i++)
            {
                sLeaser.sprites[i] = new FSprite("Futile_White", true)
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
            if (this.target == null) { sLeaser.CleanSpritesAndRemove(); return; }

            if (this.CreatureMainChunk != null)
            {
                this.pos = this.CreatureMainChunk.pos;
            }

            float easedLife = BTWFunc.EaseOut(1 - this.FractionLife, 4);
            float easedDesc = BTWFunc.EaseIn(1 - this.FractionDestruct, 2);

            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.x = pos.x - camPos.x;
                sprite.y = pos.y - camPos.y;
                sprite.alpha = 0f;
            }

            sLeaser.sprites[0].scale = 6.5f - BTWFunc.EaseOut(this.FractionBlock) * 0.5f;
            sLeaser.sprites[0].alpha = 0.05f + BTWFunc.EaseOut(this.FractionBlock) * 0.1f;

            for (int i = 1; i <= this.circlesAmount; i++)
            {
                sLeaser.sprites[i].x += Mathf.Sin(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 35f;
                sLeaser.sprites[i].y += Mathf.Cos(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 35f;
                sLeaser.sprites[i].color = Color.white;
                sLeaser.sprites[i].alpha = 0.4f + 0.6f * (1 - GetCircleFraction(i));
                sLeaser.sprites[i].scale = 0.45f * BTWFunc.EaseOut(GetCircleFraction(i), 3);
            }

            if (this.destruction > 0)
            {
                sLeaser.sprites[0].alpha = easedDesc * 0.05f;
            }
            if (this.target != null && (this.target.inShortcut || this.target.room == null))
            {
                foreach (FSprite sprite in sLeaser.sprites)
                {
                    sprite.alpha = 0f;
                }
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

        public Player target;
        public Color baseColor = Color.white;
        public int life = 0;
        private int circlesAmount = 0;
        public int shieldTime = BTWFunc.FrameRate * 10;
        public int destruction = 0;
        public int blockAnim = 0;
        private bool isMine = true;
        private bool meadowSync = false;
        private const int blockAnimMax = 30;
        private const int destructTime = BTWFunc.FrameRate * 1;
        public Vector2 pos;

        public float FractionLife
        {
            get
            {
                return Mathf.Clamp01((float)life / shieldTime);
            }
        }
        public float FractionDestruct
        {
            get
            {
                return Mathf.Clamp01((float)destruction / destructTime);
            }
        }
        public float FractionBlock
        {
            get
            {
                return Mathf.Clamp01((float)blockAnim / blockAnimMax);
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
                return this.room != null && this.target != null && !this.target.dead;
            }
        }
        public bool Shielding
        {
            get
            {
                return this.room != null && FractionLife < 1 && CreatureStillValid;
            }
        }
    }

    // Functions
    public static void ApplyHooks()
    {
        On.ArenaGameSession.SpawnItem += ItemMultCompetitive;
        On.Player.ProcessDebugInputs += Player_SlashKillDebug;
        IL.Weapon.Update += Weapon_BlockWithArenaShield;
        On.Creature.Violence += Player_BlockViolence;
        On.Creature.Die += Player_BlockLiteralDeath;
        On.Creature.Grab += Player_RemoveShieldOnGrabItem;
        On.Player.ThrowObject += Player_RemoveShieldOnThrowObject;
        On.Creature.Violence += Player_RemoveShieldOnViolence;
        On.RainCycle.ArenaEndSessionRain += RainCycle_SuddenDeath;
        On.UpdatableAndDeletable.Destroy += Creature_DontDestroyIfReviving;
        On.AbstractWorldEntity.Destroy += AbstractCreature_DontDestroyIfReviving;
        Plugin.Log("CompetitiveAddition ApplyHooks Done !");
    }


    

    public static void ApplyPostHooks()
    {
        On.ArenaGameSession.PlayersStillActive += ArenaGameSession_AddRevivingPlayers;
        Plugin.Log("CompetitiveAddition ApplyPostHooks Done !");
    }

    public static bool OutOfBounds(Creature creature)
    {
        float num6 = -creature.bodyChunks[0].restrictInRoomRange + 1f;
        if (creature is Player player 
            && creature.bodyChunks[0].restrictInRoomRange == creature.bodyChunks[0].defaultRestrictInRoomRange)
        {
            if (player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                num6 = Mathf.Max(num6, -250f);
            }
            else
            {
                num6 = Mathf.Max(num6, -500f);
            }
        }
        return creature.bodyChunks[0].pos.y < num6 
            && (!creature.room.water 
                || creature.room.waterInverted 
                || creature.room.defaultWaterLevel < -10) 
            && (!creature.Template.canFly 
                || creature.Stunned 
                || creature.dead) 
            && (creature is Player 
                || !creature.room.game.IsArenaSession 
                || creature.room.game.GetArenaGameSession.chMeta == null 
                || !creature.room.game.GetArenaGameSession.chMeta.oobProtect);
    }
    public static bool DoesBlock(Weapon weapon, SharedPhysics.CollisionResult result)
    {
        if (weapon != null && result.obj != null 
            && result.obj is Player player && player != null
            && arenaShields.TryGetValue(player, out var shield) 
            && shield.Shielding)
        {
            Plugin.Log("["+ weapon +"] BLOCKED BY SHIELD OF PLAYER ["+ player +"]");

            shield.Block();
            Vector2 inbetweenPos = Vector2.Lerp(result.obj.firstChunk.lastPos, weapon.firstChunk.lastPos, 0.5f);
            Vector2 dir = (weapon.firstChunk.lastPos - result.obj.firstChunk.lastPos).normalized * 2f + BTWFunc.RandomCircleVector() + Vector2.up * 0.5f;
            
            weapon.WeaponDeflect(inbetweenPos, dir.normalized, 50f);
            return true;
        }
        return false;
    }
    public static bool MeadowCheckIfShouldMultiplyItems()
    {
        if (Plugin.meadowEnabled)
        {
            return !MeadowCompat.IsMeadowLobby();
        }
        return true;
    }
    public static bool ReachedMomentWhenLivesAreSetTo0(ArenaGameSession arenaGame)
    {
        if (arenaGame == null) { return true; }
        if (!arenaGame.SessionStillGoing 
            || (arenaGame.game?.world?.rainCycle != null && arenaGame.game.world.rainCycle.TimeUntilRain <= 2000))
        {
            return true;
        }
        return false;
    }
    public static void LogLivesState(AbstractCreature abstractPlayer)
    {
        if (abstractPlayer == null) { return; } 
        Plugin.Log($"Logging lives state of {abstractPlayer} :");
        Plugin.Log($"{abstractPlayer} : "
            + $"\nalive <{abstractPlayer.state.alive }>\n"
            + $"meadowAlive <{Plugin.meadowEnabled && MeadowCompat.IsPlayerAlive(abstractPlayer)}>"
            + $"\nin danger <{abstractPlayer.realizedCreature != null && (abstractPlayer.realizedCreature as Player).dangerGrasp != null}>"
            + $"\nshould count even if dead <" +
                (!abstractPlayer.state.alive 
                || (Plugin.meadowEnabled && !MeadowCompat.IsPlayerAlive(abstractPlayer))
                || (
                    abstractPlayer.realizedCreature != null 
                    && (abstractPlayer.realizedCreature as Player).dangerGrasp != null
                    )
                )+">");
        if (arenaLivesList.TryGetValue(abstractPlayer, out var arenaLives))
        {
            Plugin.Log($"Lives state found ! \n"
                + $"Lives <{arenaLives.lifes}>\n"
                + $"Lives Left <{arenaLives.lifesleft}>\n"
                + $"Block Out Arena <{arenaLives.blockArenaOut}>\n"
                + $"Strict <{arenaLives.enforceAfterReachingZero}>\n"
                + $"Reviving block <{PlayerCountedAsAliveInArena(abstractPlayer)}>\n");
        }
        else
        {
            Plugin.Log("No lives states !");
        }
    }
    public static bool PlayerCountedAsAliveInArena(AbstractCreature abstractPlayer)
    {
        return abstractPlayer != null 
            && (
                (abstractPlayer.state != null && !abstractPlayer.state.alive) 
                || (Plugin.meadowEnabled && !MeadowCompat.IsPlayerAlive(abstractPlayer))
                || (
                    abstractPlayer.realizedCreature != null 
                    && (abstractPlayer.realizedCreature as Player).dangerGrasp != null
                    )
                )
            && arenaLivesList.TryGetValue(abstractPlayer, out var arenaLives) 
            && arenaLives.blockArenaOut
            && arenaLives.lifesleft > 0;
    }
    public static int AdditionalPlayerInArenaCount(ArenaGameSession arenaGame)
    {
        int revivingPlayers = 0;
        if (!ReachedMomentWhenLivesAreSetTo0(arenaGame) && arenaGame != null && arenaGame.Players != null)
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

    // Hooks  
    private static void Creature_DontDestroyIfReviving(On.UpdatableAndDeletable.orig_Destroy orig, UpdatableAndDeletable self)
    {
        if (self.room != null
            && self is Creature creature
            && creature != null
            && creature.abstractCreature != null
            && arenaLivesList.TryGetValue(creature.abstractCreature, out var lives)
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
            && arenaLivesList.TryGetValue(abstractCreature, out var lives))
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
                if (arenaLivesList.TryGetValue(abstractCreature, out var arenaLives))
                {
                    arenaLives.Dismiss();
                }
            }
        }
    }  
    private static int ArenaGameSession_AddRevivingPlayers(On.ArenaGameSession.orig_PlayersStillActive orig, ArenaGameSession self, bool addToAliveTime, bool dontCountSandboxLosers)
    {
        return orig(self, addToAliveTime, dontCountSandboxLosers) + AdditionalPlayerInArenaCount(self);
    }
    private static void Player_RemoveShieldOnThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        if (arenaShields.TryGetValue(self, out var shield) 
            && shield.Shielding)
        {
            Plugin.Log("REMOVED SHIELD OF PLAYER ["+ self +"]. Reason : item throw.");
            shield.Dismiss();
        }
        orig(self, grasp, eu);
    }
    private static bool Player_RemoveShieldOnGrabItem(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
    {
        if (self is Player player && player != null
            && arenaShields.TryGetValue(player, out var shield) 
            && shield.Shielding)
        {
            Plugin.Log("REMOVED SHIELD OF PLAYER ["+ player +"]. Reason : item grab.");
            shield.Dismiss();
        }
        return orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
    }
    private static void Player_BlockLiteralDeath(On.Creature.orig_Die orig, Creature self)
    {
        if (self is Player player && player != null
            && arenaShields.TryGetValue(player, out var shield) 
            && shield.Shielding)
        {
            if (!OutOfBounds(self))
            {
                Plugin.Log("DEATH BLOCKED BY SHIELD OF PLAYER ["+ player +"]");

                shield.Block();
                player.Stun(BTWFunc.FrameRate * 1);
                if (player.State is HealthState)
                {
                    (player.State as HealthState).health = 1f;
                }
                if (player.airInLungs < 1f)
                {
                    player.airInLungs = 1f;
                }
                if (player.Hypothermia > 0f)
                {
                    player.Hypothermia = 0f;
                }
                if (player.grabbedBy != null && player.grabbedBy.Count > 0)
                {
                    List<Creature.Grasp> grasps = new(player.grabbedBy);
                    foreach (Creature.Grasp grasp in grasps)
                    {
                        grasp.grabber.Stun(BTWFunc.FrameRate * 3);
                        grasp.Release();
                    }
                }
                if (player.injectedPoison > 0f)
                {
                    player.injectedPoison = 0f;
                }
                return;
            }
            shield.Dismiss();
        }
        orig(self);
    }
    private static void Player_SlashKillDebug(On.Player.orig_ProcessDebugInputs orig, Player self)
    {
        orig(self);
        bool targetLocal = !Plugin.meadowEnabled || MeadowCompat.IsMine(self.abstractPhysicalObject);
        if (self.room == null || !self.room.game.devToolsActive || !targetLocal)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (Input.GetKey(KeyCode.LeftShift) && self.room.world.game.IsArenaSession)
            {
                ArenaShield arenaShield = new(self);
                self.room.AddObject( arenaShield );
            }
            else if (Input.GetKey(KeyCode.LeftControl) && self.room.world.game.IsArenaSession)
            {
                ArenaLives arenaLives = new(self.abstractCreature);
                self.room.AddObject( arenaLives );
            }
            else
            {
                ArenaForcedDeath arenaForcedDeath = new(self.abstractCreature);
                self.room.AddObject( arenaForcedDeath );
            }
        }
    }
    private static void ItemMultCompetitive(On.ArenaGameSession.orig_SpawnItem orig, ArenaGameSession self, Room room, PlacedObject placedObj)
    {
        if (!Plugin.meadowEnabled || MeadowCheckIfShouldMultiplyItems())
        {
            float loop = //(BeyondTheWestRemixMenu.DoItemSpawnScalePerPlayers.Value ? BeyondTheWestRemixMenu.ItemSpawnMultiplierPerPlayers.Value : 1f)
                1f * BTWRemix.ItemSpawnMultiplier.Value;

            while (loop > 0f)
            {
                if (loop < 1f)
                {
                    if (UnityEngine.Random.value > loop)
                    {
                        return;
                    }
                }
                orig(self, room, placedObj);
                loop--;
            }
        }
        else
        {
            orig(self, room, placedObj);
        }
    }
    private static void Weapon_BlockWithArenaShield(ILContext il)
    {
        Plugin.Log("Weapon PassThrough IL starts");
        try
        {
            Plugin.Log("Trying to hook IL");
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStfld<Weapon>("floorBounceFrames"),
                x => x.MatchRet()
                )
            )
            {
                cursor.MoveAfterLabels();

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_S, (byte)16);
                cursor.EmitDelegate(DoesBlock);
                Instruction Mark = cursor.Previous;

                cursor.Emit(OpCodes.Ldloca_S, (byte)16);
                cursor.Emit(OpCodes.Ldnull);
                cursor.Emit(OpCodes.Stfld, typeof(SharedPhysics.CollisionResult).GetField("obj"));
                Instruction Mark2 = cursor.Next;

                if (cursor.TryGotoPrev(MoveType.After, x => x == Mark))
                {
                    cursor.Emit(OpCodes.Brfalse_S, Mark2);
                }
                else { Plugin.logger.LogError("Couldn't find IL hook 2 :<"); }
            }
            else { Plugin.logger.LogError("Couldn't find IL hook 1 :<"); }

            Plugin.Log("IL hook ended");
        }
        catch (Exception ex)
        {
            Plugin.logger.LogError(ex);
        }
        // Plugin.Log(il);
        Plugin.Log("Weapon PassThrough IL ends");
    }
    private static void Player_BlockViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (self is Player player && player != null
            && arenaShields.TryGetValue(player, out var shield) 
            && shield.Shielding)
        {
            Plugin.Log("VIOLENCE BLOCKED BY SHIELD OF PLAYER ["+ player +"]");

            shield.Block();
            if (source?.owner != null && source.owner is Creature creature)
            {
                creature.Stun(3 * BTWFunc.FrameRate);
                Vector2 dir = (source.lastPos - (hitChunk ?? self.firstChunk).lastPos).normalized * 3f + BTWFunc.RandomCircleVector() + Vector2.up;
                
                BTWFunc.CustomKnockback(creature, dir.normalized, 30f, true);
            }
            return;
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type , damage, stunBonus);
    }
    private static void Player_RemoveShieldOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        if (source?.owner != null
            && source?.owner is Player player && player != null
            && arenaShields.TryGetValue(player, out var shield) 
            && shield.Shielding)
        {
            Plugin.Log("REMOVED SHIELD OF PLAYER ["+ player +"]. Reason : violence.");

            shield.Dismiss();
            Vector2 dir = (source.lastPos - (hitChunk ?? self.firstChunk).lastPos).normalized * 3f + BTWFunc.RandomCircleVector() + Vector2.up;
            BTWFunc.CustomKnockback(player, dir.normalized, 20f, true);
            player.Stun((int)stunBonus);
            player.gourmandAttackNegateTime = player.stun;
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type , 0f, stunBonus);
            return;
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type , damage, stunBonus);
    }
}
