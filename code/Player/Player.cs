using Sandbox;
using Sandbox.Component;
using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata;

namespace ZombieHorde;

public partial class HumanPlayer : Player, IUse
{
	public TimeSince timeSinceDropped;

	[Net]
	public float MaxHealth { get; set; } = 100;
	[Net]
	public int RevivesRemaining { get; set; }

	public bool SupressPickupNotices { get; private set; }

	public TimeSince TimeSinceLastKill { get; set; }
	private TimeSince TimeSincePassiveHealed = 0;

	[Net, Predicted]
	public Angles ViewPunchOffset { get; set; } = Angles.Zero;
	[Net, Predicted]
	public Angles ViewPunchVelocity { get; set; } = Angles.Zero;
	[Net, Predicted]
	public TimeUntil TimeUntilAdrenalineExpires { get; set; } = 0;

	public HumanPlayer()
	{
		Inventory = new ZomInventory( this );
	}

	public override void Respawn()
	{
		SetModel( "models/human/citizen_human.vmdl" );

		// need to set owner for camera shake
		Controller = new HumanWalkController();
		(Controller as BaseZomWalkController).Owner = this;

		Animator = new HumanPlayerAnimator();

		CameraMode = new ZomFirstPersonCamera();

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Clothing.DressEntity( this );

		SupressPickupNotices = true;

		Inventory.DeleteContents();

		//Inventory.Add( new Pistol(), true );
		Inventory.Add( new M1911(), true );

		SupressPickupNotices = false;
		Health = 100;
		RevivesRemaining = BaseGamemode.Current.HumanMaxRevives;

		SetAnimParameter( "sit", 0 );
		HealthBar.RefreshAvatar( To.Single(Client) );

		base.Respawn();
	}

	public void SpawnAsSpectator()
	{
		SetModel( "models/human/citizen_human.vmdl" );

		Animator = new HumanPlayerAnimator();

		CameraMode = new SpectatePlayerCamera();

		EnableAllCollisions = false;
		EnableDrawing = false;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Health = 100;
		HealthBar.RefreshAvatar( To.Single( Client ) );

		base.Respawn();
		LifeState = LifeState.Dead;
	}

	[ConCmd.Admin]
	public static void GiveAll()
	{
		var ply = ConsoleSystem.Caller.Pawn as HumanPlayer;

		ply.Inventory.Add( new F1() );
		ply.Inventory.Add( new R870() );
		ply.Inventory.Add( new PipeBomb() );
		ply.Inventory.Add( new MedKit() );
		ply.Inventory.Add( new HealthSyringe() );
	}

	[ConCmd.Admin]
	public static void ent_create( string entity )
	{
		var ply = ConsoleSystem.Caller.Pawn as HumanPlayer;

		Type type = Type.GetType( entity );
		var ent = TypeLibrary.Create( entity, type ) as Entity;

		var tr = Trace.Ray( ply.EyePosition, ply.EyePosition + ply.EyeRotation.Forward * 5000 )
					.UseHitboxes()
					.WithAnyTags( "solid", "player", "npc" )
					.Ignore( ply )
					.Size( 2 );

		ent.Position = tr.Run().HitPosition + Vector3.Up * 10;

		//var prize = TypeLibrary.Create( lootTable[index], t ) as Entity;
		//prize.Position = Position;
	}

	[ConCmd.Admin]
	public static void SetHealth(float health)
	{
		var ply = ConsoleSystem.Caller.Pawn as HumanPlayer;

		ply.Health = health;
	}

