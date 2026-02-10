using UnityEngine;
using System;
using RWCustom;
using BeyondTheWest.MeadowCompat;
using System.Runtime.CompilerServices;
using Noise;
using System.Collections.Generic;
using BeyondTheWest.MSCCompat;

namespace BeyondTheWest;

public class ScreenDistord : UpdatableAndDeletable, IDrawable
{
    public ScreenDistord() : base()
    {
    }
    public ScreenDistord(int fadeTime) : this()
    {
        this.timerFadeOut = new(fadeTime);
        this.timerFadeIn.value = this.timerFadeIn.max; 
        this.timer.value = this.timer.max; 
    }
    public ScreenDistord(int fadeIn, int fadeHold, int fadeOut) : this()
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
        sLeaser.sprites = new FSprite[2];

        sLeaser.sprites[0] = new FSprite("Futile_White", true)
        {
            color = Color.white,
            x = this.rainWorld.screenSize.x / 2f,
            y = this.rainWorld.screenSize.y / 2f,
            alpha = 0f,
            shader = rCam.room.game.rainWorld.Shaders["HeatDistortion"],
        };
        sLeaser.sprites[1] = new FSprite("Futile_White", true)
        {
            x = this.rainWorld.screenSize.x / 2f,
            y = this.rainWorld.screenSize.y / 2f,
            alpha = 0f,
            shader = rCam.room.game.rainWorld.Shaders["Sandstorm"],
        };

        this.AddToContainer(sLeaser, rCam, null);
    }
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        foreach (FSprite sprite in sLeaser.sprites)
        {
            rCam.ReturnFContainer("GrabShaders").AddChild(sprite);
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
        float fadeToBlack = !this.timerFadeIn.ended ? this.timerFadeIn.fract 
            : !this.timer.ended ? 1 
            : this.timerFadeOut.fractInv;

        foreach (FSprite fadeSprite in sLeaser.sprites)
        {
            fadeSprite.scaleX = this.rainWorld.screenSize.x * 2f; 
            fadeSprite.scaleY = this.rainWorld.screenSize.y * 2f;
        }
		sLeaser.sprites[0].alpha = fadeToBlack;
		sLeaser.sprites[1].alpha = fadeToBlack * 0.75f;
    }
    public Counter timer = new(1);
    public Counter timerFadeIn = new(1);
    public Counter timerFadeOut = new(BTWFunc.FrameRate * 2);

    public RainWorld rainWorld => this.room?.world?.game?.rainWorld;
}