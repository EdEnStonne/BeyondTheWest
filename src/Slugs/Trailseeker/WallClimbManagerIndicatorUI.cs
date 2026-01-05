using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;

namespace BeyondTheWest;
public class WallClimbManagerIndicatorUI : UpdatableAndDeletable, IDrawable
{
    public WallClimbManagerIndicatorUI(WallClimbManager wallClimbManager)
    {
        this.WCM = wallClimbManager;
    }
    
    //-------------- Local Functions
    
    //-------------- Override Functions
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
        if (this.WCM == null || this.WCM.RealizedPlayer == null || this.slatedForDeletetion || this.room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
            this.WCM.indicatorUI = null;
            this.Destroy();
            return;
        }

        Player player = WCM.RealizedPlayer;
        if (!player.inShortcut)
        {
            Vector2 pos = this.SpriteHeadPos == Vector2.negativeInfinity ?
                player.firstChunk.pos + new Vector2(0f, 60f)
                : this.SpriteHeadPos + new Vector2(0f, 40f);
            bool ignorePoleToggle = BTWRemix.TrailseekerIgnorePoleToggle.Value;
            bool ignorePoleInvert = BTWRemix.TrailseekerIgnorePoleInvert.Value;
            
            sLeaser.sprites[0].alpha = this.WCM.holdToPoles ? 0f : 1f;
            sLeaser.sprites[1].alpha = this.WCM.holdToPoles ? 1f : 0f;
            if (!ignorePoleToggle)
            {
                if (ignorePoleInvert) { sLeaser.sprites[0].alpha = 0f; }
                else { sLeaser.sprites[1].alpha = 0f; }
            }
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.x = pos.x;
                sprite.y = pos.y;
                sprite.scale = this.scale;
                if (ignorePoleToggle)
                {
                    sprite.alpha *= Mathf.Clamp01(showPoleIcon * 4f / MaxShowPoleIconFrames);
                }
                sprite.scale *= 1f
                    + BTWFunc.EaseIn(Mathf.Clamp01((showPoleIcon * 16f / MaxShowPoleIconFrames) - 15f), 3) * 0.5f;
            }
            if (showPoleIcon > 0) { showPoleIcon--; }
        }
        else
        {
            foreach (FSprite sprite in sLeaser.sprites)
            {
                sprite.alpha = 0f;
            }
            showPoleIcon = 0;
        }

    }
    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[2];

        if (Futile.atlasManager.DoesContainElementWithName("NoPoleIcon"))
        {
            sLeaser.sprites[0] = new FSprite("NoPoleIcon");
        }
        else
        {
            sLeaser.sprites[0] = new FSprite("Futile_White");
        }
        if (Futile.atlasManager.DoesContainElementWithName("YesPoleIcon"))
        {
            sLeaser.sprites[1] = new FSprite("YesPoleIcon");
        }
        else
        {
            sLeaser.sprites[1] = new FSprite("Futile_White");
        }

        this.AddToContainer(sLeaser, rCam, null);
    }
    public void ShowPoleIcon()
    {
        this.showPoleIcon = this.MaxShowPoleIconFrames;
    }

    //-------------- Variables
    public WallClimbManager WCM;
    public Vector2 SpriteHeadPos
    {
        get
        {
            if (this.WCM != null && this.WCM.RealizedPlayer != null && BTWSkins.cwtPlayerSpriteInfo.TryGetValue(this.WCM.abstractPlayer, out var psl))
            {
                return psl[3].GetPosition();
            }
            return Vector2.negativeInfinity;
        }
    }
    public int MaxShowPoleIconFrames = 200;
    public int showPoleIcon = 0;
    public float scale = 0.5f;
}