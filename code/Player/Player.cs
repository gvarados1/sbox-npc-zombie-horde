using Sandbox;
using Sandbox.Component;

namespace ZombieHorde;

public partial class HumanPlayer : Player, IUse
{
	TimeSince timeSinceDropped;

	[Net]
	public float MaxHealth { get; set; } = 100;
	[Net]
	public int RevivesRemaining { get; set; }

	public bool SupressPickupNotices { get; private set; }

	public TimeSince TimeSinceLastKill { get; set; }
	private TimeSince TimeSincePassiveHealed = 0;

	public HumanPlayer()
	{
		Inventory = new ZomInventory( this );
	}

	public override void Respawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Controller = new HumanWalkController();

		Animator = new HumanPlayerAnimator();

		CameraMode = new FirstPersonCamera();

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Clothing.DressEntity( this );

		SupressPickupNotices = true;

		Inventory.DeleteContents();
		/*
		Inventory.Add( new Crowbar() );
		Inventory.Add( new Pistol(), true );
		*/

		SupressPickupNotices = false;
		Health = 100;
		RevivesRemaining = BaseGamemode.Ent.HumanMaxRevives;

		SetAnimParameter( "sit", 0 );

		base.Respawn();
	}

	public void SpawnAsSpectator()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Animator = new HumanPlayerAnimator();

		CameraMode = new SpectatePlayerCamera();

		EnableAllCollisions = false;
		EnableDrawing = false;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Health = 100;

		base.Respawn();
		LifeState = LifeState.Dead;
	}

	[ConCmd.Admin]
	public static void GiveAll()
	{
		var ply = ConsoleSystem.Caller.Pawn as HumanPlayer;

		ply.Inventory.Add( new SMG() );

		/*
		ply.GiveAmmo( AmmoType.Pistol, 1000 );
		ply.GiveAmmo( AmmoType.Python, 1000 );
		ply.GiveAmmo( AmmoType.Buckshot, 1000 );
		ply.GiveAmmo( AmmoType.Crossbow, 1000 );
		ply.GiveAmmo( AmmoType.Grenade, 1000 );
		ply.GiveAmmo( AmmoType.Tripmine, 1000 );

		ply.Inventory.Add( new Python() );
		ply.Inventory.Add( new Shotgun() );
		ply.Inventory.Add( new SMG() );
		ply.Inventory.Add( new AK47() );
		ply.Inventory.Add( new Crossbow() );
		ply.Inventory.Add( new GrenadeWeapon() );
		ply.Inventory.Add( new TripmineWeapon() );
		ply.Inventory.Add( new NpcSpawner() );
		*/
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

		Inventory.DeleteContents();

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


	public override void Simulate( Client cl )
	{
		if ( LifeState == LifeState.Dead )
		{
			if ( IsServer && BaseGamemode.Ent.EnableRespawning())
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

		if ( Input.Pressed( InputButton.View ) )
		{
			if ( CameraMode is ThirdPersonCamera )
			{
				CameraMode = new FirstPersonCamera();
			}
			else
			{
				CameraMode = new ThirdPersonCamera();
			}
		}

		if ( Input.Pressed( InputButton.Drop ) )
		{
			var dropped = Inventory.DropActive();
			if ( dropped != null )
			{
				if ( dropped.PhysicsGroup != null )
				{
					dropped.PhysicsGroup.Velocity = Velocity + (EyeRotation.Forward + EyeRotation.Up) * 300;
				}

				timeSinceDropped = 0;
				SwitchToBestWeapon();
			}
		}

		SimulateActiveChild( cl, ActiveChild );

		//
		// If the current weapon is out of ammo and we last fired it over half a second ago
		// lets try to switch to a better wepaon
		//
		if ( ActiveChild is BaseZomWeapon weapon && !weapon.IsUsable() && weapon.TimeSincePrimaryAttack > 0.5f && weapon.TimeSinceSecondaryAttack > 0.5f )
		{
			SwitchToBestWeapon();
		}

		//passively heal up to 20 hp
		if ( Host.IsServer )
		{
			if(Health < 20 )
			{
				if(TimeSincePassiveHealed > .5f)
				{
					TimeSincePassiveHealed = 0;
					Health += 1;
				}
			}
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
		//ActiveChild = best;
	}

	public override void StartTouch( Entity other )
	{
		if ( timeSinceDropped < 1 ) return;

		base.StartTouch( other );
	}

	public override void PostCameraSetup( ref CameraSetup setup )
	{
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

	DamageInfo LastDamage;

	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState == LifeState.Dead )
			return;

		LastDamage = info;
		TimeSincePassiveHealed = -2;

		Velocity = 0;

		this.ProceduralHitReaction( info );

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
			RevivesRemaining -= 1;
			LifeState = LifeState.Dying;
			SetAnimParameter( "sit", 2 );
			SetAnimParameter( "sit_pose", Rand.Int(3) );

			Controller = new IncapacitatedController();
			if ( Host.IsServer ) PlaySound( "human.incapacitate" );
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
		if ( LifeState != LifeState.Alive )
			return;

		if ( ActiveChild is BaseZomWeapon weapon )
		{
			weapon.RenderHud( screenSize );
		}
	}

	public void TryAlertZombies( Entity target, float percent, float radius )
	{
		foreach ( CommonZombie zom in Entity.FindInSphere( Position, radius ).OfType<CommonZombie>() )
		{
			var chance = percent; // todo: decrease chance further away from position;
			zom.TryAlert( target, chance );
		}
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
