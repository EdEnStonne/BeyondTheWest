using System.Collections.Generic;
using System.Linq;
using BeyondTheWest.MeadowCompat;
using BeyondTheWest.MSCCompat;
using RWCustom;
using UnityEngine;

namespace BeyondTheWest.Items;

public class Tristor : Weapon, ElectricExplosion.IReactToElectricExplosion
{
    public Tristor(AbstractTristor abstractTristor) : base(abstractTristor, abstractTristor.world)
    // Mostly taken from Rock code
    {
        this.bodyChunks = new BodyChunk[1];
		this.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 7f, 0.15f);
		this.bodyChunkConnections = new BodyChunkConnection[0];
		this.airFriction = 0.999f;
		this.g = 0.95f;
		this.bounce = 0.45f;
		this.surfaceFriction = 0.35f;
		this.collisionLayer = this.DefaultCollLayer;
		this.waterFriction = 0.97f;
		this.buoyancy = 0.2f;
		this.firstChunk.loudness = 15f;
		this.tailPos = this.firstChunk.pos;
		this.soundLoop = new ChunkDynamicSoundLoop(this.firstChunk);
		this.bounceCooldown.ResetUp();
		ChangeState(State.Idle);
    }
    public override bool HeavyWeapon => true;
    public override int DefaultCollLayer => 1;

	public override void Update(bool eu)
	{
		if (BTWFunc.IsLocal(this))
		{
			BounceDetect();
			if (this.mode == Mode.Thrown)
			{
				this.g = this.Charged ? 0.75f : 0.95f;
			}
			else
			{
				this.g = 0.95f;
			}
		}

		if (this.attractForce == null || this.attractForce.room == null || this.attractForce.room != this.room || this.attractForce.slatedForDeletetion)
		{
			CreateActractionForces();
		}

		if (BTWFunc.IsLocal(this))
		{
			UpdateState();
			// BTWPlugin.Log($"Tristor [{this.AT.ID}] updated to state [{this.state}]<{this.stateCounter.value}>. Forces : <{this.attractForce?.active}><{this.repulseForce?.active}><{this.centralForce?.active}>");
		}
		else
		{
			this.attractForce.active = false;
			this.repulseForce.active = false;
			this.centralForce.active = false;
		}
		UpdateStateColor();


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

		if (BTWFunc.IsLocal(this) && this.mode == Mode.Thrown && this.Charged && this.Submersion == 1f && !this.submergedCount.ended)
		{
			this.submergedCount.Tick();
			if (this.submergedCount.ended)
			{
				this.Explode();
			}
		}
		else
		{
			this.submergedCount.Reset();
		}

		this.lastVelocity = this.firstChunk.vel;
		this.lastPosition = this.firstChunk.pos;
	}
	
	public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
	{
		if (result.obj == null)
		{
			return false;
		}
		if (result.obj.abstractPhysicalObject.rippleLayer != this.abstractPhysicalObject.rippleLayer && !result.obj.abstractPhysicalObject.rippleBothSides && !this.abstractPhysicalObject.rippleBothSides)
		{
			return false;
		}
		if (this.thrownBy is Scavenger && (this.thrownBy as Scavenger).AI != null)
		{
			(this.thrownBy as Scavenger).AI.HitAnObjectWithWeapon(this, result.obj);
		}
		this.vibrate = 20;
		if (result.obj is Creature creature)
		{
			float stunBonus = 60f;
			if (ModManager.MMF && MoreSlugcats.MMF.cfgIncreaseStuns.Value 
                && (creature is Cicada 
                    || creature is LanternMouse 
                    || (ModManager.MSC && creature is MoreSlugcats.Yeek)))
			{
				stunBonus = 120f;
			}
			creature.Violence(base.firstChunk, 
                new Vector2?(base.firstChunk.vel * base.firstChunk.mass), 
                result.chunk, result.onAppendagePos, Creature.DamageType.Blunt, 0.025f, stunBonus);
			
			if (this.Charged)
			{
				Explode();
			}
		}
		else if (result.chunk != null)
		{
			result.chunk.vel += base.firstChunk.vel * base.firstChunk.mass / result.chunk.mass;
		}
		else if (result.onAppendagePos != null)
		{
			(result.obj as IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, base.firstChunk.vel * base.firstChunk.mass);
		}
		this.ChangeMode(Mode.Free);
		base.firstChunk.vel = base.firstChunk.vel * -0.5f + Custom.DegToVec(Random.value * 360f) * (Mathf.Lerp(0.1f, 0.4f, Random.value) * base.firstChunk.vel.magnitude);
		this.room.PlaySound(SoundID.Rock_Hit_Creature, base.firstChunk);
		if (result.chunk != null)
		{
			this.room.AddObject(new ExplosionSpikes(this.room, result.chunk.pos + BTWFunc.DirVec(result.chunk.pos, result.collisionPoint) * result.chunk.rad, 5, 2f, 4f, 4.5f, 30f, new Color(1f, 1f, 1f, 0.5f)));
		}
		this.SetRandomSpin();
		return true;
	}
	public override void Thrown(Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
	{
		base.Thrown(thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
		Room room = this.room;
		if (room == null)
		{
			return;
		}
		if (this.Charged)
		{
			this.throwModeFrames = -1;
			this.doNotTumbleAtLowSpeed = true;
			this.bounceCooldown.ResetUp();
		}
		room.PlaySound(SoundID.Slugcat_Throw_Rock, base.firstChunk);
	}
	public override void PickedUp(Creature upPicker)
	{
		this.room.PlaySound(SoundID.Slugcat_Pick_Up_Rock, base.firstChunk);
	}
    public override void HitWall()
    {
		base.HitWall();
		ElectricExplosion.MakeSparks(room, 20, this.firstChunk.pos, (byte)BTWFunc.RandInt(1, 4), this.coreColor);
		// BTWPlugin.Log($"Tristor [{this}] hit the wall !");
    }
	public override void HitByWeapon(Weapon weapon)
	{
		if (weapon.mode == Mode.Thrown && this.thrownBy == null && weapon.thrownBy != null)
		{
			this.thrownBy = weapon.thrownBy;
		}
		base.HitByWeapon(weapon);
		if (this.Charged && this.bounceCooldown.ended)
		{
			this.Explode();
			this.firstChunk.vel = this.firstChunk.vel * 2f 
				+ BTWFunc.RandomCircleVector(this.firstChunk.vel.magnitude);
			if (!this.Charged && this.collisionLayer == 1)
			{
				this.ChangeCollisionLayer(2);
			}
		}
	}
	public override void WeaponDeflect(Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
	{
		base.WeaponDeflect(inbetweenPos, deflectDir, bounceSpeed);
		if (this.Charged && this.bounceCooldown.ended)
		{
			bool weakExplode = BTWFunc.random > 0.5f;
			this.Explode(weakExplode);
			this.firstChunk.vel = this.firstChunk.vel * (weakExplode ? 1.1f : 1.5f) 
				+ BTWFunc.RandomCircleVector(this.firstChunk.vel.magnitude / (weakExplode ? 10f : 2f));
		}
	}
    public override void ChangeMode(Mode newMode)
    {
        base.ChangeMode(newMode);
		if (this.state != State.Idle)
		{
			ChangeState(State.Idle);
		}
    }
    public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        base.Collide(otherObject, myChunk, otherChunk);
		if (otherObject is Tristor otherTristor)
		{
			if (otherTristor.state == State.Colapse && this.state != State.Idle)
			{
				ChangeState(State.Colapse);
			}
			else if (!this.Inhert && !otherTristor.Inhert)
			{
				Vector2 push = (this.firstChunk.pos - otherTristor.firstChunk.pos).normalized + BTWFunc.RandomCircleVector(0.25f);
				Vector2 pushInv = -(this.firstChunk.pos - otherTristor.firstChunk.pos).normalized + BTWFunc.RandomCircleVector(0.25f);
				bool noProitiry = this.Positioning && otherTristor.Positioning;

				this.firstChunk.vel = this.Static ? 3f * push : 
					noProitiry || !this.Positioning ? 7f * push : 
					this.firstChunk.vel;

				otherTristor.firstChunk.vel = otherTristor.Static ? 3f * pushInv : 
					noProitiry || !otherTristor.Positioning ? 7f * pushInv : 
					otherTristor.firstChunk.vel;

				if (this.Searching) { this.stateCounter.Down(5); }
				if (otherTristor.Searching) { otherTristor.stateCounter.Down(5); }
				
				if (ModManager.MSC)
				{
					LightingArc lightingArc = new(
						this.firstChunk, otherTristor.firstChunk,
						BTWFunc.Random(0.3f, 0.5f), 
						BTWFunc.Random(0.3f, 0.5f), 
						BTWFunc.RandInt(5, 10), 
						this.coreColor
					);
					this.room.AddObject(lightingArc);
				}
				room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, 
					this.firstChunk.pos, 
					BTWFunc.Random(0.05f, 0.1f), 
					BTWFunc.Random(1.3f, 1.5f));
			}
		}
    }
    public override void NewRoom(Room newRoom)
    {
        base.NewRoom(newRoom);
		CreateActractionForces();
    }

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[6];
		
		FSprite Shard1 = new FSprite($"TristorShard{this.AT.shard1Type}", true)
        {
            color = Color.HSVToRGB(this.AT.mainHue, 0.5f, 1f),
            scale = this.scale,
			rotation =  this.AT.rotationOffset[0]
        };
        sLeaser.sprites[3] = Shard1;
        
		FSprite Shard2 = new FSprite($"TristorShard{this.AT.shard2Type}", true)
        {
            color = Color.HSVToRGB(this.AT.secHue, 0.5f, 1f),
            scale = this.scale,
			rotation =  this.AT.rotationOffset[1]
        };
        sLeaser.sprites[2] = Shard2;
        
		FSprite Rock = new FSprite($"TristorRock{this.AT.rockType}", true)
        {
            scale = this.scale
        };
        sLeaser.sprites[1] = Rock;
        
		FSprite Core = new FSprite($"TristorCore{this.AT.coreType}", true)
        {
            scale = this.scale,
            color = this.coreColor,
        };
        sLeaser.sprites[0] = Core;  

        TriangleMesh.Triangle[] trail = new TriangleMesh.Triangle[]
		{
			new(0, 1, 2)
		};
		TriangleMesh TrailMesh = new TriangleMesh("Futile_White", trail, false, false);
		sLeaser.sprites[4] = TrailMesh;

        sLeaser.sprites[5] = new FSprite("Futile_White", true)
        {
            shader = rCam.room.game.rainWorld.Shaders["LightSource"],
			scale = 3f,
            alpha = 0.5f
        };

		this.AddToContainer(sLeaser, rCam, null);
	}
    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        base.AddToContainer(sLeaser, rCam, newContatiner);
		rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[5]);
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
			pos += Custom.DegToVec(Random.value * 360f) * (2f * Random.value);
		}

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
			if (i != 4)
			{
				sLeaser.sprites[i].x = pos.x - camPos.x;
				sLeaser.sprites[i].y = pos.y - camPos.y;
				sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), rot);
			}
        }

		if (base.mode == Mode.Thrown) // guh a trail ??
		{
			sLeaser.sprites[4].isVisible = true;
			Vector2 trail = Vector2.Lerp(this.tailPos, base.firstChunk.lastPos, timeStacker);
			Vector2 a = BTWFunc.PerpendicularVector((pos - trail).normalized);

			(sLeaser.sprites[4] as TriangleMesh).MoveVertice(0, pos + a * 3f - camPos);
			(sLeaser.sprites[4] as TriangleMesh).MoveVertice(1, pos - a * 3f - camPos);
			(sLeaser.sprites[4] as TriangleMesh).MoveVertice(2, trail - camPos);
		
			if (this.Charged)
			{
				sLeaser.sprites[4].color = coreColor;
			}
			else
			{
				sLeaser.sprites[4].color = this.color;
			}
		}
		else
		{
			sLeaser.sprites[4].isVisible = false;
		}

		if (this.blink > 0)
		{
			if (this.blink > 1 && Random.value < 0.5f)
			{
				sLeaser.sprites[1].color = base.blinkColor;
			}
			else
			{
				sLeaser.sprites[1].color = this.color;
			}
		}
		else if (sLeaser.sprites[1].color != this.color)
		{
			sLeaser.sprites[1].color = this.color;
		}

		if (this.Charged)
		{
			this.shardGlowFract = Mathf.Lerp(this.shardGlowFract, 1, 0.002f);
		}
		else
		{
			this.shardGlowFract = Mathf.Lerp(this.shardGlowFract, 0, 0.001f);
		}
		sLeaser.sprites[3].color = Color.Lerp(Color.HSVToRGB(this.AT.mainHue, 0.5f, 0.2f), Color.HSVToRGB(this.AT.mainHue, 0.5f, 1f), this.shardGlowFract);
		sLeaser.sprites[2].color = Color.Lerp(Color.HSVToRGB(this.AT.mainHue, 0.5f, 0.2f), Color.HSVToRGB(this.AT.secHue, 0.5f, 1f), this.shardGlowFract);
		sLeaser.sprites[3].rotation += this.AT.rotationOffset[0];
		sLeaser.sprites[2].rotation += this.AT.rotationOffset[1];

        this.glowLoop = ++this.glowLoop % (BTWFunc.FrameRate * 600);
        Color glowColor;
        float glowLerp;
        if (this.Charged)
        {
            glowLerp = Mathf.Sin(1 / (BTWFunc.FrameRate * 5f) * Mathf.PI * 2f * this.glowLoop);
            glowColor = Color.Lerp(
                coreColor,
                Color.Lerp(coreColor, this.color, 0.5f),
                glowLerp
            );
        }
        else
        {
            glowLerp = Mathf.Abs(Mathf.Sin(1 / (BTWFunc.FrameRate * 60f) * Mathf.PI * 2f * this.glowLoop)) * 5f;
            glowColor = Color.Lerp(
                Color.Lerp(coreColor, this.color, 0.5f),
                this.color,
                Mathf.Clamp01(glowLerp)
            );
        }
        sLeaser.sprites[0].color = glowColor;
		sLeaser.sprites[5].x = pos.x - camPos.x;
		sLeaser.sprites[5].y = pos.y - camPos.y;
		sLeaser.sprites[5].alpha = Mathf.Clamp01(0.5f - glowLerp);
		sLeaser.sprites[5].color = glowColor;

		if (base.slatedForDeletetion || this.room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		this.color = palette.blackColor;
        sLeaser.sprites[3].color = Color.Lerp(sLeaser.sprites[3].color, palette.skyColor, 0.2f);
        sLeaser.sprites[2].color = Color.Lerp(sLeaser.sprites[2].color, palette.skyColor, 0.2f);
		sLeaser.sprites[1].color = this.color;
	}
	
	public void BounceDetect()
	{
		if (this.mode == Mode.Thrown && this.Charged)
		{
			if (this.bounceCooldown.ended)
			{
				bool bounce = false;
				bool skid = this.floorBounceFrames > 0;
				Vector2 originalPos = this.firstChunk.pos;
				IntVector2 slopeAngle = this.room.GetTile(originalPos).GetSlopeAngle(this.room);

				if (slopeAngle != new IntVector2(0, 0)
					&& (this.firstChunk.ContactPoint.y != 0
						|| this.firstChunk.ContactPoint.x != 0))
				{
					this.firstChunk.pos = this.lastPosition;
					this.firstChunk.vel = new Vector2(
						Mathf.Abs(this.lastVelocity.y) * slopeAngle.x,
						Mathf.Abs(this.lastVelocity.x) * slopeAngle.y);
					if (!skid)
					{
						this.firstChunk.vel += BTWFunc.RandomCircleVector(this.lastVelocity.magnitude / 2f);
					}
					this.firstChunk.vel = Mathf.Max(10f, Mathf.Abs(this.firstChunk.vel.magnitude) * 1.05f) 
						* this.firstChunk.vel.normalized;
					this.firstChunk.contactPoint.y = 0;
					this.firstChunk.contactPoint.x = 0;
					this.throwDir = new IntVector2(
						Mathf.Abs(this.throwDir.y) * slopeAngle.x,
						Mathf.Abs(this.throwDir.x) * slopeAngle.y);
					bounce = true;
				}
				if (this.firstChunk.ContactPoint.y != 0)
				{
					int dir = -this.firstChunk.ContactPoint.y;
					this.firstChunk.pos = this.lastPosition;
					this.firstChunk.vel = this.lastVelocity;
					if (!skid)
					{
						this.firstChunk.vel += BTWFunc.RandomCircleVector(this.lastVelocity.magnitude / 2f);
					}
					this.firstChunk.vel.y = Mathf.Max(15f, Mathf.Abs(this.firstChunk.vel.y) * 1.2f) * dir;
					this.firstChunk.contactPoint.y = 0;
					this.throwDir.y *= -1;
					this.rotationSpeed = -this.rotationSpeed;
					bounce = true;
				}
				if (this.firstChunk.ContactPoint.x != 0)
				{
					int dir = -this.firstChunk.ContactPoint.x;
					this.firstChunk.pos = this.lastPosition;
					this.firstChunk.vel = this.lastVelocity;
					if (!skid)
					{
						this.firstChunk.vel += BTWFunc.RandomCircleVector(this.lastVelocity.magnitude / 2f);
					}
					this.firstChunk.vel.x = Mathf.Max(15f, Mathf.Abs(this.firstChunk.vel.x) * 1.2f) * dir;
					this.firstChunk.contactPoint.x = 0;
					this.throwDir.x *= -1;
					this.rotationSpeed = -this.rotationSpeed;
					bounce = true;
				}

				if (bounce)
				{
					this.thrownPos = this.firstChunk.pos;
					this.setRotation = new Vector2?(this.throwDir.ToVector2());
					this.room.PlaySound(SoundID.Weapon_Skid, this.firstChunk, false, 0.75f, BTWFunc.Random(1.1f, 1.3f));
					Explode(skid);
					if (ModManager.MSC)
					{
						LightingArc lightingArc = new(
							this.firstChunk, originalPos, skid ? 0.25f : 0.5f, skid ? 0.5f : 0.75f, BTWFunc.RandInt(3, 7), this.coreColor
						);
						this.room.AddObject(lightingArc);
						if (BTWPlugin.meadowEnabled)
						{
							MeadowCalls.MSCCompat_RPCSyncLightnightArc(lightingArc);
						}
					}
					
					// BTWPlugin.Log($"Tristor [{this}] bounced !");
				}
			}
			else if (this.firstChunk.ContactPoint.y != 0 || this.firstChunk.ContactPoint.x != 0)
			{
				this.doNotTumbleAtLowSpeed = false;
			}
		}
		this.bounceCooldown.Tick();
	}
	public void Explode(bool chargeless = false)
	{
		this.vibrate = 20;
		this.bounceCooldown.Reset();

		float reach = 60f;
		float force = 10f;
		float damage = 1.25f;
		float stun = 3f * BTWFunc.FrameRate;
		bool submerged = this.Submersion == 1f;

		if (submerged)
		{
			this.ChangeMode(Mode.Free);
			chargeless = false;
		}
		if (!chargeless)
		{
			this.AT.charge = Mathf.Max(0, this.AT.charge - 1);
			reach = submerged ? 250f : 120f;
			force = submerged ? 30f : 20f;
			stun = submerged ? 8f * BTWFunc.FrameRate : 5f * BTWFunc.FrameRate;
			damage = 1.9f;
		}

		ElectricExplosion electricExplosion = new(room, this, this.firstChunk.pos, 3, reach, force,
                damage, stun, this.thrownBy, chargeless ? 0 : 0.7f, 
				0.9f, false, true, true)
		{
			color = color,
			volume = 1f,
			hitNonSubmerged = !submerged
		};
        this.room.AddObject( electricExplosion );
		this.shardGlowFract = 0.5f;

		if (!this.Charged)
		{
			this.doNotTumbleAtLowSpeed = false;
		}
	}
	public void UpdateState()
	{
		if (this.room != null)
		{
			Vector2 position = this.firstChunk.pos;
			Vector2 velocity = this.firstChunk.vel;
			if (this.state == State.Colapse)
			{
				this.attractForce.active = false;
				this.repulseForce.active = false;
				this.centralForce.active = false;

				this.stateCounter.Tick();
				if (this.stateCounter.ended && this.mode == Mode.Free)
				{
					ChangeState(State.Idle);
				}
			}

			else if (this.state == State.Idle)
			{
				if (this.mode == Mode.Free && velocity.magnitude < 2f)
				{
					this.stateCounter.Tick();
				}
				else
				{
					this.stateCounter.Reset();
				}

				this.attractForce.active = false;
				this.repulseForce.active = this.mode == Mode.Free;
				this.repulseForce.attractionForce = -80000f;
				this.repulseForce.maxRadius = 50f;
				this.centralForce.active = false;

				if (this.stateCounter.ended && this.mode == Mode.Free)
				{
					ChangeState(State.Searching);
				}
			}

			else if (this.state == State.Searching)
			{
				bool tristorNear = false; 
				bool aboveAvalable = false;
				float closestTristorDist = float.MaxValue;
				IntVector2 nearestTristorTile = new();

				this.attractForce.active = true;
				this.repulseForce.active = true;
				this.repulseForce.attractionForce = -100000f;
				this.repulseForce.maxRadius = foundRadius - 5f;
				this.centralForce.active = false;
				
				
				for (int i = 0; i < this.attractForce.chunksAffected.Count; i++)
				{
                    if (this.attractForce.chunksAffected[i].owner is not Tristor otherTristor) 
					{ 
						BTWPlugin.LogError($"[{this}] Tried to attract to [{this.attractForce.chunksAffected[i].owner}] !"); 
						continue; 
					}
                    Vector2 tristorPos = otherTristor.firstChunk.pos;
					float distance = (position - tristorPos).magnitude;

					if (distance < closestTristorDist)
					{
						closestTristorDist = distance;
						nearestTristorTile = this.room.GetTilePosition(tristorPos);
						if (distance < foundRadius && otherTristor.state != State.Positioning)
						{
							tristorNear = true;
							aboveAvalable = otherTristor.state == State.Static;
						}
					}
				}

				if (tristorNear)
				{
					this.stateCounter.Tick();
				}
				else
				{
					this.stateCounter.Reset();
				}
				this.g = 0.25f * this.stateCounter.fractInv;

				if (this.stateCounter.ended && this.mode == Mode.Free)
				{
					this.idealPosition = aboveAvalable && BTWFunc.Chance(0.65f)  ? 
						nearestTristorTile + new IntVector2(0, 1) : 
						this.room.GetTilePosition(this.firstChunk.pos);
					
					if (!this.room.GetTile(this.idealPosition).IsAir())
					{
						this.idealPosition = this.room.GetTilePosition(this.firstChunk.pos);
					}

					ChangeState(State.Positioning);
				}
			}

			else if (this.state == State.Positioning || this.state == State.Static)
			{
				if (!this.room.GetTile(this.idealPosition).IsAir())
				{
					ChangeState(State.Colapse);
					BTWPlugin.Log($"Tristor <{this.AT.ID}> was trying to move to [{this.idealPosition}], which is [{this.room.GetTile(this.idealPosition).Terrain}] ! Collapsing...");
				}

				this.g = 0f;

				this.attractForce.active = false;
				this.repulseForce.active = true;
				this.repulseForce.attractionForce = -100000f;
				this.repulseForce.maxRadius = 15f;
				this.centralForce.active = true;
				this.centralForce.center = this.room.MiddleOfTile(this.idealPosition);

				if (this.state == State.Positioning)
				{
					this.stateLoop = 0;
					this.targetRotation = 0;
				}
				else
				{
        			this.stateLoop = ++this.stateLoop % (BTWFunc.FrameRate * 600);
					this.centralForce.center.y += 2f * Mathf.Sin(1 / (BTWFunc.FrameRate * 5f) * Mathf.PI * 2f * this.stateLoop);
					if (this.stateLoop % (BTWFunc.FrameRate * 30) == 0)
					{
						this.targetRotation = 90 * BTWFunc.RandInt(0, 3);
					}
				}
				float angle = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), this.rotation);
				this.rotationSpeed += (angle - this.targetRotation) * 0.01f;
				this.rotationSpeed *= this.Positioning ? 0.95f : 0.5f;

				float distance = (position - this.room.MiddleOfTile(this.idealPosition)).magnitude; // still has to use that cause force didn't update yet
				bool collapsing = false;

				if (this.state == State.Static && distance > collapseRadius)
				{
					collapsing = true;
				}

				var neighboorList = BTWFunc.GetAllObjects(this.room).FindAll(
					x => x is Tristor othertristor 
					&& othertristor != this
					&& othertristor.mode == Mode.Free
					&& (x.firstChunk.pos - position).magnitude < searchRadius);
				if (neighboorList.Count == 0)
				{
					collapsing = true;
				}
				else
				{
					for (int i = 0; i < neighboorList.Count; i++)
					{
						Tristor tristor = neighboorList[i] as Tristor;
						if (this.state == State.Positioning)
						{
							if (tristor.idealPosition == this.idealPosition)
							{
								int dir = BTWFunc.random < 0.5f ? 1 : -1;
								if (this.room.GetTile(this.idealPosition + new IntVector2(0, 1)).IsAir())
								{
									tristor.idealPosition = this.idealPosition + new IntVector2(0, 1);
								}
								else if (this.room.GetTile(this.idealPosition + new IntVector2(dir, 0)).IsAir())
								{
									tristor.idealPosition = this.idealPosition + new IntVector2(dir, 0);
								}
								else if (this.room.GetTile(this.idealPosition + new IntVector2(-dir, 0)).IsAir())
								{
									tristor.idealPosition = this.idealPosition + new IntVector2(-dir, 0);
								}
								else 
								{
									tristor.idealPosition = this.idealPosition + new IntVector2(0, -1);
								}
								tristor.ChangeState(State.Positioning);
							}
						}
						else if (tristor.state == State.Static)
						{
							if (BTWFunc.random < 0.005f)
							{
								bool recharge = false;
								this.shardGlowFract = 1f;
								tristor.shardGlowFract = 1f;

								if (tristor.AT.charge != this.AT.charge
									&& BTWFunc.random < 0.10f)
								{
									recharge = true;
									if (tristor.AT.charge < this.AT.charge)
									{
										tristor.AT.charge++;
									}
									else
									{
										this.AT.charge++;
									}
								}

								if (ModManager.MSC)
								{
									LightingArc lightingArc = new(
										this.firstChunk, tristor.firstChunk,
										recharge ? 0.75f : BTWFunc.Random(0.3f, 0.5f), 
										recharge ? 0.75f : BTWFunc.Random(0.3f, 0.5f), 
										recharge ? BTWFunc.FrameRate : BTWFunc.RandInt(5, 15), 
										this.coreColor
									);
									this.room.AddObject(lightingArc);
									if (BTWPlugin.meadowEnabled)
									{
										MeadowCalls.MSCCompat_RPCSyncLightnightArc(lightingArc);
									}
								}
								room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, 
									this.firstChunk.pos, 
									recharge ? 0.5f : BTWFunc.Random(0.05f, 0.15f), 
									BTWFunc.Random(1.3f, 1.5f));
							}
						}
					}
				}
				

				if (collapsing)
				{
					this.stateCounter.Tick();
				}
				else
				{
					this.stateCounter.Reset();
				}

				if (this.state == State.Positioning 
					&& distance < 1f 
					&& this.firstChunk.vel.magnitude < 0.5f
					&& !collapsing)
				{
					ChangeState(State.Static);
				}
				if (this.stateCounter.ended && this.mode == Mode.Free)
				{
					ChangeState(State.Colapse);
				}
			}
		}
	}
	public void UpdateStateColor()
	{
		if (this.state == State.Colapse)
		{
			coreColor = new(1f, 0.5f, 0.4f);
		}
		else if (this.state == State.Idle)
		{
			coreColor = new(0.4f, 0.8f, 1f);
		}
		else if (this.state == State.Searching)
		{
			this.coreColor = new(0.4f, 1f, 0.8f);
		}
		else if (this.state == State.Positioning)
		{
			this.coreColor = new(1f, 1f, 0.4f);	
		}
		else if (this.state == State.Static)
		{
			this.coreColor = new(0.6f, 0.4f, 1f);
		}
	}
	public void ChangeState(State newState)
	{
		this.stateLoop = 0;
		if (newState == State.Idle)
		{
			this.stateCounter.Reset(BTWFunc.FrameRate * 10);
		}
		else if (newState == State.Searching)
		{
			this.stateCounter.Reset(BTWFunc.FrameRate * 3);
		}
		else if (newState == State.Positioning)
		{
			this.stateCounter.Reset(10);
		}
		else if (newState == State.Static)
		{
			this.stateCounter.Reset(BTWFunc.FrameRate * 1);
		}
		else if (newState == State.Colapse)
		{
			this.stateCounter.Reset(BTWFunc.FrameRate * 2);
		}
		this.state = newState;
	}
	public void DestroyForces()
	{
		this.attractForce?.Destroy();
		this.repulseForce?.Destroy();
		this.centralForce?.Destroy();
	}
	public void CreateActractionForces()
	{
		DestroyForces();
		this.attractForce = new(this);
		this.room?.AddObject(this.attractForce);

		this.repulseForce = new(this);
		this.room?.AddObject(this.repulseForce);

		this.centralForce = new(this);
		this.room?.AddObject(this.centralForce);

		// BTWPlugin.Log($"Tristor [{this.AT.ID}] created forces [{this.attractForce}][{this.repulseForce}][{this.centralForce}] in room <{this.room}> !");
	}

	public void Explosion(ElectricExplosion electricExplosion)
    {
        if (electricExplosion.sourceObject != this 
			&& electricExplosion.maxDamage > BTWFunc.Random(0.7f, 1f)
			&& !this.grabbedBy.Exists(x => x.grabber == electricExplosion.sourceObject)
			&& (electricExplosion.killTagHolder == null
				|| !this.grabbedBy.Exists(x => x.grabber == electricExplosion.killTagHolder))
			&& this.Charged
			&& this.bounceCooldown.ended
			&& this.mode == Mode.Free)
		{
			Vector2 dir = this.firstChunk.pos - electricExplosion.pos;
			float distance = Mathf.Max(0, dir.magnitude - this.firstChunk.rad);
			float ratioDist = distance/electricExplosion.rad;
			if (ratioDist <= 1f)
			{
				this.ChangeState(State.Colapse);
				if (BTWFunc.random < ratioDist)
				{
					this.firstChunk.vel = dir.normalized * 20f + BTWFunc.RandomCircleVector(10f);
					if (-this.firstChunk.ContactPoint.y * this.firstChunk.vel.y < 0)
					{
						this.firstChunk.vel.y *= -1;
					}
					if (-this.firstChunk.ContactPoint.x * this.firstChunk.vel.x < 0)
					{
						this.firstChunk.vel.x *= -1;
					}
					this.Explode(true);
					this.thrownBy = null;
					this.thrownPos = this.firstChunk.pos;
					this.throwDir = new IntVector2((int)Mathf.Sign(this.firstChunk.vel.x), (int)Mathf.Sign(this.firstChunk.vel.y));
					this.changeDirCounter = 3;
					this.ChangeOverlap(true);
					this.ChangeMode(Mode.Thrown);
					this.setRotation = new Vector2?(throwDir.ToVector2());
					this.rotationSpeed = 0f;
					this.meleeHitChunk = null;
					if (this.Charged)
					{
						this.throwModeFrames = -1;
						this.doNotTumbleAtLowSpeed = true;
					}
				}
			}
		}
    }

    private Vector2 lastVelocity = Vector2.zero;
	private Vector2 lastPosition = Vector2.zero;
	private Counter bounceCooldown = new(3);

	private Counter submergedCount = new((int)(BTWFunc.FrameRate * 0.5));
	
    private int glowLoop;
    private readonly float scale = 0.75f;
    private Color coreColor = new(0.4f, 0.8f, 1f);
	public bool refreshTexture = false;
	public float shardGlowFract = 1f;

	public State state;
	public Counter stateCounter = new(BTWFunc.FrameRate * 10);
	public IntVector2 idealPosition;
    private int stateLoop;
	public float targetRotation;
	public TristorCentralForce centralForce;
	public TristorAttractForce attractForce;
	public TristorRepulseForce repulseForce;
	private const float searchRadius = 60f;
	private const float foundRadius = 40f;
	private const float collapseRadius = 15f;

	public bool Inhert => this.state == State.Idle || this.state == State.Colapse;
	public bool Localized => this.state == State.Positioning || this.state == State.Static;
	public bool Positioning => this.state == State.Positioning;
	public bool Static => this.state == State.Static;
	public bool Searching => this.state == State.Searching;

	public bool Charged => this.AT.Charged;
    public AbstractTristor AbstractTristor
    {
        get
        {
            return (AbstractTristor)this.abstractPhysicalObject;
        }
    }
    public AbstractTristor AT
    {
        get
        {
            return this.AbstractTristor;
        }
    }
	public class State : ExtEnum<Tristor.State>
	{
		public State(string value, bool register = false) : base(value, register) { }
		public static State Idle;
		public static State Searching;
		public static State Positioning;
		public static State Static;
		public static State Colapse;
	}

	public class TristorAttractForce : AttractiveObjectForce
	{
		public TristorAttractForce(Tristor tristor)
			: base(tristor.firstChunk, 500f, 0.25f,
				x => x.owner is Tristor othertristor 
					&& othertristor != tristor
					&& !othertristor.Inhert 
					&& othertristor.mode == Mode.Free )
		{
			this.tristor = tristor;
			this.active = false;
		}
		
		public Tristor tristor;
	}
	public class TristorRepulseForce : AttractiveObjectForce
	{
		public TristorRepulseForce(Tristor tristor)
			: base(tristor.firstChunk, -4000f, 1f, 50f,
				x => x.owner is Tristor othertristor 
					&& othertristor != tristor
					&& othertristor.mode == Mode.Free)
		{
			this.invertDistancePower = 4f;
			this.tristor = tristor;
			this.active = false;
		}

        // public override void Update(bool eu)
        // {
        //     base.Update(eu);
		// 	BTWPlugin.Log($"Force of Tristor [{tristor?.AT?.ID}] updated and found <{this.chunksAffected?.Count}> object affected ! <{this.active}><{this.attractionForce}><{this.maxRadius}><{this.maxForce}>");
        // }

		public Tristor tristor;
	}
	public class TristorCentralForce : AttractiveCentralForce
	{
		public TristorCentralForce(Tristor tristor)
			: base(tristor.firstChunk, tristor.firstChunk.pos, 150f, 0.1f)
		{
			this.tristor = tristor;
			this.active = false;
		}
		

        public override void Update(bool eu)
        {
            base.Update(eu);
			if (this.active)
			{
				this.attractedChunk.vel *= tristor.Positioning ? 0.90f : 0.65f;
			}
        }
		public Tristor tristor;
	}
}