using UnityEngine;
using System;
using System.Collections.Generic;

namespace BeyondTheWest;

public abstract class AttractiveCentralForce : UpdatableAndDeletable
{
    public AttractiveCentralForce(BodyChunk attractedChunk, Vector2 center, float force, float maxForce, float maxRadius) 
        : this(attractedChunk, center, force)
    {
        this.maxRadius = maxRadius;
        this.maxForce = maxForce;
    }
    public AttractiveCentralForce(BodyChunk attractedChunk, Vector2 center, float force, float maxForce) 
        : this(attractedChunk, center, force)
    {
        this.maxForce = maxForce;
    }
    public AttractiveCentralForce(BodyChunk attractedChunk, Vector2 center, float force) 
        : this(attractedChunk, center)
    {
        this.attractionForce = force;
    }
    public AttractiveCentralForce(BodyChunk attractedChunk, Vector2 center)
    {
        this.attractedChunk = attractedChunk;
        this.center = center;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (attractedChunk.owner?.room != this.room)
        {
            this.Destroy();
            return;
        }
        
        if (active)
        {
            ApplyForce();
        }
    }

    public virtual Vector2 GetCurrentForce()
    {
        Vector2 vectdist = this.center - attractedChunk.pos;
        float dist = vectdist.magnitude;
        vectdist = vectdist.normalized;
        float force = 0;

        if (!this.LimitedRadius || dist < this.maxRadius)
        {
            float distPOW = Mathf.Pow(dist, invertDistancePower);
            if (Mathf.Abs(distPOW) < epsilon) { distPOW = epsilon; }
            force = attractionForce * (1 / distPOW);
            if (Mathf.Abs(force) > maxForce) { force = maxForce * Mathf.Sign(force); }
        }

        return force * vectdist;
    }

    public virtual void ApplyForce()
    {
        attractedChunk.vel += GetCurrentForce();
    }

    public float attractionForce = 100f;
    public float maxRadius = -1;
    public float maxForce = 0.1f;
    public float invertDistancePower = 2f;
    public const float epsilon = 1E-3f;
    public bool active = true;
    public bool LimitedRadius => maxRadius > 0;
    public Vector2 center;
    public BodyChunk attractedChunk;
}

public abstract class AttractiveObjectForce : AttractiveCentralForce
{
    public AttractiveObjectForce(BodyChunk attractedChunk, float force, float maxForce, float maxRadius, Func<BodyChunk, bool> attractionCondition) 
        : base(attractedChunk, Vector2.one, force, maxForce, maxRadius)
    {
        this.selector = attractionCondition;
    }
    public AttractiveObjectForce(BodyChunk attractedChunk, float force, float maxForce, Func<BodyChunk, bool> attractionCondition) 
        : base(attractedChunk, Vector2.one, force, maxForce)
    {
        this.selector = attractionCondition;
    }
    public AttractiveObjectForce(BodyChunk attractedChunk, float force, Func<BodyChunk, bool> attractionCondition) 
        : base(attractedChunk, Vector2.one)
    {
        this.selector = attractionCondition;
    }
    public AttractiveObjectForce(BodyChunk attractedChunk, Func<BodyChunk, bool> attractionCondition) 
        : this(attractedChunk)
    {
        this.selector = attractionCondition;
    }
    public AttractiveObjectForce(BodyChunk attractedChunk) 
        : base(attractedChunk, Vector2.one) { }

    public override void ApplyForce()
    {
        this.chunksAffected.Clear();
        var objectList = BTWFunc.GetAllObjects(this.room);
        for (int i = 0; i < objectList.Count; i++)
        {
            int closestChunk = -1;
            float mindist = this.maxRadius > 0 ? this.maxRadius : float.MaxValue;
            for (int n = 0; n < objectList[i].bodyChunks.Length; n++)
            {
                if (this.selector(objectList[i].bodyChunks[n]))
                {
                    float dist = (objectList[i].bodyChunks[n].pos - this.attractedChunk.pos).magnitude;
                    // BTWPlugin.Log($"Found [{objectList[i].bodyChunks[n].owner}] at <{dist}>");
                    if ((!this.LimitedRadius || dist < this.maxRadius) && mindist > dist)
                    {
                        mindist = dist;
                        closestChunk = n;
                    }
                }
            }
            if (closestChunk >= 0)
            {
                chunksAffected.Add(objectList[i].bodyChunks[closestChunk]);
            }
        }

        Vector2 totalForce = Vector2.zero;
        for (int j = 0; j < chunksAffected.Count; j++)
        {
            this.center = chunksAffected[j].pos;
            totalForce += GetCurrentForce();
        }
        totalForce = Mathf.Min(totalForce.magnitude, this.maxForce) * totalForce.normalized;

        attractedChunk.vel += totalForce;
    }

    public Func<BodyChunk, bool> selector = x => x.owner != null;
    public List<BodyChunk> chunksAffected = new();
}