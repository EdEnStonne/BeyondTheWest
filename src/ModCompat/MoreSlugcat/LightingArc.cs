using UnityEngine;
using BeyondTheWest;
using MoreSlugcats;
using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace BeyondTheWest.MSCCompat;
public class LightingArc : UpdatableAndDeletable
{
    public LightingArc(float width, float intensity, int lifeTime, Color color)
    {
        this.width = width;
        this.intensity = intensity;
        this.lifeTime = lifeTime <= 0 ? 1 : lifeTime;
        this.color = color;
    }
    public LightingArc(BodyChunk from, BodyChunk target, float width, float intensity, int lifeTime, Color color)
        : this(width, intensity, lifeTime, color)
    {
        this.from = from;
        this.target = target;

        CreateLightingArc();
    }
    public LightingArc(Vector2 fromPos, BodyChunk target, float width, float intensity, int lifeTime, Color color)
        : this(width, intensity, lifeTime, color)
    {
        this.fromOffset = fromPos;
        this.target = target;

        CreateLightingArc();
    }
    public LightingArc(BodyChunk from, Vector2 targetPos, float width, float intensity, int lifeTime, Color color)
        : this(width, intensity, lifeTime, color)
    {
        this.from = from;
        this.targetOffset = targetPos;

        CreateLightingArc();
    }
    public LightingArc(Vector2 fromPos, Vector2 targetPos, float width, float intensity, int lifeTime, Color color)
        : this(width, intensity, lifeTime, color)
    {
        this.fromOffset = fromPos;
        this.targetOffset = targetPos;

        CreateLightingArc();
    }

    private void CreateLightingArc()
    {
        this.lightningArc = new LightningBolt(Vector2.zero, Vector2.zero, 0, width, lifeTime /  30f, 0.64f, 0.64f, true)
        {
            intensity = intensity
        };
        if (color != null)
        {
            this.lightningArc.color = color;
        }
        SetLightingArcPos();
    }
    private void SetLightingArcPos()
    {
        if (this.lightningArc != null)
        {
            Vector2 fromPos = this.fromOffset + (this.from != null ? this.from.pos : Vector2.zero);
            Vector2 toPos = this.targetOffset + (this.target != null ? this.target.pos : Vector2.zero);
            this.lightningArc.from = fromPos;
            this.lightningArc.target = toPos;
        }
    }
    public override void Update(bool eu)
    {
        base.Update(eu);
        if (!this.lightningAddedToRoom)
        {
            if (this.lightningArc == null) { this.Destroy(); return; }
            if (this.room != null)
            {
                
                this.room.AddObject(this.lightningArc);
                this.lightningAddedToRoom = true;
                // Plugin.Log("Lighting Arc added to room !");
            }
            return;
        }
        if (this.timelived < this.lifeTime
            && this.lightningArc != null 
            && !this.lightningArc.slatedForDeletetion)
        {
            SetLightingArcPos();
            timelived++;
        }
        else
        {
            this.Destroy();
            // Plugin.Log("Lighting Arc removed !");
        }
    }

    public LightningBolt lightningArc;
    public BodyChunk from;
    public BodyChunk target;
    public Vector2 fromOffset = Vector2.zero;
    public Vector2 targetOffset = Vector2.zero;
    public Color color;
    public bool mustDestroy = false;
    public bool lightningAddedToRoom = false;
    public int timelived = 0;
    public int lifeTime;
    public float width;
    public float intensity;
}
