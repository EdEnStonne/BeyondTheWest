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

namespace BeyondTheWest;

public class DebugCircle : UpdatableAndDeletable, IDrawable
{

    public DebugCircle(Vector2 pos)
    {
        this.pos = pos;
    }
    public DebugCircle() : this(Vector2.zero) { }


    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];

        sLeaser.sprites[0] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["VectorCircleFadable"],
            color = this.color,
            alpha = this.innerRatio,
            scale = this.radius / 7f
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

        sLeaser.sprites[0].x = pos.x - camPos.x;
        sLeaser.sprites[0].y = pos.y - camPos.y;
        sLeaser.sprites[0].scale = this.radius / 7f;
        sLeaser.sprites[0].alpha = this.innerRatio;
        sLeaser.sprites[0].color = visible ? this.color : Color.black;
    }
    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        foreach (FSprite sprite in sLeaser.sprites)
        {
            rCam.ReturnFContainer("HUD").AddChild(sprite);
        }
    }
    public Color color = Color.white;
    public Vector2 pos = Vector2.one;
    public float radius = 20f;
    public float innerRatio = 0.2f;
    public bool visible = false;
}