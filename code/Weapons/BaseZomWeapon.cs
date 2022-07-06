namespace ZombieHorde;

partial class BaseZomWeapon : BaseWeapon
{
	public virtual int ClipSize => 16;
	public virtual float ReloadTime => 3.0f;
	public virtual WeaponSlot WeaponSlot => WeaponSlot.Primary;
	public virtual int AmmoMax => 60;
	public virtual float BulletSpread => .05f;
	public virtual float ShotSpreadMultiplier => 2f;

	// todo: go through all my [Net]s and figure out which can be [Local]
	[Net, Predicted]
	public int AmmoClip { get; set; }
	[Net, Predicted]
	public int AmmoReserve { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceReload { get; set; }

	[Net, Predicted]
	public bool IsReloading { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceDeployed { get; set; }
	[Net, Predicted]
	public TimeSince TimeSinceShove { get; set; }
	[Net]
	public bool OverridingAnimator { get; set; } = false;
	[Net, Local, Predicted]
	public float SpreadMultiplier { get; set; } = 1;



	public int AvailableAmmo()
	{
		var owner = Owner as HumanPlayer;
		if ( owner == null ) return 0;
		return AmmoReserve;
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		TimeSinceDeployed = 0;

		IsReloading = false;
	}

	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
	}

	public override void Reload()
	{
		if ( IsReloading )
			return;

		if ( AmmoClip >= ClipSize )
			return;

		if ( AmmoReserve <= 0 )
		{
			return;
		}

		TimeSinceReload = 0;

		IsReloading = true;

		(Owner as AnimatedEntity).SetAnimParameter( "b_reload", true );

		StartReloadEffects();
	}

	public override void Simulate( Client owner )
	{
		if ( TimeSinceDeployed < 0.6f )
			return;

		if ( !IsReloading )
		{
			base.Simulate( owner );
		}
		else
		{
			if ( CanSecondaryAttack() )
			{
				using ( LagCompensation() )
				{
					TimeSinceSecondaryAttack = 0;
					AttackSecondary();
				}
			}
		}

		if ( IsReloading && TimeSinceReload > ReloadTime )
		{
			OnReloadFinish();
		}

		AdjustAccuracyMultiplier();
	}

	public void AdjustAccuracyMultiplier()
	{
		if ( Owner is HumanPlayer ply )
		{
			var controller = ply.Controller as HumanWalkController;
			var targetMultipler = 1f;

			// hack: floor velocity to limit prediction errors
			var adjustedVelocity = MathF.Floor( ply.Velocity.WithZ( 0 ).Length);

			targetMultipler = Math.Min( adjustedVelocity / controller.WalkSpeed + 1, 2.5f )* .6f + .4f;


			
			if ( controller.GroundEntity == null )
			{
				targetMultipler *= 1.2f;
			}
			else if ( controller.Duck.IsActive )
			{
				targetMultipler *= .75f;
			}

			// prediction issue: velocity gets set to 0 when attacked. this can not be predicted! what do I do?
			SpreadMultiplier = SpreadMultiplier.LerpTo( targetMultipler, .25f );

			//SpreadMultiplier = MathF.Floor( SpreadMultiplier * 1000 ) / 1000;
			//SpreadMultiplier = SpreadMultiplier.Clamp( 0, 8 );

			//Log.Info( SpreadMultiplier + ", " + targetMultipler);
		}
	}

	public override bool CanPrimaryAttack()
	{
		if ( TimeSinceShove < .5 ) return false;
		if ( !Owner.IsValid() || !Input.Down( InputButton.PrimaryAttack ) ) return false;

		var rate = PrimaryRate;
		if ( rate <= 0 ) return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	public virtual void OnReloadFinish()
	{
		IsReloading = false;

		var ammo = Math.Min(AmmoReserve, ClipSize - AmmoClip);

		AmmoReserve -= ammo;
		AmmoClip += ammo;
	}

	[ClientRpc]
	public virtual void StartReloadEffects()
	{
		ViewModelEntity?.SetAnimParameter( "reload", true );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
	}

	public override void AttackSecondary()
	{
		if(TimeSinceShove > 1 )
		{
			MeleeAttack();
			TimeSinceShove = 0;
			//TimeSincePrimaryAttack = -2;
		}
	}

	public async void MeleeAttack()
	{
		// pause reloading
		var wasReloading = IsReloading;
		var timeSinceReload = TimeSinceReload;
		if ( wasReloading )
		{
			// when I make my own weapons I will be able store/pause reload states during melee shove. Currently we have to start the animation over (even though reloading doesn't restart)
			IsReloading = false;
		}

		PlaySound( "dm.crowbar_attack" );
		var ply = (Owner as AnimatedEntity);
		OverridingAnimator = true;
		ViewModelEntity?.SetAnimParameter( "fire", true );
		if(ViewModelEntity is ZomViewModel vm) vm.PlayMeleeAnimation();
		ply.SetAnimParameter( "holdtype", 5 );
		ply.SetAnimParameter( "b_attack", true );

		Rand.SetSeed( Time.Tick );

		var forward = (Owner.EyeRotation * Rotation.FromPitch( 5 )).Forward.Normal;

		foreach ( var tr in TraceShove( Owner.EyePosition, Owner.EyePosition + forward * 90, 8 ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100, 20 )
				.UsingTraceResult( tr )
				.WithAttacker( Owner )
				.WithWeapon( this );

			tr.Entity.TakeDamage( damageInfo );
		}
		if ( IsServer )
		{
			TryAlertZombies( Owner, 1, 50f, Owner.EyePosition + forward * 60 );
			//DebugOverlay.Sphere( Owner.EyePosition + forward * 60, 50, Color.Yellow, .5f );
			foreach ( var overlap in Entity.FindInSphere( Owner.EyePosition + forward * 60, 50 ) )
			{
				if ( overlap is CommonZombie zom )
				{
					var damageInfo = DamageInfo.FromBullet( Position, forward * 100, 15 )
						.WithAttacker( Owner )
						.WithWeapon( this );

					zom.TakeDamage( damageInfo );

					zom.Stun( 1.5f );
					zom.Velocity = forward * 200;
				}
			}
		}

		// note: using Task.Delay causes prediction issues here but I don't think I care?
		await Task.Delay( 210 );
		OverridingAnimator = false;

		// continue reloading
		if ( wasReloading )
		{
			Reload();
			TimeSinceReload = timeSinceReload;
		}
	}

	public virtual IEnumerable<TraceResult> TraceShove( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool InWater = Map.Physics.IsPointWater( start );

		var tr = Trace.Ray( start, end )
				.UseHitboxes()
				.HitLayer( CollisionLayer.Water, !InWater )
				.HitLayer( CollisionLayer.Debris )
				.Ignore( Owner )
				.Ignore( this )
				.WithoutTags( "zombie" )
				.Size( radius )
				.Run();

		if ( tr.Hit )
			yield return tr;
	}

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		Host.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

		ViewModelEntity?.SetAnimParameter( "fire", true );
		CrosshairLastShoot = 0;

	}

	/// <summary>
	/// Shoot a single bullet
	/// </summary>
	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize, int bulletCount = 1 )
	{
		//
		// Seed rand using the tick, so bullet cones match on client and server
		//
		Rand.SetSeed( Time.Tick );

		spread *= SpreadMultiplier;
		//Log.Info( spread + ", " + SpreadMultiplier );

		SpreadMultiplier *= ShotSpreadMultiplier;

		for ( int i = 0; i < bulletCount; i++ )
		{
			var forward = (Owner.EyeRotation * Rotation.FromPitch( 5 )).Forward;
			forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f; //0.25f;
			forward = forward.Normal;

			//
			// ShootBullet is coded in a way where we can have bullets pass through shit
			// or bounce off shit, in which case it'll return multiple results
			//
			foreach ( var tr in TraceBullet( Owner.EyePosition, Owner.EyePosition + forward * 5000, bulletSize ) )
			{
				tr.Surface.DoBulletImpact( tr );

				if ( tr.Distance > 200 )
				{
					CreateTracerEffect( tr.EndPosition );
				}

				if ( !IsServer ) continue;
				if ( !tr.Entity.IsValid() ) continue;

				var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100 * force, damage )
					.UsingTraceResult( tr )
					.WithAttacker( Owner )
					.WithWeapon( this );

				tr.Entity.TakeDamage( damageInfo );

				// alert zombies where the bullet hits
				TryAlertZombies( damageInfo.Attacker, .2f, 500f, tr.HitPosition );
			}
		}
		// alert zombies where the bullet is shot from
		TryAlertZombies( Owner, .2f, 500f, Position );
	}

	public void TryAlertZombies( Entity target, float percent, float radius, Vector3 position)
	{
		foreach ( CommonZombie zom in Entity.FindInSphere( position, radius ).OfType<CommonZombie>() )
		{
			var chance = percent; // todo: decrease chance further away from position;
			zom.TryAlert( target, chance );
		}
	}

	[ClientRpc]
	public void CreateTracerEffect( Vector3 hitPosition )
	{
		// get the muzzle position on our effect entity - either viewmodel or world model
		var pos = EffectEntity.GetAttachment( "muzzle" ) ?? Transform;

		var system = Particles.Create( "particles/tracer.standard.vpcf" );
		system?.SetPosition( 0, pos.Position );
		system?.SetPosition( 1, hitPosition );
	}

	public bool TakeAmmo( int amount )
	{
		if ( AmmoClip < amount )
			return false;

		AmmoClip -= amount;
		return true;
	}

	[ClientRpc]
	public virtual void DryFire()
	{
		PlaySound( "dm.dryfire" );
	}

	public override void CreateViewModel()
	{
		Host.AssertClient();

		if ( string.IsNullOrEmpty( ViewModelPath ) )
			return;

		ViewModelEntity = new ZomViewModel();
		ViewModelEntity.Position = Position;
		ViewModelEntity.Owner = Owner;
		ViewModelEntity.EnableViewmodelRendering = true;
		ViewModelEntity.SetModel( ViewModelPath );
		ViewModelEntity.SetAnimParameter( "deploy", true );
	}

	public override void CreateHudElements()
	{
		if ( Local.Hud == null ) return;
	}

	public bool IsUsable()
	{
		if ( AmmoClip > 0 ) return true;
		return AvailableAmmo() > 0;
	}

	protected TimeSince CrosshairLastShoot { get; set; }
	protected TimeSince CrosshairLastReload { get; set; }

	public virtual void RenderHud( in Vector2 screensize )
	{
		var scale = Screen.Height / 1080.0f;
		var center = new Vector2(Screen.Width * .5f / scale, Screen.Height * .56f / scale);

		if ( IsReloading || (AmmoClip == 0 && ClipSize > 1) )
			CrosshairLastReload = 0;

		RenderCrosshair( center, CrosshairLastShoot.Relative, CrosshairLastReload.Relative );
	}

	public virtual void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;
	}
}

public enum WeaponSlot
{
	Secondary,
	Primary,
	Grenade,
	Medkit,
	Pills,
	Prop
}