	public override void OnKilled()
	{
		base.OnKilled();

		ZomChatBox.AddInformation( To.Everyone, $"{Client.Name} died!", $"avatar:{Client.PlayerId}", "#FF0000" );

		DropInventory();

		if ( LastDamage.Flags.HasFlag( DamageFlags.Blast ) )
		{
			using ( Prediction.Off() )
			{
				var particles = Particles.Create( "particles/gib.vpcf" );
				if ( particles != null )
				{
					particles.SetPosition( 0, Position + Vector3.Up * 40 );
				}
			}
		}
		else
		{
			BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex ) );
		}

		Controller = null;

		CameraMode = new SpectateRagdollCamera();
		SetSpectatorCamera( 8000 );

		EnableAllCollisions = false;
		EnableDrawing = false;

		foreach ( var child in Children.OfType<ModelEntity>() )
		{
			child.EnableDrawing = false;
		}
	}

	public void DropInventory( bool dropPistol = false )
	{
		foreach ( var item in Children.ToList() )
		{
			if ( item is BaseZomWeapon )
			{
				if ( item is not M1911 || dropPistol )
					Inventory.Drop( item );
			}
		}

		Inventory.DeleteContents();
	}

	[ClientRpc]
	public async void SetSpectatorCamera(int delay )
	{
		await Task.Delay( delay );
		if(LifeState == LifeState.Dead)
		{
			//CameraMode = new SpectateFreeCamera();
			CameraMode = new SpectatePlayerCamera();
		}
	}

	[Event.Hotload]
	public void OnHotloaded()
	{
		if ( LifeState == LifeState.Dead )
		{
			CameraMode = new SpectatePlayerCamera();
		}
	}

	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );
		FrameUpdateViewOffset();
	}

	public override void Simulate( Client cl )
	{
		UpdateViewOffset();

		if ( LifeState == LifeState.Dead )
		{
			if ( CameraMode is SpectatePlayerCamera cam )
			{
				if(cam.SpectateTarget != null)
					Health = cam.SpectateTarget.Health;
			}

			if ( IsServer && BaseGamemode.Current.EnableRespawning())
			{
				Respawn();
			}

			return;
		}

		base.Simulate( cl );

		//
		// Input requested a weapon switch
		//
		if ( Input.ActiveChild != null )
		{
			ActiveChild = Input.ActiveChild;
		}

		TickPlayerUse();
		TickFlashlight();
		NudgeNearbyPlayers();

		if ( Input.Pressed( InputButton.View ) )
		{
			if ( CameraMode is ZomThirdPersonCamera )
			{
				CameraMode = new ZomFirstPersonCamera();
			}
			else
			{
				CameraMode = new ZomThirdPersonCamera();
			}
		}

		if ( Input.Pressed( InputButton.Menu ) )
		{
			TryPing();
		}

		if ( Input.Pressed( InputButton.Drop ) )
		{
			DropActive();
		}

		SimulateActiveChild( cl, ActiveChild );
		TickHeartBeat();
		TickAbilities();

		//passively heal up to 20 hp or take damage while incapacitated
		if ( Host.IsServer )
		{
			if ( LifeState == LifeState.Alive )
			{
				if ( Health < 20 )
				{
					if ( TimeSincePassiveHealed > 1f )
					{
						TimeSincePassiveHealed = 0;
						Health += 1;
					}
				}
			}
			else if (LifeState == LifeState.Dying )
			{
				if ( TimeSincePassiveHealed > .5f )
				{
					TimeSincePassiveHealed = 0;
					Health -= 1;
					if ( Health < 0 )
						OnKilled();
				}
			}
		}

		// kill the player if they fall out of the map somehow
		if ( IsServer )
		{
			if ( Position.z < -20000 )
				OnKilled();
		}
	}

	public void TickAbilities()
	{
		// adrenaline
		if(TimeUntilAdrenalineExpires > 0 )
		{
			//DebugOverlay.ScreenText( TimeUntilAdrenalineExpires.ToString(), 13 );
		}
	}

	private TimeSince TimeSincePinged = 0;

	public void TryPing()
	{
		if ( TimeSincePinged < .5f ) return;
		TimeSincePinged = 0;

		var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 5000 ).Ignore( this ).WithoutTags("trigger", "gib").Radius( 2 ).Run();
		var pos = tr.EndPosition + Vector3.Up * 10;
		var type = PingType.Generic;
		var time = 5;
		var message = "Ping!";
		Entity pingEntity = null;

		// do another trace if we hit the ground, check for nearby weapons.
		if(tr.Entity is WorldEntity)
			tr = Trace.Ray( tr.EndPosition, tr.EndPosition + Vector3.Up ).Ignore( this ).WithTag("weapon").Radius( 30 ).Run();
		//DebugOverlay.TraceResult( tr, 3 );
		if ( tr.Entity != null )
		{
			var ent = tr.Entity;
			if(ent is BaseZomWeapon wep)
			{
				type = PingType.Item;
				message = DisplayInfo.For( ent ).Name;
				pos = ent.Position + Vector3.Up * 5;
				time = 10;
				// sometimes the ping doesn't work if I set the parent??
				//pingEntity = ent;
			}
		}
		else
		{
			// no weapons nearby, check for other entities
			tr = Trace.Ray( tr.EndPosition, tr.EndPosition + Vector3.Up ).Ignore( this ).WithoutTags( "trigger", "gib" ).EntitiesOnly().Radius( 30 ).Run();
		}

		if ( tr.Entity != null )
		{
			var ent = tr.Entity;
			if ( ent is HealthKit )
			{
				type = PingType.Item;
				message = "Health Kit";
				pos = ent.Position + Vector3.Up * 5;
				time = 10;
				//pingEntity = ent;
			}
			else if ( ent is AmmoPile )
			{
				type = PingType.Item;
				message = "Ammo";
				pos = ent.Position + Vector3.Up * 5;
				time = 10;
				//pingEntity = ent;
			}
			else if ( ent is LootBox )
			{
				type = PingType.Item;
				message = "Treasure!";
				//pos = ent.Position;
				time = 300;
				pingEntity = ent;
			}
			else if ( ent is UncommonZombie )
			{
				type = PingType.Danger;
				message = "Armored Zombie!";
				//pos = ent.Position;;
				//pingEntity = ent;
			}
			else if ( ent is CommonZombie )
			{
				type = PingType.Danger;
				message = "Zombie!";
				//pos = ent.Position;;
				//pingEntity = ent;
			}
		}

		PingMarker.Ping( To.Everyone, pos, type, message, time, pingEntity );
	}

	TimeSince TimeSinceHeartBeat = 0;
	public void TickHeartBeat()
	{
		if ( IsServer ) return;
		if ( Health > 19 ) return;

		var time = 0.4f + Health/80;
		if(TimeSinceHeartBeat > time )
		{
			TimeSinceHeartBeat = 0;
			var snd = PlaySound( "human.heartbeat" );
			snd.SetVolume( 1.25f - Health / 20 );
			//snd.SetPitch( 1.1f );
		}
	}

	public void SwitchToBestWeapon()
	{
		var best = Children.Select( x => x as BaseZomWeapon )
			.Where( x => x.IsValid() && x.IsUsable() )
			//.OrderByDescending( x => x.BucketWeight )
			.FirstOrDefault();

		if ( best == null ) return;

		// let's not do this yet
		ActiveChild = best;
	}

	public void DropActive()
	{
		var dropped = Inventory.DropActive();
		if ( dropped != null )
		{
			if ( dropped.PhysicsGroup != null )
			{
				// do a trace to check if we're throwing the gun through a wall/floor
				var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 64 )
				.UseHitboxes()
				.WorldOnly()
				.Ignore( this )
				.Size( 8 );

				var hit = tr.Run().Hit;
				if ( hit )
				{
					Log.Info( hit );
					dropped.Position = EyePosition + Vector3.Down * 16;
					dropped.Rotation = EyeRotation * Rotation.FromYaw( 90 );
				}

				dropped.PhysicsGroup.Velocity = Velocity + (EyeRotation.Forward + EyeRotation.Up) * 200;
			}

			timeSinceDropped = 0;
			SwitchToBestWeapon();
		}
	}

	public IEnumerable<Entity> TouchingEntities => touchingEntities;
	public int TouchingEntityCount => touchingEntities.Count;

	readonly List<Entity> touchingEntities = new();

	public override void StartTouch( Entity other )
	{
		if(other is HumanPlayer || other is CommonZombie ) // only list humans & zombies for now. not sure if we'll need to list other entities in the future.
			AddToucher( other );

		if ( timeSinceDropped < 1 ) return;

		base.StartTouch( other );
	}

	public override void EndTouch( Entity other )
	{
		base.EndTouch( other );

		if ( other.IsWorld )
			return;

		if ( touchingEntities.Contains( other ) )
		{
			touchingEntities.Remove( other );
		}
	}

	protected void AddToucher( Entity toucher )
	{
		if ( !toucher.IsValid() )
			return;

		if ( touchingEntities.Contains( toucher ) )
			return;

		touchingEntities.Add( toucher );
	}

	public void NudgeNearbyPlayers()
	{
		foreach ( var ply in TouchingEntities.OfType<HumanPlayer>() )
		{
			if ( !ply.IsValid() )
				continue;

			Velocity += (Position - ply.Position).WithZ(0).Normal * 20;
		}

		if ( IsServer )
		{
			foreach ( var zom in TouchingEntities.OfType<CommonZombie>() )
			{
				if ( !zom.IsValid() )
					continue;

				zom.Velocity += (zom.Position - Position).WithZ( 0 ).Normal * 10;
			}
		}
	}

	public override void PostCameraSetup( ref CameraSetup setup )
	{
		setup.FieldOfView = 90; // hack: force fov to 90 to prevent shooting in the wrong spot
		base.PostCameraSetup( ref setup );

		if ( setup.Viewer != null )
		{
			AddCameraEffects( ref setup );
		}
	}

	float walkBob = 0;
	float lean = 0;
	float fov = 0;

	private void AddCameraEffects( ref CameraSetup setup )
	{
		var speed = Velocity.Length.LerpInverse( 0, 320 );
		var forwardspeed = Velocity.Normal.Dot( setup.Rotation.Forward );

		var left = setup.Rotation.Left;
		var up = setup.Rotation.Up;

		if ( GroundEntity != null )
		{
			walkBob += Time.Delta * 25.0f * speed;
		}

		setup.Position += up * MathF.Sin( walkBob ) * speed * 2;
		setup.Position += left * MathF.Sin( walkBob * 0.6f ) * speed * 1;

		// Camera lean
		lean = lean.LerpTo( Velocity.Dot( setup.Rotation.Right ) * 0.01f, Time.Delta * 15.0f );

		var appliedLean = lean;
		appliedLean += MathF.Sin( walkBob ) * speed * 0.3f;
		setup.Rotation *= Rotation.From( 0, 0, appliedLean );

		speed = (speed - 0.7f).Clamp( 0, 1 ) * 3.0f;

		fov = fov.LerpTo( speed * 20 * MathF.Abs( forwardspeed ), Time.Delta * 4.0f );

		setup.FieldOfView += fov;
	}

	public void ViewPunch(Angles angles)
	{
		ViewPunchVelocity += angles;
	}

	public void ViewPunch( float pitch, float yaw = 0, float roll = 0 )
	{
		ViewPunchVelocity += new Angles(pitch, yaw, roll);
	}

	public void UpdateViewOffset()
	{
		ViewPunchOffset += ViewPunchVelocity;
		ViewPunchOffset = Angles.Lerp( ViewPunchOffset, Angles.Zero, Time.Delta * 8f );
		ViewPunchVelocity = Angles.Lerp( ViewPunchVelocity, Angles.Zero, Time.Delta * 4f );

		// this badboy gets reset every tick, so I need to constantly reset it!
		// I have a lot of CameraModes. I should consider making a custom base for them all
		if(CameraMode is ZomFirstPersonCamera cam)
			cam.Owner = this;
		if ( CameraMode is ZomThirdPersonCamera cam1 )
			cam1.Owner = this;
	}

	public void FrameUpdateViewOffset()
	{
		Host.AssertClient();
		ViewPunchOffset = Angles.Lerp( ViewPunchOffset, Angles.Zero, Time.Delta * 8f );
		ViewPunchVelocity = Angles.Lerp( ViewPunchVelocity, Angles.Zero, Time.Delta * 4f );

		// this badboy gets reset every tick, so I need to constantly reset it!
		// I have a lot of CameraModes. I should consider making a custom base for them all
		if ( CameraMode is ZomFirstPersonCamera cam )
			cam.Owner = this;
		if ( CameraMode is ZomThirdPersonCamera cam1 )
			cam1.Owner = this;
	}

	DamageInfo LastDamage;

	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState == LifeState.Dead )
			return;

		LastDamage = info;
		TimeSincePassiveHealed = -2;

		if(TimeUntilAdrenalineExpires < 0)
			Velocity = 0;

		this.ProceduralHitReaction( info );

		Rand.SetSeed( Time.Tick );
		ViewPunch( Rand.Float( .5f ) + -.25f, (Rand.Float( .5f ) + 1) * (Rand.Int( 1 ) * 2 - 1) );

		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;

		if ( info.Flags.HasFlag( DamageFlags.Blast ) )
		{
			Deafen( To.Single( Client ), info.Damage.LerpInverse( 0, 60 ) );
		}

		if ( info.Attacker is HumanPlayer attacker )
		{
			info.Damage *= .1f;
			if ( attacker != this )
			{
				attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, Health.LerpInverse( 100, 0 ) );
			}

			TookDamage( To.Single( this ), info.Attacker.Position );
		}

		if ( info.Attacker is CommonZombie zomAttacker )
		{
			TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position );
		}

		if ( Health > 0 && info.Damage > 0 )
		{
			Health -= info.Damage;
			if ( Health <= 0 )
			{
				if(LifeState == LifeState.Alive )
				{
					Health = 200;
					Incapacitate();
				}
				else if(LifeState == LifeState.Dying )
				{
					Health = 0;
					OnKilled();
				}
			}
		}
	}

	public void Incapacitate()
	{
		if( RevivesRemaining > 0 )
		{
			ZomChatBox.AddInformation( To.Everyone, $"{Client.Name} is incapacitated!", $"avatar:{Client.PlayerId}", "#FF5B71" );

			SetAnimParameter( "sit", 2 );
			SetAnimParameter( "sit_pose", Rand.Int(3) );

			Controller = new IncapacitatedController();
			(Controller as BaseZomWalkController).Owner = this;
			if ( Host.IsServer ) PlaySound( "human.incapacitate" );
			PingMarker.Ping( Position, PingType.DownedPlayer, $"Save {Client.Name}!", -1, this );

			RevivesRemaining -= 1;
			LifeState = LifeState.Dying;
		}
		else
		{
			Health = 0;
			OnKilled();
		}
	}

	public void Revive()
	{
		LifeState = LifeState.Alive;
		SetAnimParameter( "sit", 0 );
		Controller = new HumanWalkController();
		(Controller as BaseZomWalkController).Owner = this;
		Health = 20;
	}

	[ClientRpc]
	public void DidDamage( Vector3 pos, float amount, float healthinv )
	{
		Sound.FromScreen( "dm.ui_attacker" )
			.SetPitch( 1 + healthinv * 1 );

		HitIndicator.Current?.OnHit( pos, amount );
	}

	public TimeSince TimeSinceDamage = 1.0f;

	[ClientRpc]
	public void TookDamage( Vector3 pos )
	{
		//DebugOverlay.Sphere( pos, 10.0f, Color.Red, true, 10.0f );

		TimeSinceDamage = 0;
		DamageIndicator.Current?.OnHit( pos );
	}

	[ConCmd.Client]
	public static void InflictDamage()
	{
		if ( Local.Pawn is HumanPlayer ply )
		{
			ply.TookDamage( ply.Position + ply.EyeRotation.Forward * 100.0f );
		}
	}

	TimeSince timeSinceLastFootstep = 0;

	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( !IsServer )
			return;

		if ( timeSinceLastFootstep < 0.2f )
			return;

		volume *= FootstepVolume();

		timeSinceLastFootstep = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		tr.Surface.DoFootstep( this, tr, foot, volume * 10 );
	}
	public void RenderHud( Vector2 screenSize )
	{
		if ( LifeState != LifeState.Alive && LifeState != LifeState.Dying )
			return;

		if ( ActiveChild is BaseZomWeapon weapon )
		{
			weapon.RenderHud( screenSize );
		}
	}

	public override float FootstepVolume()
	{
		return Velocity.WithZ( 0 ).Length.LerpInverse( 0.0f, 200.0f ) * 1.0f;
	}

	public void TryAlertZombies( Entity target, float percent, float radius )
	{
		foreach ( CommonZombie zom in Entity.FindInSphere( Position, radius ).OfType<CommonZombie>() )
		{
			var chance = percent; // todo: decrease chance further away from position;
			zom.TryAlert( target, chance );
		}
	}

	protected override Entity FindUsable()
	{
		// First try a direct 0 width line
		var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 85 )
			.Ignore( this )
			.WithoutTags( "trigger" )
			.Run();

		// See if any of the parent entities are usable if we ain't.
		var ent = tr.Entity;
		while ( ent.IsValid() && !IsValidUseEntity( ent ) )
		{
			ent = ent.Parent;
		}

		// Nothing found, try a wider search
		if ( !IsValidUseEntity( ent ) )
		{
			tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 85 )
			.Radius( 2 )
			.Ignore( this )
			.WithoutTags( "trigger", "gib" )
			.Run();

			// See if any of the parent entities are usable if we ain't.
			ent = tr.Entity;
			while ( ent.IsValid() && !IsValidUseEntity( ent ) )
			{
				ent = ent.Parent;
			}
		}

		// Still no good? Bail.
		if ( !IsValidUseEntity( ent ) ) return null;

		return ent;
	}

	public void StopUse()
	{
		StopUsing();
	}

	public bool OnUse( Entity user )
	{
		if ( LifeState != LifeState.Dying ) return false;

		Revive();
		return true;
	}

	public bool IsUsable( Entity user )
	{
		if ( LifeState != LifeState.Dying ) return false;

		// this is required by iUse!
		return true;
	}
}
