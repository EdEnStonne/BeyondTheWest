using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;

namespace BeyondTheWest;
public class StaticChargeBatteryUI : UpdatableAndDeletable, IDrawable
{
    public StaticChargeBatteryUI(StaticChargeManager staticChargeManager)
    {
        this.SCM = staticChargeManager;
    }
    
    //-------------- Local Functions
    // Sprites
    private void SetBatteryBGSprite(RoomCamera.SpriteLeaser sLeaser)
    {
        if (this.SCM != null)
        {
            TriangleMesh BatteryBG = (TriangleMesh)sLeaser.sprites[1];
            if (this.SCM.overchargeImmunity > 0)
            {
                float blue = 0.2f + 0.2f * Mathf.Sin((this.SCM.overchargeImmunity%(Mathf.PI * 100)) * 2 * Mathf.PI / (BTWFunc.FrameRate * 2f));
                BatteryBG.color = new Color(0.5f - blue, 1f, 1f);
            }
            else
            {
                BatteryBG.color = new Color(1f, 1f, 1f);
            }
            sLeaser.sprites[1] = BatteryBG;
        }
    }
    private void SetBatteryChargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
    {
        float xpos = 13f * Mathf.Clamp01(pourcent) - 7f;
        TriangleMesh BatteryCharge = (TriangleMesh)sLeaser.sprites[2];
        BatteryCharge.MoveVertice(2, new Vector2(xpos, -4f));
        BatteryCharge.MoveVertice(3, new Vector2(xpos, 4f));

        BatteryCharge.color = new Color(1f, 1f, 0.25f);
        BatteryCharge.alpha = Mathf.Clamp01(pourcent * 10);
        sLeaser.sprites[2] = BatteryCharge;
    }
    private void SetBatteryOverchargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
    {
        pourcent = Mathf.Clamp01(pourcent);
        TriangleMesh BatteryOvercharge = (TriangleMesh)sLeaser.sprites[3];

        
        if (pourcent == 0f)
        {
            BatteryOvercharge.alpha = 0f;
        }
        else
        {
            float xpos = 18f * pourcent - 9.5f;
            BatteryOvercharge.MoveVertice(2, new Vector2(xpos, -5.5f));
            BatteryOvercharge.MoveVertice(3, new Vector2(xpos, 5.5f));

            BatteryOvercharge.alpha = Mathf.Clamp01(pourcent * 5);
            
            if (this.SCM != null && this.SCM.endlessCharge > 0)
            {
                float blue = 0.25f + 0.25f * Mathf.Sin((this.SCM.overchargeImmunity%(Mathf.PI * 100)) * 2 * Mathf.PI / (BTWFunc.FrameRate * 1f));
                BatteryOvercharge.color = new Color(1f - blue, 1f - blue, 1f);
            }
            else
            {
                BatteryOvercharge.color = new Color(1f, 0.25f + 0.75f * (1 - pourcent), 0.25f);
            }
        }
        sLeaser.sprites[3] = BatteryOvercharge;
    }
    private void SetBatteryRechargeSprite(RoomCamera.SpriteLeaser sLeaser, float pourcent)
    {
        pourcent = Mathf.Clamp01(pourcent);
        float xpos = 18f * pourcent - 9f;
        TriangleMesh BatteryRecharge = (TriangleMesh)sLeaser.sprites[4];
        BatteryRecharge.MoveVertice(2, new Vector2(xpos, -9f));
        BatteryRecharge.MoveVertice(3, new Vector2(xpos, -7f));

        BatteryRecharge.color = new Color(1f, 0.5f + 0.5f * Mathf.Clamp01(2f - pourcent * 2f), 0.25f + 0.75f * Mathf.Clamp01(1f - pourcent * 2f));
        sLeaser.sprites[4] = BatteryRecharge;
    }

    //-------------- Override Functions

