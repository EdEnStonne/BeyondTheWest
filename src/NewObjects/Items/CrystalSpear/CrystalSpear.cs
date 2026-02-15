using UnityEngine;
using BeyondTheWest;
using System.Collections.Generic;
using BeyondTheWest.MeadowCompat;
using System;
using System.Linq;
using RWCustom;
using Noise;

namespace BeyondTheWest.Items;

public class CrystalSpear : Spear, VoidSpark.IReactToVoidFlux
{
    public CrystalSpear(AbstractCrystalSpear abstractCrystalSpear) : base(abstractCrystalSpear, abstractCrystalSpear.world)
    {
	    base.gravity = 0.85f;
    }

    public override void Update(bool eu)
    {
        int oldBounceCount = this.floorBounceFrames;
        IntVector2 oldThrowDir = this.throwDir;

        base.Update(eu);

        if (this.Local()
            && oldBounceCount > 0 
            && this.floorBounceFrames < oldBounceCount - 1
            && oldThrowDir == this.throwDir)
        {
            if (this.throwDir.x == 0)
            {
                this.firstChunk.vel.x += Mathf.Sign(this.firstChunk.vel.x) * 12f;
                this.firstChunk.vel.y *= 0.65f;
            }
            else if (this.throwDir.y == 0)
            {
                this.firstChunk.vel.y += Mathf.Sign(this.firstChunk.vel.y) * 12f;
                this.firstChunk.vel.x *= 0.65f;
            }
            Pop();
            if (BTWPlugin.meadowEnabled)
            {
                MeadowCalls.BTWItems_PopCrystalSpear(this);
            }
        }
    }
    public override void HitByWeapon(Weapon weapon)
    {
        base.HitByWeapon(weapon);
        if (BTWFunc.Chance(0.35f) && !this.exploded)
		{
			Explode();
		}
    }
    public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu) // From Scavenger Bomb
	{
		if (result.obj == null)
		{
			return false;
		}
		if (result.obj.abstractPhysicalObject.rippleLayer != this.abstractPhysicalObject.rippleLayer && !result.obj.abstractPhysicalObject.rippleBothSides && !this.abstractPhysicalObject.rippleBothSides)
		{
			return false;
		}
		this.vibrate = 20;
		this.ChangeMode(Mode.Free);
		if (result.obj is Creature creature && creature != this.thrownBy)
		{
			this.Explode(creatureHit: creature);
		}
		return true;
	}
    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        base.TerrainImpact(chunk, direction, speed, firstContact);

        if (this.mode == Mode.Thrown && direction == this.throwDir)
        {
            this.Explode();
            this.Destroy();
        }
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
		string[] allTexture = this.ACP.appearance.AllTextures;
		int textureCount = this.ACP.appearance.textureCount;
		sLeaser.sprites = new FSprite[textureCount + 2];
        
        sLeaser.sprites[0] = new FSprite("SmallSpear", true);
        for (int i = 1; i <= textureCount; i++)
		{
			sLeaser.sprites[i] = new FSprite(allTexture[textureCount - i], true)
			{
				color = this.baseColor,
				scale = this.crystalScale,
				alpha = VoidCrystal.transparency
			};
		}
        sLeaser.sprites[textureCount + 1] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["LightSource"],
			scale = 4f,
            alpha = 0.5f
        };
        this.AddToContainer(sLeaser, rCam, null);
    } 
    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        newContatiner ??= rCam.ReturnFContainer("Items");
        sLeaser.sprites[0].RemoveFromContainer();
        newContatiner.AddChild(sLeaser.sprites[0]);

        for (int i = sLeaser.sprites.Length - 1; i >= 1; i--)
        {
            sLeaser.sprites[i].RemoveFromContainer();
            if (i == sLeaser.sprites.Length - 1)
            {
		        rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[i]);
            }
            else
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
        }
    }
    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
	    base.ApplyPalette(sLeaser, rCam, palette);
		this.skyColor = Color.Lerp(palette.skyColor, Color.white, 0.5f);
	}
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
	    base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		if (this.blink > 0)
        {
            if (this.blink > 1 && BTWFunc.Chance(0.5f))
            {
                sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
            }
            else
            {
                sLeaser.sprites[0].color = this.color;
            }
        }

		int textureCount = this.ACP.appearance.textureCount;
        Vector2 crystalPos = this.CrystalAttachPos(timeStacker);//sLeaser.sprites[0].GetPosition() + Custom.DegToVec(sLeaser.sprites[0].rotation) * 30f;
        for (int i = 1; i <= textureCount; i++)
        {
            sLeaser.sprites[i].x = crystalPos.x - camPos.x;
            sLeaser.sprites[i].y = crystalPos.y - camPos.y;
            sLeaser.sprites[i].rotation = sLeaser.sprites[0].rotation;
            
            Vector2 coord = this.ACP.appearance.GetTextureCoordFromLinearCoord(textureCount - i);
			float addedRot = this.ACP.appearance.layerRotation[(int)coord.x] * 90f;
			sLeaser.sprites[i].rotation += addedRot;

			float rotDir = sLeaser.sprites[i].rotation + coord.y * 90f + 45f;
			float diffFromSky = Mathf.Abs(rotDir - skyAngle)%360;
			if (diffFromSky > 180) { diffFromSky = 360 - diffFromSky; }
			diffFromSky /= 180f;

            sLeaser.sprites[i].color = Color.Lerp(this.baseColor, skyColor, BTWFunc.EaseIn(1 - diffFromSky, 8));
        }
        sLeaser.sprites[textureCount + 1].x = crystalPos.x - camPos.x;
        sLeaser.sprites[textureCount + 1].y = crystalPos.y - camPos.y;
		sLeaser.sprites[textureCount + 1].color = Color.Lerp(this.baseColor, Color.white, 0.5f);
		sLeaser.sprites[textureCount + 1].scale = 4f;
		sLeaser.sprites[textureCount + 1].alpha = 0.5f;

		if (base.slatedForDeletetion || this.room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}

    public Vector2 CrystalAttachPos(float timeStacker) 
    {
	    Vector2 pos = Vector2.Lerp(base.firstChunk.lastPos, base.firstChunk.pos, timeStacker);
	    Vector3 rot = Vector3.Slerp(this.lastRotation, this.rotation, timeStacker);
        Vector2 rotDir = Custom.DirVec(new Vector2(0f, 0f), rot);
		float tipLenght = Mathf.Lerp(this.lastPivotAtTip ? 7f : 18f, this.pivotAtTip ? 7f : 18f, timeStacker);
		return pos + rotDir * tipLenght;
    }

    public void Pop(Vector2 pos = default)
    {
        if (pos == default) { pos = this.firstChunk.pos; }
		this.room.AddObject(new ShockWave(this.firstChunk.pos, 60f, 0.045f, 5, false));
        this.room.PlaySound(SoundID.Fire_Spear_Pop, this.firstChunk, false, 0.8f, BTWFunc.Random(1.8f, 1.9f));
        VoidSpark.MakeDraggedSparks(this.room, 40f, this.firstChunk.pos, 
            (byte)BTWFunc.RandInt(20, 30), this.baseColor, 0.2f);
    }
    public void Explode(Creature creatureHit = null, Vector2 pos = default)
	{
		if (this.exploded) { return; }

		this.exploded = true;
        if (pos == default) { pos = this.firstChunk.pos; }
		
		this.room.AddObject(new Explosion.ExplosionLight(pos, 160f, 1f, 3, this.explodeColor));
		this.room.AddObject(new ExplosionSpikes(this.room, pos, 9, 4f, 5f, 5f, 90f, this.explodeColor));
		this.room.AddObject(new ShockWave(pos, 60f, 0.045f, 12, false));
		this.room.ScreenMovement(new Vector2?(pos), default, 0.8f);
		for (int k = 0; k < BTWFunc.Random(1, this.ACP.appearance.shard); k++)
		{
			this.room.AddObject(new VoidCrystal.VoidCrystalFragment(base.firstChunk.pos, Custom.RNV() * BTWFunc.Random(20f, 40f)));
		}
		this.abstractPhysicalObject.LoseAllStuckObjects();
		this.room.PlaySound(SoundID.Fire_Spear_Explode, pos, 0.8f, BTWFunc.Random(1.8f, 1.9f), this.abstractPhysicalObject);
		this.room.InGameNoise(new InGameNoise(pos, 6000f, this, 1f));

		if (this.Local())
		{
            VoidSpark spark = new(this.firstChunk.lastPos, 1.75f, BTWFunc.FrameRate)
            {
                killTagHolder = this.thrownBy?.abstractCreature,
                source = this,
                target = creatureHit
            };
            if (this.thrownBy != null)
            {
                spark.sparedList.Add(this.thrownBy);
            }
            
            AbstractSpear abstractSpear = new AbstractSpear(
                this.room.world, null, room.GetWorldCoordinate(this.firstChunk.lastPos), 
                this.room.world.game.GetNewID(), false);
            room.abstractRoom.entities.Add(abstractSpear);
            abstractSpear.RealizeInRoom();
            Spear spearIssued = null;
            if (abstractSpear.realizedObject is Spear spear)
            {
                spearIssued = spear;
                spear.ChangeMode(this.mode == Mode.Thrown ? Mode.Thrown : Mode.Free);

                spear.changeDirCounter = this.changeDirCounter;
                spear.firstFrameTraceFromPos = this.firstFrameTraceFromPos;
                spear.lastMode = this.lastMode;
                spear.lastRotation = this.lastRotation;
                spear.overrideExitThrownSpeed = this.overrideExitThrownSpeed;
                spear.rotationSpeed = this.rotationSpeed;
                spear.setRotation = this.setRotation;
                spear.thrownBy = this.thrownBy;
                spear.thrownClosestToCreature = this.thrownClosestToCreature;
                spear.spearDamageBonus = this.spearDamageBonus;
                spear.doNotTumbleAtLowSpeed = this.doNotTumbleAtLowSpeed;

                spear.rotation = -this.rotation;
                spear.vibrate = 20;
                spear.firstChunk.pos = this.firstChunk.lastPos;
                spear.firstChunk.lastPos = this.firstChunk.pos;
                spear.firstChunk.lastLastPos = this.firstChunk.pos;

                if (spear.mode == Mode.Thrown)
                {
                    spear.thrownPos = this.firstChunk.lastPos;
                    spear.floorBounceFrames = 20;
                    spear.throwDir = new IntVector2(-this.throwDir.x, -this.throwDir.y);
                    spear.throwModeFrames = Mathf.Max(this.throwModeFrames, 40);
                    spear.overrideExitThrownSpeed = 12f;

                    spear.firstChunk.vel = new Vector2(
                        (this.throwDir.x != 0 ? -0.75f : 1.25f) * this.firstChunk.vel.x, 
                        (this.throwDir.y != 0 ? -0.75f : 1.25f) * this.firstChunk.vel.y);
                }
                else
                {
                    spear.firstChunk.vel = BTWFunc.RandomCircleVector(20f);
                }

                BTWPlugin.Log($"BOOM ! [{this}] converted into [{spear}] with a reversed direction !");
            }
            if (spearIssued != null)
            {
                spark.sparedList.Add(spearIssued);
            }
			this.room.AddObject( spark );

            if (BTWPlugin.meadowEnabled)
            {
                MeadowCalls.BTWItems_ExplodeCrystalSpear(this);
            }
		}

		this.Destroy();
	}

    public void VoidSparkAppears(VoidSpark voidSpark)
    {
        
    }

    public void HitByVoidSpark(VoidSpark voidSpark)
    {
		if (BTWFunc.Chance(voidSpark.damage / 10f) && !this.exploded)
		{
			Explode();
		}
    }

    public AbstractCrystalSpear abstractCrystalSpear
    {
        get
        {
            return (AbstractCrystalSpear)this.abstractPhysicalObject;
        }
    }
    public AbstractCrystalSpear ACP
    {
        get
        {
            return this.abstractCrystalSpear;
        }
    }
    public Color baseColor = new Color(1f, 0.2f, 0.5f);
    private Color skyColor = new Color(1f, 1f, 1f);
    private Color explodeColor = new Color(1f, 1f, 1f);
	public float crystalScale = 0.9f;
    public const float skyAngle = 0;
	public bool exploded = false;
}