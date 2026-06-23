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
using BeyondTheWest.MeadowCompat.Data;

namespace BeyondTheWest.ArenaAddition;
public class ArenaLives : UpdatableAndDeletable, IDrawable
{
    public static Dictionary<int, ArenaLives> arenaLivesList = new();
    public static bool TryGetLives(AbstractCreature abstractPlayer, out ArenaLives lives)
    {
        return arenaLivesList.TryGetValue(BTWFunc.GetPlayerArenaNumber(abstractPlayer), out lives);
    }
    public static bool TryGetLives(int playerID, out ArenaLives lives)
    {
        return arenaLivesList.TryGetValue(playerID, out lives);
    }
    public static ArenaLives GetLives(int playerID)
    {
        TryGetLives(playerID, out ArenaLives lives);
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
            && ArenaLives.TryGetLives(BTWFunc.GetPlayerArenaNumber(abstractPlayer), out var arenaLives) 
            && arenaLives.blockArenaOut
            && arenaLives.lifesleft > 0
            && !(BTWPlugin.meadowEnabled && !MeadowFunc.IsOwnerInSession(abstractPlayer));
    }
    public static int AdditionalPlayerInArenaCount(ArenaGameSession arenaGame)
    {
        return arenaLivesList.Count(x => x.Value.Reviving && x.Value.blockArenaOut);
    }
    public ArenaLives(int playerID, int lifes, int reviveTime, int reviveAdditionnalTime, bool blockArenaOut, bool fake = false) 
    {
        this.reviveTime = reviveTime;
        this.reviveAdditionnalTime = reviveAdditionnalTime;
        this.lifes = lifes;
        this.lifesleft = lifes;
        this.fake = fake;
        this.blockArenaOut = blockArenaOut;
        this.playerID = playerID;

        if (arenaLivesList.ContainsKey(playerID)) 
        { 
            arenaLivesList[playerID].Destroy(); 
            arenaLivesList.Remove(playerID); 
        }
        arenaLivesList.Add(playerID, this);

        DisplayLives(false);
    }
    public ArenaLives(int playerID, int lifes, int reviveTime, int reviveAdditionnalTime, bool fake = false) 
        : this(playerID, lifes, reviveTime, reviveAdditionnalTime, true, fake) { }
    public ArenaLives(int playerID, int lifes, bool fake = false)
        : this(playerID, lifes, BTWFunc.FrameRate * 15, BTWFunc.FrameRate * 5, fake) { }
    public ArenaLives(int playerID, bool fake = false)
        : this(playerID, 3, fake) { }
    
