namespace ZombieHorde;

/// <summary>
/// Pump Shotgun
/// </summary>
[Library( "zom_chromeshotgun" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/shotguns/r870/w_r870.vmdl", "gray")]
[Title( "Chrome Shotgun" ), Category( "Primary Weapons" )]
partial class ChromeShotgun : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/shotguns/r870/w_r870.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/shotguns/r870/v_r870_chrome.vmdl";
	public override float PrimaryRate => 1.2f;
	public override float SecondaryRate => 1.5f;
	public override int ClipSize => 8;
	public override float ReloadTime => 0.6f;
	public override WeaponSlot WeaponSlot => WeaponSlot.Primary;
	public override int AmmoMax => 80;
	public override float BulletSpread => 0.15f;
	public override float ShotSpreadMultiplier => 1.5f;
	public override string Icon => "weapons/licensed/HQFPSWeapons/Icons/Inventory/Items/Equipment/Icon_R870.png";
	public override Color RarityColor => WeaponRarity.Uncommon;

	[Net, Predicted]
	public bool StopReloading { get; set; }

	// HEY!! THIS WEAPON HAS A SECOND SKIN THAT COULD BE USED AS A FASTER-FIRING VERSION!!!

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;
		AmmoReserve = AmmoMax;
		SetMaterialGroup( 1 );
	}
	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );
		ViewModelEntity?.SetMaterialGroup( 1 );
	}

	public override void Simulate( Client owner )
	{
		base.Simulate( owner );

		if ( IsReloading && (Input.Pressed( InputButton.PrimaryAttack )) )
		{
			StopReloading = true;
		}
	}

	public override void AttackSecondary()
	{
		if ( TimeSinceShove > 1 )
		{
			//ViewModelEntity?.SetAnimParameter( "fire", true );
			MeleeAttack();
			TimeSinceShove = 0;
			//TimeSincePrimaryAttack = -2;
		}
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;

		if ( !TakeAmmo( 1 ) )
		{
			DryFire();

			if ( AvailableAmmo() > 0 )
			{
				Reload();
			}
			return;
		}

		(Owner as AnimatedEntity).SetAnimParameter( "b_attack", true );

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();
		PlaySound( "shotgun1a.shoot" );
		PlaySound( "shotgun1.shoot.tail" );

		//
		// Shoot the bullets
		//
		ShootBullet( BulletSpread, 0.8f, 14.0f, 15.0f, 6 );
		Rand.SetSeed( Time.Tick );
		(Owner as HumanPlayer).ViewPunch( Rand.Float( -.5f ) + -.5f, Rand.Float( 1f ) - .5f );
	}

	[ClientRpc]
	protected override void ShootEffects()
	{
		Host.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
		Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );

		ViewModelEntity?.SetAnimParameter( "fire", true );
		CrosshairLastShoot = 0;
	}

	public override void OnReloadFinish()
	{
		var stop = StopReloading;

		StopReloading = false;
		IsReloading = false;

		TimeSincePrimaryAttack = .2f;

		if ( AmmoClip >= ClipSize )
			return;

		if ( Owner is HumanPlayer player )
		{
			// infinite ammo?
			if ( AmmoMax == -1 )
			{
				AmmoClip = ClipSize;
				return;
			}

			if(AmmoReserve > 0 )
			{
				AmmoReserve -= 1;
				AmmoClip += 1;
			}
			else
			{
				FinishReload();
			}

		if ( AmmoClip < ClipSize && !stop )
			{
				Reload();
			}
			else
			{
				FinishReload();
			}
		}
		if ( stop )
		{
			AttackPrimary();
		}
	}

	[ClientRpc]
	protected virtual void FinishReload()
	{
		ViewModelEntity?.SetAnimParameter( "reload_finished", true );
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		if ( OverridingAnimator ) return;
		anim.SetAnimParameter( "holdtype", 3 ); // TODO this is shit
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
	}

	public override void SetCarryPosition()
	{
		base.SetCarryPosition();
		// dumb hard-coded positions
		EnableDrawing = true;
		var transform = Transform.Zero;
		transform.Position += Vector3.Right * 8.2f;
		transform.Position += Vector3.Down * 3;
		transform.Position += Vector3.Forward * -0;
		transform.Rotation *= Rotation.FromPitch( 220 );
		transform.Rotation *= Rotation.FromYaw( -15 );
		transform.Rotation *= Rotation.FromRoll( -30 );
		SetParent( Owner, "spine_2", transform );
	}

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		// one day I will make my own crosshairs!
		var draw = Render.Draw2D;

		var color = Color.Lerp( Color.Red, Color.White, lastReload.LerpInverse( 0.0f, 0.4f ) );
		draw.BlendMode = BlendMode.Lighten;
		draw.Color = color.WithAlpha( 0.2f + CrosshairLastShoot.Relative.LerpInverse( 1.2f, 0 ) * 0.5f );

		// center circle
		{
			var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.1f, 0.0f ) );
			var length = 2.0f + shootEase * 2.0f;
			draw.Circle( center, length );
		}


		draw.Color = draw.Color.WithAlpha( draw.Color.a * 0.2f );

		// outer lines
		{
			//var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.2f, 0.0f ) );
			var shootEase = SpreadMultiplier * 1f;
			//var length = 3.0f + shootEase * 2.0f;
			var length = 6.0f + shootEase * 2.0f;
			//var gap = 30.0f + shootEase * 50.0f;
			var gap = 12.0f + shootEase * 20.0f;
			var thickness = 2.0f;

			draw.Line( thickness, center + Vector2.Up * gap + Vector2.Left * length, center + Vector2.Up * gap - Vector2.Left * length );
			draw.Line( thickness, center - Vector2.Up * gap + Vector2.Left * length, center - Vector2.Up * gap - Vector2.Left * length );

			draw.Line( thickness, center + Vector2.Left * gap + Vector2.Up * length, center + Vector2.Left * gap - Vector2.Up * length );
			draw.Line( thickness, center - Vector2.Left * gap + Vector2.Up * length, center - Vector2.Left * gap - Vector2.Up * length );
		}
	}

	/*
	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;

		var color = Color.Lerp( Color.Red, Color.White, lastReload.LerpInverse( 0.0f, 0.4f ) );
		draw.BlendMode = BlendMode.Lighten;
		draw.Color = color.WithAlpha( 0.2f + lastAttack.LerpInverse( 1.2f, 0 ) * 0.5f );

		// center
		{
			var shootEase = 1 + Easing.BounceIn( lastAttack.LerpInverse( 0.3f, 0.0f ) );
			draw.Ring( center, 15 * shootEase, 14 * shootEase );
		}

		// outer lines
		{
			var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.4f, 0.0f ) );
			var length = 30.0f;
			var gap = 30.0f + shootEase * 50.0f;
			var thickness = 4.0f;
			var extraAngle = 30 * shootEase;

			draw.CircleEx( center + Vector2.Right * gap, length, length - thickness, 32, 220, 320 );
			draw.CircleEx( center - Vector2.Right * gap, length, length - thickness, 32, 40, 140 );

			draw.Color = draw.Color.WithAlpha( 0.1f );
			draw.CircleEx( center + Vector2.Right * gap * 2.6f, length, length - thickness * 0.5f, 32, 220, 320 );
			draw.CircleEx( center - Vector2.Right * gap * 2.6f, length, length - thickness * 0.5f, 32, 40, 140 );
		}
	}
	*/
}
