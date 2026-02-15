using System;
using System.Collections.Generic;
using System.Linq;
using BeyondTheWest.MSCCompat;
using Noise;
using RWCustom;
using UnityEngine;

namespace BeyondTheWest.Items;

public class VoidCrystal : Weapon, VoidSpark.IReactToVoidFlux
{
    public VoidCrystal(AbstractVoidCrystal abstractVoidCrystal) : base(abstractVoidCrystal, abstractVoidCrystal.world)
    // Mostly taken from Rock code
    {
        this.bodyChunks = new BodyChunk[1];
		this.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 4f, 0.05f);
		this.bodyChunkConnections = new BodyChunkConnection[0];
		this.airFriction = 0.999f;
		this.g = 0.85f;
		this.bounce = 0.35f;
		this.surfaceFriction = 0.2f;
		this.collisionLayer = this.DefaultCollLayer;
		this.waterFriction = 0.98f;
		this.buoyancy = 0.4f;
		this.firstChunk.loudness = 21f;
		this.tailPos = this.firstChunk.pos;
		this.soundLoop = new ChunkDynamicSoundLoop(this.firstChunk);
    }
    public override bool HeavyWeapon => true;
    public override int DefaultCollLayer => 1;

	public void HitSound(float vol = 1f)
	{
		this.room.PlaySound(SoundID.Spear_Bounce_Off_Wall, base.firstChunk, false, vol * 0.5f, BTWFunc.Random(2.6f, 2.85f));
		this.room.PlaySound(SoundID.Rock_Hit_Wall, base.firstChunk, false, vol * 0.75f, BTWFunc.Random(3f, 3.1f));
		this.room.PlaySound(SoundID.Weapon_Skid, base.firstChunk, false, vol * 1f, BTWFunc.Random(1.7f, 1.75f));
	}

	public override void Update(bool eu)
	{
		if (BTWFunc.IsLocal(this))
		{
			if (this.ignited)
			{
				if (!this.exploded)
				{
					this.explodeCounter.Tick();
					if (this.explodeCounter.ended)
					{
						this.Explode();
					}
				}
				else
				{
					this.Destroy();
				}
			}
		}
		if (this.ignited)
		{
			this.g = Mathf.Pow(this.explodeCounter.fractInv, 3);
			if (this.floorBounceFrames == 0)
			{
				this.firstChunk.vel *= this.explodeCounter.fractInv;
				this.firstChunk.vel.y += this.explodeCounter.fract;
			}
		}
		else
		{
			this.g = 0.85f;
		}

		base.Update(eu);

		this.soundLoop.sound = SoundID.None;
		if (base.firstChunk.vel.magnitude > 5f)
		{
			if (base.firstChunk.ContactPoint.y < 0)
			{
				this.soundLoop.sound = SoundID.Rock_Skidding_On_Ground_LOOP;
			}
			else
			{
				this.soundLoop.sound = SoundID.Rock_Through_Air_LOOP;
			}
			this.soundLoop.Volume = Mathf.InverseLerp(5f, 15f, base.firstChunk.vel.magnitude);
		}
		this.soundLoop.Update();

        if (this.grabbedBy.Count > 0)
        {
            this.rotationSpeed = 0;
        }
		else if (base.firstChunk.ContactPoint.y != 0)
		{
			this.rotationSpeed = (this.rotationSpeed * 2f + base.firstChunk.vel.x * 5f) / 3f;
		}
	}

    public override void HitWall() // taken from Weapon
    {
        if (this.room.BeingViewed)
		{
			for (int i = 0; i < 7; i++)
			{
				this.room.AddObject(new Spark(
					base.firstChunk.pos + this.throwDir.ToVector2() * (base.firstChunk.rad - 1f), 
					Custom.DegToVec(BTWFunc.Random(360f)) * BTWFunc.Random(10f) + -this.throwDir.ToVector2() * 10f, 
					new Color(1f, 1f, 1f), null, 2, 4));
			}
		}
		this.room.ScreenMovement(new Vector2?(base.firstChunk.pos), this.throwDir.ToVector2() * 1.5f, 0f);
		HitSound();
		this.SetRandomSpin();
		this.ChangeMode(Mode.Free);
		base.forbiddenToPlayer = 10;
    }
    public override void WeaponDeflect(Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
    {
        base.WeaponDeflect(inbetweenPos, deflectDir, bounceSpeed);
		HitSound(0.5f);
		this.InitExplosion();
    }
    public override void HitByWeapon(Weapon weapon)
    {
        base.HitByWeapon(weapon);
		HitSound(0.5f);
		this.InitExplosion();
		if (weapon is VoidCrystal otherCrystal)
		{
			otherCrystal.InitExplosion();
		}
    }
    public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        base.Collide(otherObject, myChunk, otherChunk);
		if ((this.mode == Mode.Thrown || this.ignited) && otherObject is Creature creature && creature != this.thrownBy)
		{
			this.Explode();
			this.target = creature;
		}
    }
    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        base.TerrainImpact(chunk, direction, speed, firstContact);
		if (speed > 1f)
		{
			HitSound(Mathf.Clamp01(speed / 20f));
		}
		if (this.mode == Mode.Thrown && (speed > 8f || this.floorBounceFrames > 0))
		{
			InitExplosion();
		}
		else if (speed > 40f)
		{
			InitExplosion();
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
			if (this.floorBounceFrames == 0)
			{
				this.Explode();
				this.target = creature;
			}
			else
			{
				this.InitExplosion();
				this.explodeCounter.value += (int)(this.explodeCounter.max * 0.5f);
			}
		}
		else
		{
			this.InitExplosion();
		}
		return true;
	}

	public void InitExplosion()
	{
		if (this.ignited) { return; }

		if (this.floorBounceFrames > 0) { this.explodeCounter.max *= 2; }
		this.ignited = true;
		this.room.PlaySound(SoundID.Flare_Bomb_Burn, base.firstChunk, false, 0.5f, BTWFunc.Random(2f, 2.1f));
		base.forbiddenToPlayer = (int)this.explodeCounter.max + 10;
	}
	public void Explode()
	{
		if (this.exploded) { return; }

		this.exploded = true;
		Vector2 vector = base.firstChunk.pos + this.rotation * 10f;
		
		this.room.AddObject(new Explosion.ExplosionLight(vector, 160f, 1f, 3, this.explodeColor));
		this.room.AddObject(new ExplosionSpikes(this.room, vector, 9, 4f, 5f, 5f, 90f, this.explodeColor));
		this.room.AddObject(new ShockWave(vector, 60f, 0.045f, 12, false));
		this.room.ScreenMovement(new Vector2?(vector), default, Mathf.Min(1f, this.AVC.containedVoidEnergy / 10f));
		for (int k = 0; k < BTWFunc.Random(1, this.AVC.appearance.shard); k++)
		{
			this.room.AddObject(new VoidCrystalFragment(base.firstChunk.pos, Custom.RNV() * BTWFunc.Random(20f, 40f), this));
		}
		this.abstractPhysicalObject.LoseAllStuckObjects();
		float vol = Mathf.Clamp(this.AVC.containedVoidEnergy / 6f, 0.5f, 0.9f);
		this.room.PlaySound(SoundID.Fire_Spear_Explode, vector, vol, BTWFunc.Random(1.8f, 1.9f), this.abstractPhysicalObject);
		this.room.InGameNoise(new InGameNoise(vector, 8000f * vol, this, 1f));

		if (this.AVC.containedVoidEnergy > 0.1f && this.Local())
		{
			if (BTWFunc.Chance(0.65f))
			{
				while (this.AVC.containedVoidEnergy > 0)
				{
					float energyDispensed = Mathf.Min(this.AVC.containedVoidEnergy, BTWFunc.Random(0.75f, 1.25f));
					this.room.AddObject( new VoidSpark( this.firstChunk.pos , energyDispensed, (int)Mathf.Clamp(40 * 1f / energyDispensed, 10, 200) )
					{
						killTagHolder = this.thrownBy?.abstractCreature,
						source = this,
						target = this.target
					} );
					this.AVC.containedVoidEnergy -= energyDispensed;
				}
			}
			else
			{
				this.room.AddObject( new VoidSpark( this.firstChunk.pos , this.AVC.containedVoidEnergy, (int)Mathf.Clamp(40 * 1f/this.AVC.containedVoidEnergy, 10, 200))
					{
						killTagHolder = this.thrownBy?.abstractCreature,
						source = this,
						target = this.target
					}  );
				this.AVC.containedVoidEnergy = 0;
			}
		}
		this.Destroy();
	}

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		string[] allTexture = this.AVC.appearance.AllTextures;
		int textureCount = this.AVC.appearance.textureCount;
		sLeaser.sprites = new FSprite[textureCount + 2];

		for (int i = 0; i < textureCount; i++)
		{
			// BTWPlugin.Log($"Loading Void Crystal. Index is <{i}>, texture name is [{allTexture[i]}]");
			sLeaser.sprites[i] = new FSprite(allTexture[textureCount - i - 1], true)
			{
				color = this.baseColor,
				scale = this.scale,
				alpha = transparency
			};
		}

        TriangleMesh.Triangle[] trail = new TriangleMesh.Triangle[]
		{
			new(0, 1, 2)
		};
		TriangleMesh TrailMesh = new TriangleMesh("Futile_White", trail, false, false);
		sLeaser.sprites[textureCount] = TrailMesh;

        sLeaser.sprites[textureCount + 1] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["LightSource"],
			scale = 3f,
            alpha = 0.3f
        };

		this.AddToContainer(sLeaser, rCam, null);
	}
    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        base.AddToContainer(sLeaser, rCam, newContatiner);
		rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[sLeaser.sprites.Length - 1]);
    }
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (this.refreshTexture)
		{
			this.refreshTexture = false;
			InitiateSprites(sLeaser, rCam);
		}
		
		Vector2 pos = Vector2.Lerp(base.firstChunk.lastPos, base.firstChunk.pos, timeStacker);
		Vector3 rot = Vector3.Slerp(this.lastRotation, this.rotation, timeStacker);
		if (this.vibrate > 0)
		{
			pos += Custom.DegToVec(BTWFunc.random * 360f) * (2f * BTWFunc.random);
		}
		if (!this.explodeCounter.atZero)
		{
			pos += Custom.DegToVec(BTWFunc.random * 360f) * (5f * Mathf.Pow(this.explodeCounter.fract, 3));
		}

		int textureCount = this.AVC.appearance.textureCount;
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
			if (i != textureCount)
			{
				sLeaser.sprites[i].x = pos.x - camPos.x;
				sLeaser.sprites[i].y = pos.y - camPos.y;
				sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), rot);
				sLeaser.sprites[i].color = this.baseColor;
			}
        }

		if (base.mode == Mode.Thrown)
		{
			sLeaser.sprites[textureCount].isVisible = true;
			Vector2 trail = Vector2.Lerp(this.tailPos, base.firstChunk.lastPos, timeStacker);
			Vector2 a = BTWFunc.PerpendicularVector((pos - trail).normalized);

			(sLeaser.sprites[textureCount] as TriangleMesh).MoveVertice(0, pos + a * 3f - camPos);
			(sLeaser.sprites[textureCount] as TriangleMesh).MoveVertice(1, pos - a * 3f - camPos);
			(sLeaser.sprites[textureCount] as TriangleMesh).MoveVertice(2, trail - camPos);

			sLeaser.sprites[textureCount].color = this.baseColor;
		}
		else
		{
			sLeaser.sprites[textureCount].isVisible = false;
		}
		
		bool doBlink = this.blink > 1 && BTWFunc.Chance(0.5f);
		for (int i = 0; i < textureCount; i++)
		{
			Vector2 coord = this.AVC.appearance.GetTextureCoordFromLinearCoord(textureCount - i - 1);
			float addedRot = this.AVC.appearance.layerRotation[(int)coord.x] * 90f;
			sLeaser.sprites[i].rotation += addedRot;

			float rotDir = sLeaser.sprites[i].rotation + coord.y * 90f + 45f;
			float diffFromSky = Mathf.Abs(rotDir - skyAngle)%360;
			if (diffFromSky > 180) { diffFromSky = 360 - diffFromSky; }
			diffFromSky /= 180f;

			if (doBlink)
			{
				sLeaser.sprites[i].color = this.blinkColor;
			}
			else
			{
				sLeaser.sprites[i].color = Color.Lerp(sLeaser.sprites[i].color, skyColor, BTWFunc.EaseIn(1 - diffFromSky, 8));
				sLeaser.sprites[i].color = Color.Lerp(sLeaser.sprites[i].color, explodeColor, Mathf.Pow(this.explodeCounter.fract, 3));
			}
		}
		sLeaser.sprites[textureCount + 1].color = Color.Lerp(this.baseColor, Color.white, 0.5f + this.explodeCounter.fract / 2f);
		sLeaser.sprites[textureCount + 1].scale = Mathf.Lerp(3f, Mathf.Min(30f, 3f + this.AVC.containedVoidEnergy * 2f), this.explodeCounter.fract);
		sLeaser.sprites[textureCount + 1].alpha = Mathf.Lerp(0.4f, 1f, this.explodeCounter.fract);

		if (base.slatedForDeletetion || this.room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		this.skyColor = Color.Lerp(palette.skyColor, Color.white, 0.5f);
	}

    public void VoidSparkAppears(VoidSpark voidSpark)
    {
		
    }

    public void HitByVoidSpark(VoidSpark voidSpark)
    {
		this.AVC.containedVoidEnergy += voidSpark.damage;
		if (this.AVC.containedVoidEnergy > 10f 
			&& BTWFunc.Chance(this.AVC.containedVoidEnergy / 100f)
			&& !this.ignited)
		{
			InitExplosion();
		}
    }

    public Color baseColor = new Color(1f, 0.2f, 0.5f);
    private Color skyColor = new Color(1f, 1f, 1f);
    private Color explodeColor = new Color(1f, 1f, 1f);
	public bool refreshTexture = false;
	public float scale = 0.7f;
	public const float skyAngle = -90;
	public const float transparency = 0.75f;

	public bool ignited = false;
	public bool exploded = false;
	public Counter explodeCounter = new(BTWFunc.FrameRate * 1);
	public Creature target;

    public AbstractVoidCrystal abstractVoidCrystal
    {
        get
        {
            return (AbstractVoidCrystal)this.abstractPhysicalObject;
        }
    }
    public AbstractVoidCrystal AVC
    {
        get
        {
            return this.abstractVoidCrystal;
        }
    }
	public class VoidCrystalFragment : CosmeticSprite // taken from SpearFragment
	{
		public VoidCrystalFragment(Vector2 pos, Vector2 vel) : this(pos, vel, null) {}
		public VoidCrystalFragment(Vector2 pos, Vector2 vel, VoidCrystal owner)
		{
			this.pos = pos + vel * 2f;
			this.lastPos = pos;
			this.vel = vel;
			this.owner = owner;
			this.rotation = BTWFunc.Random(360);
			this.lastRotation = this.rotation;
			this.rotVel = Mathf.Lerp(-26f, 26f, BTWFunc.random);
			if (this.owner != null)
			{
				this.baseColor = owner.baseColor;
			}
		}


		public override void Update(bool eu)
		{
			this.vel *= 0.999f;
			this.vel.y = this.vel.y - this.room.gravity * 0.9f;
			this.lastRotation = this.rotation;
			this.rotation += this.rotVel * this.vel.magnitude;
			if (Vector2.Distance(this.lastPos, this.pos) > 18f 
				&& this.room.GetTile(this.pos).Solid 
				&& !this.room.GetTile(this.lastPos).Solid)
			{
				IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(
						this.room, this.room.GetTilePosition(this.lastPos), this.room.GetTilePosition(this.pos));
				FloatRect floatRect = Custom.RectCollision(this.pos, this.lastPos, this.room.TileRect(intVector.Value).Grow(2f));
				this.pos = floatRect.GetCorner(FloatRect.CornerLabel.D);
				bool flag = false;
				if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
				{
					this.vel.x = Mathf.Abs(this.vel.x) * 0.5f;
					flag = true;
				}
				else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
				{
					this.vel.x = -Mathf.Abs(this.vel.x) * 0.5f;
					flag = true;
				}
				else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
				{
					this.vel.y = Mathf.Abs(this.vel.y) * 0.5f;
					flag = true;
				}
				else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
				{
					this.vel.y = -Mathf.Abs(this.vel.y) * 0.5f;
					flag = true;
				}
				if (flag)
				{
					this.rotVel *= 0.8f;
					this.rotVel += Mathf.Lerp(-1f, 1f, BTWFunc.random) * 4f * BTWFunc.random;
					this.room.PlaySound(SoundID.Spear_Fragment_Bounce, this.pos, 0.65f, BTWFunc.Random(2.2f, 2.4f) , (this.owner != null) ? this.owner.abstractPhysicalObject : null);
				}
			}
			if ((this.room.GetTile(this.pos).Solid && this.room.GetTile(this.lastPos).Solid) || this.pos.x < -100f)
			{
				this.Destroy();
			}
			base.Update(eu);
		}


		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite(AbstractVoidCrystal.VoidCrystalAppearance.GetRandomTextureName(), true)
            {
                scaleX = (BTWFunc.random < 0.5f) ? -1f : 1f,
                scaleY = (BTWFunc.random < 0.5f) ? -1f : 1f,
                color = baseColor,
                alpha = 0.5f
            };
            this.AddToContainer(sLeaser, rCam, null);
		}
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
			sLeaser.sprites[0].x = vector.x - camPos.x;
			sLeaser.sprites[0].y = vector.y - camPos.y;
			sLeaser.sprites[0].rotation = Mathf.Lerp(this.lastRotation, this.rotation, timeStacker);
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		}
		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
		public float rotation;
		public float lastRotation;
		public float rotVel;
		public VoidCrystal owner;
    	private Color baseColor = new Color(1f, 0.2f, 0.5f);
	}
}