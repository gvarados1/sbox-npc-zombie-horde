using Sandbox.Component;
using Sandbox.UI;

namespace ZombieHorde;

partial class BaseZomWeapon : BaseWeapon, IUse
{
	public virtual int ClipSize => 16;
	public virtual float ReloadTime => 3.0f;
	public virtual WeaponSlot WeaponSlot => WeaponSlot.Primary;
	public virtual int AmmoMax => 60;
	public virtual float BulletSpread => .05f;
	public virtual float ShotSpreadMultiplier => 2f;
	public virtual float ShotSpreadLerp => .2f;
	public virtual string Icon => "";
	public virtual Color RarityColor => WeaponRarity.Common;
	public virtual Transform ViewModelOffsetDuck => Transform.Zero;
	public virtual Transform ViewModelOffset => Transform.Zero; // used to make throwable/melee weapons look different even though we're using the exact same animations.
	public virtual bool UseAlternativeSprintAnimation => false;

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
		if ( AmmoMax == -1 ) return -1;
		return AmmoReserve;
	}

	public virtual void Deploy()
	{
		TimeSinceDeployed = 0;
		ViewModelEntity?.SetAnimParameter( "deploy", true );
	}
	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );
		SetParent( Owner, true );

		TimeSinceDeployed = 0;

		IsReloading = false;
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		if ( dropped ) return;

		SetCarryPosition();
	}

	public virtual void SetCarryPosition()
	{
		EnableDrawing = false;
		ResetInterpolation();
	}

	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
		Tags.Add( "weapon" );

		var glow = Components.GetOrCreate<Glow>();
		glow.Active = true;
		glow.Color = Color.Yellow;
		glow.RangeMin = 0;
		glow.RangeMax = int.MaxValue;
	}

	public override void OnCarryStart( Entity carrier )
	{
		base.OnCarryStart( carrier );
		SetCarryPosition();
		var glow = Components.GetOrCreate<Glow>();
		glow.Active = false;
	}

	public override void OnCarryDrop( Entity dropper )
	{
		base.OnCarryDrop( dropper );
		var glow = Components.GetOrCreate<Glow>();
		glow.Active = true;
	}

	public override void Reload()
	{
		if ( IsReloading )
			return;

		if ( AmmoClip >= ClipSize )
			return;

		if ( AmmoReserve <= 0 && AmmoMax != -1)
		{
			return;
		}

		TimeSinceReload = 0;

		if ( (Owner as HumanPlayer).TimeUntilAdrenalineExpires > .5f )
			TimeSinceReload = ReloadTime / 1.5f;

		IsReloading = true;

		(Owner as AnimatedEntity).SetAnimParameter( "b_reload", true );

		StartReloadEffects();
	}

	public override void Simulate( Client owner )
	{
		//SetCarryPosition();
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

	public virtual void AdjustAccuracyMultiplier()
	{
		if ( Owner is HumanPlayer ply )
		{
			if ( Owner.LifeState != LifeState.Alive )
			{
				SpreadMultiplier = SpreadMultiplier.LerpTo( 1, ShotSpreadLerp );
				SpreadMultiplier = SpreadMultiplier.Clamp( 0, 12 );
				return;
			}

			var controller = ply.Controller as HumanWalkController;
			var targetMultipler = 1f;

			// hack: floor velocity to limit prediction errors
			var adjustedVelocity = MathF.Floor( ply.Velocity.WithZ( 0 ).Length);

			//targetMultipler = Math.Min( adjustedVelocity / controller.WalkSpeed + 1, 2.5f )* .6f + .4f;
			//targetMultipler = Math.Min( adjustedVelocity / controller.WalkSpeed + 1, 2f )* .7f + .3f;
			targetMultipler = Math.Min( adjustedVelocity / controller.WalkSpeed + 1, 2f )* .5f + .5f;


			
			if ( controller.GroundEntity == null )
			{
				targetMultipler *= 1.2f;
			}
			else if ( controller.Duck.IsActive )
			{
				targetMultipler *= .75f;
			}

			// prediction issue: velocity gets set to 0 when attacked. this can not be predicted! what do I do?
			SpreadMultiplier = SpreadMultiplier.LerpTo( targetMultipler, ShotSpreadLerp );

			//SpreadMultiplier = MathF.Floor( SpreadMultiplier * 1000 ) / 1000;
			SpreadMultiplier = SpreadMultiplier.Clamp( 0, 12 );

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
		
		// infinite ammo?
		if(AmmoMax == -1 )
		{
			AmmoClip = ClipSize;
			return;
		}

		var ammo = Math.Min(AmmoReserve, ClipSize - AmmoClip);

		AmmoReserve -= ammo;
		AmmoClip += ammo;
		//ViewModelEntity?.SetAnimParameter( "idle", true );
	}

	[ClientRpc]
	public virtual void StartReloadEffects()
	{
		ViewModelEntity?.SetAnimParameter( "reload", true );
		var speedMultiplier = 1f;
		if ( (Owner as HumanPlayer).TimeUntilAdrenalineExpires > .5f )
			speedMultiplier = 1.5f;

		ViewModelEntity?.SetAnimParameter( "reload_speed", speedMultiplier );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
	}

	public override void AttackSecondary()
	{
		if(TimeSinceShove > 1 )
		{
			//ViewModelEntity?.SetAnimParameter( "fire", true );
			MeleeAttack();
			//TimeSincePrimaryAttack = -2;
		}
	}

	public async void MeleeAttack()
	{
		var ply = Owner as HumanPlayer;

		var speedMultiplier = 1f;
		TimeSinceShove = 0;
		// check stamina
		if ( !ply.TakeStamina( 5 ) )
		{
			speedMultiplier *= .25f;
			ply.Stamina = 0;
			ply.TimeSinceUsedStamina = 0;
			TimeSinceShove = -1f;
		}

		Rand.SetSeed( Time.Tick );
		//(Owner as HumanPlayer).ViewPunch( Rotation.FromYaw( Rand.Float( 2f ) - 1f ) * Rotation.FromPitch( Rand.Float( .5f ) + -.25f ) );
		(Owner as HumanPlayer).ViewPunch( Rand.Float( .5f ) + -.25f, Rand.Float( .25f ) + .25f );
		//(Owner as HumanPlayer).ViewPunch( .5f, 1 );
		// pause reloading
		var wasReloading = IsReloading;
		var timeSinceReload = TimeSinceReload;
		if ( wasReloading )
		{
			// when I make my own weapons I will be able store/pause reload states during melee shove. Currently we have to start the animation over (even though reloading doesn't restart)
			//IsReloading = false;
		}

		PlaySound( "dm.crowbar_attack" );
		OverridingAnimator = true;
		if(ViewModelEntity is ZomViewModel vm) vm.PlayMeleeAnimation( speedMultiplier );
		ply.SetAnimParameter( "holdtype", 5 );
		ply.SetAnimParameter( "holdtype_handedness", 0 );
		ply.SetAnimParameter( "holdtype_attack", 1.0f );
		ply.SetAnimParameter( "b_attack", true );

		Rand.SetSeed( Time.Tick );

		var forward = Owner.EyeRotation.Forward.Normal;

		foreach ( var tr in TraceShove( Owner.EyePosition, Owner.EyePosition + forward * 90, 8 ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			var damageInfo = DamageInfoExt.FromCustom( tr.EndPosition, forward * 100, 20, DamageFlags.Slash )
				.UsingTraceResult( tr )
				.WithAttacker( Owner )
				.WithWeapon( this );

			// hack: use "bullet" damage to destroy glass. I tried making a partial class of the glass but that didn't work. Maybe I was just doing it wrong.
			if(tr.Entity is GlassShard)
				damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100, 20)
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
		//await Task.Delay( 210 );
		await Task.Delay( (int)(300 / speedMultiplier) );
		OverridingAnimator = false;

		// continue reloading
		if ( wasReloading )
		{
			//Reload();
			//TimeSinceReload = timeSinceReload;
		}
	}

	public virtual IEnumerable<TraceResult> TraceShove( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool InWater = Map.Physics.IsPointWater( start );

		var tr = Trace.Ray( start, end )
				.Ignore( Owner )
				.Ignore( this )
				.WithoutTags( "zombie", "trigger" )
				.Size( radius )
				.Run();

		if ( tr.Hit )
			yield return tr;
	}

	public override IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool underWater = Trace.TestPoint( start, "water" );

		var trace = Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc", "glass", "gib" )
				.Ignore( this )
				.Size( radius );

		//
		// If we're not underwater then we can hit water
		//
		if ( !underWater )
			trace = trace.WithAnyTags( "water" );

		var tr = trace.Run();

		if ( tr.Hit )
			yield return tr;

		// penetrate 1 layer of glass
		if ( tr.Entity is GlassShard )
		{
			var trace2 = Trace.Ray( tr.EndPosition + tr.Direction * 10, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc", "glass", "gib" )
				.Ignore( this )
				.Ignore(tr.Entity)
				.Size( radius );

			var tr2 = trace2.Run();
			if ( tr2.Hit )
				yield return tr2;
		}


		//
		// Another trace, bullet going through thin material, penetrating water surface?
		//
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
	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize = 4, int bulletCount = 1 )
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
			var forward = Owner.EyeRotation.Forward;
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
		if ( Local.Client.Components.TryGet<DevCamera>( out var devCam, true ) )
		{
			if ( devCam.Enabled ) return;
		}

			var scale = Screen.Height / 1080.0f;
		var center = new Vector2(Screen.Width * .5f / scale, Screen.Height * .565f / scale);

		if ( IsReloading || (AmmoClip == 0 && ClipSize > 1) )
			CrosshairLastReload = 0;

		RenderCrosshair( center, CrosshairLastShoot.Relative, CrosshairLastReload.Relative );
	}

	public virtual void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;
	}

	public bool OnUse( Entity user )
	{
		var ply = user as HumanPlayer;
		if ( ply.LifeState != LifeState.Alive ) return false;

		// this inventory system sucks
		var inv = ply.Inventory as ZomInventory;
		Entity dropped = null;
		switch ( WeaponSlot )
		{
			case WeaponSlot.Secondary:
			case WeaponSlot.Grenade:
			case WeaponSlot.Medkit:
			case WeaponSlot.Pills:
				{
					dropped = inv.DropItem( inv.GetSlot( WeaponSlot ) );
					break;
				}
			case WeaponSlot.Primary:
				{
					if ( inv.Add( this, true ) )
					{
						ply.StopUse();
						ply.timeSinceDropped = 0;
						return true;
					}
					if(ply.ActiveChild != null && (ply.ActiveChild as BaseZomWeapon).WeaponSlot == WeaponSlot.Primary )
					{
						dropped = inv.DropItem( inv.DropActive() );
						SetActive();
					}
					else
					{
						dropped = inv.DropItem( inv.GetSlot( WeaponSlot ) );
					}
					break;
				}
		}
		ply.Inventory.Add( this , ply.ActiveChild == dropped || WeaponSlot == WeaponSlot.Secondary || WeaponSlot == WeaponSlot.Primary );


		//Log.Info( $"{Host.Name}: 1:{inv.Secondary} 2:{inv.Primary1} 3:{inv.Primary2} 4:{inv.Grenade} 5:{inv.Medkit} 6:{inv.Pills}" );

		if (dropped != null && dropped.PhysicsGroup != null )
		{
			// do a trace to check if we're throwing the gun through a wall/floor
			var tr = Trace.Ray( Owner.EyePosition, Owner.EyePosition + Owner.EyeRotation.Forward * 64 )
			.UseHitboxes()
			.WorldOnly()
			.Ignore( this )
			.Size( 8 );

			var hit = tr.Run().Hit;
			if ( hit )
			{
				Log.Info( hit );
				dropped.Position = Owner.EyePosition + Vector3.Down * 16;
				dropped.Rotation = Owner.EyeRotation * Rotation.FromYaw( 90 );
			}

			dropped.PhysicsGroup.Velocity = ply.Velocity + (ply.EyeRotation.Forward + ply.EyeRotation.Up) * 200;
		}

		ply.StopUse();
		ply.timeSinceDropped = 0;
		return true;
	}

	public async void SetActive()
	{
		// dumb hack because it takes a tick to update active on the client
		await Task.Delay( 1 );
		(Owner as HumanPlayer).Inventory.SetActive( this );
		Log.Info( "set" );
	}

	public bool IsUsable( Entity user )
	{
		var ply = user as HumanPlayer;
		if ( ply.timeSinceDropped < .5f ) return false;
		//if ( ply.ActiveChild == this ) return false;
		return true;
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

// I was thinking about doing weapon rarities but this will probably just be used to differentiate weapon slots.
public struct WeaponRarity
{
	public static readonly Color Common = Color.Yellow;
	public static readonly Color Uncommon = Color.Green;
	public static readonly Color Rare = Color.Orange;
	public static readonly Color Epic = Color.Magenta;
}
