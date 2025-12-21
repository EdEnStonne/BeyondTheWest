using System;
using BeyondTheWest;
using UnityEngine;
using BeyondTheWest.MeadowCompat;

namespace BeyondTheWest.ArenaAddition;
public static class CompetitiveAddition
{
    public static void ApplyHooks()
    {
        On.Player.ProcessDebugInputs += Player_ArenaDebug;
        Plugin.Log("CompetitiveAddition ApplyHooks Done !");
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
   
    private static void Player_ArenaDebug(On.Player.orig_ProcessDebugInputs orig, Player self)
    {
        orig(self);
        bool targetLocal = !Plugin.meadowEnabled || BTWFunc.IsLocal(self.abstractPhysicalObject);
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
            else if (Input.GetKey(KeyCode.LeftAlt) && self.room.world.game.IsArenaSession)
            {
                ArenaItemSpawn arenaItemSpawn = new(self.mainBodyChunk.pos, ArenaItemSpawn.GetRandomTestList());
                self.room.AddObject( arenaItemSpawn );
            }
            else
            {
                ArenaForcedDeath arenaForcedDeath = new(self.abstractCreature);
                self.room.AddObject( arenaForcedDeath );
            }
        }
    }   
}
