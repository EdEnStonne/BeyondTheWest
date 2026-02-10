using System.Collections.Generic;
using System.Linq;
using BeyondTheWest.MSCCompat;
using RWCustom;
using UnityEngine;

namespace BeyondTheWest;

public class MouseSparkDrag : UpdatableAndDeletable
{
    public MouseSparkDrag(Vector2 pos, Vector2 vel, float maxLifeTime, Color color, float drag)
    {
        mouseSpark = new(pos, vel, maxLifeTime, color);
        this.drag = drag;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (mouseSpark.slatedForDeletetion)
        {
            this.Destroy();
            return;
        }

        if (!addedMouseSpark)
        {
            this.room.AddObject(mouseSpark);
            addedMouseSpark = true;
        }
        else
        {
            mouseSpark.vel *= drag;
        }
    }

    public override void Destroy()
    {
        base.Destroy();
        mouseSpark.Destroy();
        this.mouseSpark = null;
        // BTWPlugin.Log("MouseSparkDrag deleted !");
    }

    public float drag = 0.3f;
    public bool addedMouseSpark = false;
    private MouseSpark mouseSpark;
}