    // Battery Drawing
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        foreach (FSprite sprite in sLeaser.sprites)
        {
            rCam.ReturnFContainer("HUD").AddChild(sprite);
        }
    }
    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (this.SCM == null || this.SCM.Player == null || this.slatedForDeletetion || this.room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
            this.SCM.staticChargeBatteryUI = null;
            this.Destroy();
            return;
        }
        if (this.SCM.init && this.SCM.active && !this.SCM.Player.inShortcut)
        {
            Vector2 pos = this.SpriteHeadPos == Vector2.negativeInfinity ?
                this.SCM.Player.firstChunk.pos + new Vector2(0f, 40f)
                : this.SpriteHeadPos + new Vector2(0f, 20f);
            float OverChargeFactor = this.SCM.endlessCharge > 0 ? 1 :
                this.SCM.IsOvercharged && this.SCM.MaxECharge - this.SCM.FullECharge != 0 ? 
                (this.SCM.Charge - this.SCM.FullECharge) / (this.SCM.MaxECharge - this.SCM.FullECharge) : 0f;
            Vector2 shakeFactor = this.SCM.IsOvercharged && this.SCM.endlessCharge <= 0 ? 
                new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) 
                * 4f * Mathf.Pow(OverChargeFactor, 4f) 
                    : Vector2.zero;

            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.x = pos.x + shakeFactor.x;
                sprite.y = pos.y + shakeFactor.y;
                sprite.alpha = 1f;
            }

            SetBatteryBGSprite(sLeaser);
            SetBatteryChargeSprite(sLeaser, this.SCM.FullECharge > 0 ? this.SCM.Charge / this.SCM.FullECharge : 0);
            SetBatteryRechargeSprite(sLeaser, Mathf.Sqrt(this.SCM.ChargePerSecond / (this.SCM.FullECharge / 2)));
            SetBatteryOverchargeSprite(sLeaser, this.SCM.MaxECharge > 0 || this.SCM.endlessCharge > 0 ? OverChargeFactor : 0);
        }
        else
        {
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.alpha = 0f;
            }
        }

    }
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[5];

        TriangleMesh BatteryOutline = new(
            "Futile_White",
            new TriangleMesh.Triangle[] {
                new(0, 1, 2), new(0, 2, 3),
                new(4, 5, 6), new(4, 6, 7),
                new(8, 9, 10), new(8, 10, 11),
                new(3, 12, 13), new(3, 4, 13),
                new(2, 14, 15), new(2, 5, 15),
            },
            true, false
        );
        BatteryOutline.MoveVertice(0, new Vector2(-9.5f, 6.5f));
        BatteryOutline.MoveVertice(1, new Vector2(-9.5f, -6.5f));
        BatteryOutline.MoveVertice(2, new Vector2(-9f, -6.5f));
        BatteryOutline.MoveVertice(3, new Vector2(-9f, 6.5f));

        BatteryOutline.MoveVertice(12, new Vector2(-9f, 6f));
        BatteryOutline.MoveVertice(14, new Vector2(-9f, -6f));

        BatteryOutline.MoveVertice(4, new Vector2(8f, 6.5f));
        BatteryOutline.MoveVertice(5, new Vector2(8f, -6.5f));
        BatteryOutline.MoveVertice(6, new Vector2(8.5f, -6.5f));
        BatteryOutline.MoveVertice(7, new Vector2(8.5f, 6.5f));

        BatteryOutline.MoveVertice(13, new Vector2(8f, 6f));
        BatteryOutline.MoveVertice(15, new Vector2(8f, -6f));

        BatteryOutline.MoveVertice(8, new Vector2(8.5f, 3.5f));
        BatteryOutline.MoveVertice(9, new Vector2(8.5f, -3.5f));
        BatteryOutline.MoveVertice(10, new Vector2(9.5f, -3.5f));
        BatteryOutline.MoveVertice(11, new Vector2(9.5f, 3.5f));

        BatteryOutline.color = new Color(0.1f, 0.1f, 0.1f);
        sLeaser.sprites[0] = BatteryOutline;


        TriangleMesh BatteryBG = new(
            "Futile_White",
            new TriangleMesh.Triangle[] {
                new(0, 1, 2), new(0, 2, 3),
                new(4, 5, 6), new(4, 6, 7),
                new(8, 9, 10), new(8, 10, 11),
                new(3, 12, 13), new(3, 4, 13),
                new(2, 14, 15), new(2, 5, 15),
            },
            true, false
        );
        BatteryBG.MoveVertice(0, new Vector2(-9f, 6f));
        BatteryBG.MoveVertice(1, new Vector2(-9f, -6f));
        BatteryBG.MoveVertice(2, new Vector2(-8f, -6f));
        BatteryBG.MoveVertice(3, new Vector2(-8f, 6f));

        BatteryBG.MoveVertice(12, new Vector2(-8f, 5f));
        BatteryBG.MoveVertice(14, new Vector2(-8f, -5f));

        BatteryBG.MoveVertice(4, new Vector2(7f, 6f));
        BatteryBG.MoveVertice(5, new Vector2(7f, -6f));
        BatteryBG.MoveVertice(6, new Vector2(8f, -6f));
        BatteryBG.MoveVertice(7, new Vector2(8f, 6f));

        BatteryBG.MoveVertice(13, new Vector2(7f, 5f));
        BatteryBG.MoveVertice(15, new Vector2(7f, -5f));

        BatteryBG.MoveVertice(8, new Vector2(8f, 3f));
        BatteryBG.MoveVertice(9, new Vector2(8f, -3f));
        BatteryBG.MoveVertice(10, new Vector2(9f, -3f));
        BatteryBG.MoveVertice(11, new Vector2(9f, 3f));

        BatteryBG.color = new Color(1f, 1f, 1f);
        sLeaser.sprites[1] = BatteryBG;


        TriangleMesh BatteryCharge = new(
            "Futile_White",
            new TriangleMesh.Triangle[] {
                new(0, 1, 2), new(0, 2, 3)
            },
            true, false
        );
        BatteryCharge.MoveVertice(0, new Vector2(-6f, 4f));
        BatteryCharge.MoveVertice(1, new Vector2(-6f, -4f));
        BatteryCharge.MoveVertice(2, new Vector2(5f, -4f));
        BatteryCharge.MoveVertice(3, new Vector2(5f, 4f));

        BatteryCharge.color = new Color(1f, 1f, 0.25f);
        sLeaser.sprites[2] = BatteryCharge;


        TriangleMesh BatteryOvercharge = new(
            "Futile_White",
            new TriangleMesh.Triangle[] {
                new(0, 1, 2), new(0, 2, 3)
            },
            true, false
        );
        BatteryOvercharge.MoveVertice(0, new Vector2(-8.5f, 5.5f));
        BatteryOvercharge.MoveVertice(1, new Vector2(-8.5f, -5.5f));
        BatteryOvercharge.MoveVertice(2, new Vector2(7.5f, -5.5f));
        BatteryOvercharge.MoveVertice(3, new Vector2(7.5f, 5.5f));

        BatteryOvercharge.color = new Color(1f, 1f, 0.25f);
        sLeaser.sprites[3] = BatteryOvercharge;


        TriangleMesh BatteryRecharge = new(
            "Futile_White",
            new TriangleMesh.Triangle[] {
                new(0, 1, 2), new(0, 2, 3)
            },
            true, false
        );
        BatteryRecharge.MoveVertice(0, new Vector2(-9f, -7f));
        BatteryRecharge.MoveVertice(1, new Vector2(-9f, -9f));
        BatteryRecharge.MoveVertice(2, new Vector2(9f, -9f));
        BatteryRecharge.MoveVertice(3, new Vector2(9f, -7f));

        BatteryRecharge.color = new Color(1f, 0.75f, 0.25f);
        sLeaser.sprites[4] = BatteryRecharge;

        this.AddToContainer(sLeaser, rCam, null);
    }

    //-------------- Variables
    public StaticChargeManager SCM;
    public Vector2 SpriteHeadPos
    {
        get
        {
            if (this.SCM != null && this.SCM.Player != null && BTWSkins.cwtPlayerSpriteInfo.TryGetValue(this.SCM.AbstractPlayer, out var psl))
            {
                return psl[3].GetPosition();
            }
            return Vector2.negativeInfinity;
        }
    }
}

public static class StaticChargeBatteryUIHooks
{
    public static void ApplyHooks()
    {
        On.Player.SpitOutOfShortCut += Player_MoveSparkUI;
        BTWPlugin.Log("StaticChargeBatteryUIHooks ApplyHooks Done !");
    }
    private static void Player_MoveSparkUI(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
    {
        orig(self, pos, newRoom, spitOutAllSticks);
        if (StaticChargeManager.TryGetManager(self.abstractCreature, out var staticChargeManager))
        {
            staticChargeManager.newpos = Vector2.zero;
            staticChargeManager.oldpos = Vector2.zero;
            // if (staticChargeManager.staticChargeBatteryUI != null)
			// {
			// 	// now what ??
			// }
        }
    }
}