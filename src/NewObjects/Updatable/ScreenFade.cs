using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using Noise;
using System.Collections.Generic;
using BeyondTheWest.MSCCompat;

namespace BeyondTheWest;

public class ScreenBlind : UpdatableAndDeletable, IDrawable
{
    public ScreenBlind(Color color = default) : base()
    {
        this.color = color == default ? this.color : color;
    }
    public ScreenBlind(int fadeTime, Color color = default) : this(color)
    {
        this.timerFadeOut = new(fadeTime);
        this.timerFadeIn.value = this.timerFadeIn.max; 
        this.timer.value = this.timer.max; 
    }
    public ScreenBlind(int fadeIn, int fadeHold, int fadeOut, Color color = default) : this(color)
    {
        this.timerFadeIn = new(fadeIn);
        this.timer = new(fadeHold);
        this.timerFadeOut = new(fadeOut);
        if (fadeIn <= 0) { this.timerFadeIn.value = this.timerFadeIn.max; }
        if (fadeHold <= 0) { this.timer.value = this.timer.max; }
        if (fadeOut <= 0) { this.timerFadeOut.value = this.timerFadeOut.max; }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (!this.timerFadeIn.ended)
        {
            this.timerFadeIn.Tick();
        }
        else if (!this.timer.ended)
        {
            this.timer.Tick();
        }
        else if (!this.timerFadeOut.ended)
        {
            this.timerFadeOut.Tick();
        }
        else
        {
            this.Destroy();
        }
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];

        FSprite fadeSprite = new FSprite("Futile_White", true)
        {
            color = this.color,
            x = this.rainWorld.screenSize.x / 2f,
            y = this.rainWorld.screenSize.y / 2f,
            alpha = 0f
        };
        sLeaser.sprites[0] = fadeSprite;

        this.AddToContainer(sLeaser, rCam, null);
    }
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        foreach (FSprite sprite in sLeaser.sprites)
        {
            rCam.ReturnFContainer("HUD2").AddChild(sprite);
        }
    }
    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {}
    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
        {
            sLeaser.CleanSpritesAndRemove();
            return;
        }
        FSprite fadeSprite = sLeaser.sprites[0];
        float fadeToBlack = !this.timerFadeIn.ended ? this.timerFadeIn.fract 
            : !this.timer.ended ? 1 
            : this.timerFadeOut.fractInv;

        fadeSprite.scaleX = this.rainWorld.screenSize.x * 2f; //(this.rainWorld.screenSize.x * Mathf.Lerp(1.5f, 1f, fadeToBlack) + 2f) / 16f;
		fadeSprite.scaleY = this.rainWorld.screenSize.y * 2f; //(this.rainWorld.screenSize.y * Mathf.Lerp(2.5f, 1.5f, fadeToBlack) + 2f) / 16f;
		fadeSprite.alpha = fadeToBlack;
    }
    
    public Color color = Color.black;
    public Counter timer = new(1);
    public Counter timerFadeIn = new(1);
    public Counter timerFadeOut = new(BTWFunc.FrameRate * 2);

    public RainWorld rainWorld => this.room?.world?.game?.rainWorld;
}