    private void RevivePlayer() // from ArenaGameSession.SpawnPlayer
    {
        if (this.room.game.GetArenaGameSession is ArenaGameSession session)
        {
            AbstractCreature abstractCreature = new(
                this.room.game.world, 
                StaticWorld.GetCreatureTemplate("Slugcat"),
                null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, this.playerID));
            
            if (this.playerID == 0)
            {
                this.room.game.cameras[0].followAbstractCreature = abstractCreature;
            }
            if (session.chMeta != null)
            {
                abstractCreature.state = new PlayerState(
                    abstractCreature, this.playerID, 
                    session.characterStats_Mplayer[0].name, 
                    false);
            }
            else
            {
                abstractCreature.state = new PlayerState(
                    abstractCreature, this.playerID, 
                    new SlugcatStats.Name(ExtEnum<SlugcatStats.Name>.values.GetEntry(this.playerID), 
                    false), false);
            }
            abstractCreature.Realize();

            ShortcutHandler.ShortCutVessel shortCutVessel = new(new IntVector2(-1, -1), abstractCreature.realizedCreature, session.game.world.GetAbstractRoom(0), 0)
            {
                entranceNode = this.respawnExit,
                room = session.game.world.GetAbstractRoom(0)
            };
            abstractCreature.pos.room = session.game.world.offScreenDen.index;
            session.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            session.AddPlayer(abstractCreature);

            if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
            {
                session.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, this.playerID, -0.75f);
                session.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, this.playerID, 0.5f);
            }
            if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
            {
                session.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, this.playerID, 0.75f);
                session.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, this.playerID, 0.3f);
            }
            if (ModManager.MSC && (abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                session.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, this.playerID, -0.5f);
                session.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, this.playerID, -1f);
            }

            this.abstractTarget = abstractCreature;
        }
    }
    private void DestroyPlayer()
    {
        this.abstractTarget?.realizedCreature?.Destroy();
        this.abstractTarget?.Destroy();
        this.abstractTarget = null;
        BTWPlugin.Log($"Destroyed player <{this.playerID}>, if it existed...");
    }
    private void TriggerRevive() 
    {
        BTWPlugin.Log($"Let's try reviving player <{this.playerID}> ! Creature : [{this.abstractTarget}], Room : [{this.room}], Meadow : <{this.IsMeadowLobby}>");
        if (!this.fake && this.abstractTarget is not null)
        {
            BTWPlugin.Log($"Hold up, player was still existing, destroying...");
            DestroyPlayer();
            this.reviveCounter = 2;
        }
        else if (!this.fake && this.abstractTarget is null && this.room.game.GetArenaGameSession is ArenaGameSession session)
        {
            if (BTWPlugin.meadowEnabled && MeadowFunc.IsMeadowArena())
            {
                if (OnlineArenaLives.CanBeRevived(this))
                {
                    MeadowFunc.ReviveOnlinePlayer(session, this.respawnExit);
                    this.abstractTarget = BTWFunc.GetPlayerFromArenaNumber(this.playerID, this.room.game.GetArenaGameSession);
                }
            }
            else
            {
                RevivePlayer();
            }

            if (this.abstractTarget is AbstractCreature abstractPlayer)
            {
                this.countedAlive = true;
                this.reinforced = false;
                this.reviveCounter = 0;
                abstractPlayer.state.alive = true;
                BTWPlugin.Log($"Player [{abstractPlayer}] fully revived !");
                DisplayLives(false);

                ArenaShield shield = new(abstractPlayer.realizedCreature as Player, this.shieldTime);
                this.room.AddObject(shield);

                if (BTWPlugin.meadowEnabled)
                {
                    MeadowFunc.ResetDeathMessage(abstractPlayer);
                    MeadowFunc.ResetSlugcatIcon(abstractPlayer);
                    BTWPlugin.Log($"Player [{abstractPlayer}] icon and death message was reset !");
                }
            }
            else
            {
                BTWPlugin.LogError($"Could not revive player <{this.playerID}> ?!");
                this.reviveCounter += 10;
            }
        }
        
    }
    private void InitRevive()
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
        BTWPlugin.Log($"Life of player <{this.playerID}> was dismissed !");
    }

    public override void Destroy()
    {
        if (this.meadowInit && BTWPlugin.meadowEnabled && !this.fake)
        {
            MeadowCalls.BTWArena_ArenaLivesLeaveRoom(this);
        }
        if (arenaLivesList.ContainsKey(playerID)) { arenaLivesList.Remove(playerID); }
        BTWPlugin.Log($"Life of player <{this.playerID}> was destroyed !");
        base.Destroy();
    }
    public override void Update(bool eu)
    {
        base.Update(eu);

        if (BTWPlugin.meadowEnabled && !this.meadowInit && !this.fake)
        {
            MeadowCalls.BTWArena_ArenaLivesEnterRoom(this);
        }
        
        if (this.livesDisplayCounter > 0) { this.livesDisplayCounter--; }

        if (this.room?.game != null && this.room.game.GetArenaGameSession is ArenaGameSession arena)
        {
            if (CompetitiveAddition.ReachedMomentWhenLivesAreSetTo0(arena) && this.lifesleft != 0)
            {
                Dismiss();
            }
        }
        
        if (!this.fake)
        {
            if (this.abstractTarget is not null && this.abstractTarget.slatedForDeletion)
            {
                this.abstractTarget = null;
            }
            if (this.abstractTarget is null && this.reviveCounter <= 0)
            {
                this.abstractTarget = BTWFunc.GetPlayerFromArenaNumber(this.playerID, this.room.game.GetArenaGameSession);
            }
            bool alive = this.abstractTarget is not null && this.abstractTarget.state.alive;
            bool creatureValid = this.CreatureStillValid;
            // BTWPlugin.Log($"player <{this.playerID}>{this.abstractTarget} is alive ? <{alive}>. Considered alive ? <{this.countedAlive}>. Revive Timer ? <{this.reviveCounter}>. Lives ? <{this.lifesleft}>");
            if (this.countedAlive)
            {
                if (!alive)
                {
                    this.countedAlive = false;
                    this.killChain = 0;
                    InitRevive();
                    BTWPlugin.Log($"Oh no ! Player <{this.playerID}> died ! We must revive them, they have {this.lifesleft} lives left (counter at {this.reviveCounter}).");
                }
            }
            else
            {
                if (alive)
                {
                    BTWPlugin.Log($"Seems like Player <{this.playerID}> revived without us noticing...");
                    this.reinforced = false;
                    this.reviveCounter = 0;
                    if (this.lifesleft <= 0)
                    {
                        if (!this.fake && this.enforceAfterReachingZero && creatureValid)
                        {
                            BTWPlugin.Log("This player is NOT reviving like that lo-");
                            this.room.AddObject( new ArenaForcedDeath(this.abstractTarget) );
                        }
                    }
                    else
                    {
                        TriggerRevive(); // we trigger the revive anyway
                    }
                }
                else if (this.lifesleft > 0)
                {
                    this.reviveCounter--;
                    // BTWPlugin.Log($"player <{this.playerID}> is reviving, counter at {this.reviveCounter}...");
                    if (this.reviveCounter == bodyDestructionCount) { this.DestroyPlayer(); } // Destroying early to avoid troubles
                    if (this.reviveCounter <= 0) { TriggerRevive(); }
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

        Vector2 position = this.respawnPos;
        if (this.target is Player pl)
        {
            this.baseColor = pl.ShortCutColor();
        }

        if (this.karmaSymbolNeedToChange) 
        { 
            SetCircleCountAccordingToReviveTime();
            SetKarmaAccordingToLives(sLeaser); 
        }

        // float easedRevive = BTWFunc.EaseOut(this.FractionRevive, 4);
        float easedDisplay = BTWFunc.EaseIn(this.FractionDisplay, 2);

        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.x = position.x - camPos.x;
            sprite.y = position.y - camPos.y;
            sprite.alpha = 0f;
        }

        if (this.CreatureStillValid && this.livesDisplayCounter > 0)
        {
            Vector2 playerPos = this.CreatureMainChunk.pos;
            float height = 60f;
            if (this.target is Player player && ArenaShield.TryGetShield(player, out var shield) && shield.Shielding)
                { height = 85f; }
            if (this.target is Player && this.IsMeadowLobby)
                { height = 120f; }

            for (int i = 0; i < 3; i++)
            {
                sLeaser.sprites[i].x = playerPos.x - camPos.x;
                sLeaser.sprites[i].y = playerPos.y - camPos.y + height;
            }

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
                sLeaser.sprites[i + 3].x = position.x - camPos.x + Mathf.Sin(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 30f;
                sLeaser.sprites[i + 3].y = position.y - camPos.y + Mathf.Cos(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 30f;
                sLeaser.sprites[i + 3].color = Color.white;
                sLeaser.sprites[i + 3].alpha = 0.35f + 0.65f * (1 - GetCircleFraction(i));
                sLeaser.sprites[i + 3].scale = 0.5f * BTWFunc.EaseOut(GetCircleFraction(i), 3);
                if (this.fake)
                {
                    sLeaser.sprites[i + 3].color = Color.Lerp(sLeaser.sprites[i + 3].color, Color.black, 0.5f);
                    sLeaser.sprites[i + 3].x = position.x - camPos.x + Mathf.Sin(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 20f;
                    sLeaser.sprites[i + 3].y = position.y - camPos.y + Mathf.Cos(((float)i / this.circlesAmount) * Mathf.PI * 2f) * 20f;
                }
            }

            sLeaser.sprites[circlesAmountMax + 3].x = position.x - camPos.x;
            sLeaser.sprites[circlesAmountMax + 3].y = position.y - camPos.y;
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
    public int playerID {get; private set;}
    public Color baseColor = Color.white;
    public static FShader destroyPlayerShader;
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
    public const int bodyDestructionCount = 20;
    public const int bodyDestructionTime = BTWFunc.FrameRate * 3;
    public int killChain = 0;
    public bool fake = false;
    public bool reinforced = false;
    public bool blockArenaOut = true;
    public bool enforceAfterReachingZero = true;
    public bool countedAlive = true;
    public bool karmaSymbolNeedToChange = false;
    public bool IsMeadowLobby = false;
    public bool meadowInit = false;
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
    public bool Reviving => !this.countedAlive && this.lifesleft > 0;
}

public static class ArenaLivesHooks
{
    public static void ApplyHooks()
    {
        On.RainCycle.ArenaEndSessionRain += RainCycle_SuddenDeath;
        On.KarmaFlower.BitByPlayer += KarmaFlower_AddLifeToPlayer;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites_MakePlayerDisappearWhenRevived;

        BTWPlugin.Log("CompetitiveAddition ApplyHooks Done !");
    }
    public static void LoadResources(RainWorld rainWorld)
    {
        ArenaLives.destroyPlayerShader = rainWorld.Shaders["GateHologram"];

        BTWPlugin.Log("ArenaShieldHooks LoadResources Done !");
    }
    public static void ApplyPostHooks()
    {
        On.ArenaGameSession.PlayersStillActive += ArenaGameSession_AddRevivingPlayers;
        On.ArenaBehaviors.ExitManager.PlayerTryingToEnterDen += ArenaGameSession_RemoveLifeFromPlayerInDen;
        BTWPlugin.Log("CompetitiveAddition ApplyPostHooks Done !");
    }

    private static void PlayerGraphics_DrawSprites_MakePlayerDisappearWhenRevived(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (ArenaLives.TryGetLives(self.player.abstractCreature, out var arenaLives) && arenaLives.Reviving && !self.player.abstractCreature.state.alive)
        {
            for (int i = 0; i <= 9; i++)
            {
                if (arenaLives.reviveCounter <= ArenaLives.bodyDestructionTime + ArenaLives.bodyDestructionCount)
                {
                    if (sLeaser.sprites[i].shader != ArenaLives.destroyPlayerShader)
                    {
                        sLeaser.sprites[i].shader = ArenaLives.destroyPlayerShader;
                    }
                    sLeaser.sprites[i].alpha = 0.25f + 0.75f * BTWFunc.EaseIn(Mathf.Clamp01(
                        (arenaLives.reviveCounter - ArenaLives.bodyDestructionCount)/((float)ArenaLives.bodyDestructionTime)
                    ), 3);
                }
            }
        }
    }
    private static bool ArenaGameSession_RemoveLifeFromPlayerInDen(On.ArenaBehaviors.ExitManager.orig_PlayerTryingToEnterDen orig, ArenaBehaviors.ExitManager self, ShortcutHandler.ShortCutVessel shortcutVessel)
    {
        if (orig(self, shortcutVessel) && shortcutVessel?.creature is Player player)
        {
            if (ArenaLives.TryGetLives(BTWFunc.GetPlayerArenaNumber(player), out var arenaLives))
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
        if (ArenaLives.TryGetLives(BTWFunc.GetPlayerArenaNumber(abstractPlayer), out var arenaLives))
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
            && ArenaLives.TryGetLives(BTWFunc.GetPlayerArenaNumber(player), out var lives))
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
    private static void RainCycle_SuddenDeath(On.RainCycle.orig_ArenaEndSessionRain orig, RainCycle self)
    {
        orig(self);
        if (self?.world?.game?.GetArenaGameSession is ArenaGameSession arena && arena != null && arena.room != null)
        {
            foreach (ArenaLives arenaLives in ArenaLives.arenaLivesList.Values)
            {
                arenaLives.Dismiss();
            }
        }
    }  
    private static int ArenaGameSession_AddRevivingPlayers(On.ArenaGameSession.orig_PlayersStillActive orig, ArenaGameSession self, bool addToAliveTime, bool dontCountSandboxLosers)
    {
        return orig(self, addToAliveTime, dontCountSandboxLosers) + ArenaLives.AdditionalPlayerInArenaCount(self);
    }
}