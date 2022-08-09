namespace ZombieHorde;

/// <summary>
/// Pump Shotgun
/// </summary>
[Library( "zom_doublebarrel" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/shotguns/doublebarrelshotgun/w_doublebarrel.vmdl" )]
[Title( "Double Barrel Shotgun" ), Category( "Primary Weapons" )]
partial class DoubleBarrel : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/shotguns/doublebarrelshotgun/w_doublebarrel.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/shotguns/doublebarrelshotgun/v_doublebarrel.vmdl";
	public override float PrimaryRate => 2.0f;
	public override float SecondaryRate => 1.5f;
	public override int ClipSize => 2;
	public override float ReloadTime => 2.0f;
	public override WeaponSlot WeaponSlot => WeaponSlot.Primary;
	public override int AmmoMax => 80;
	public override float BulletSpread => 0.3f;
	public override float ShotSpreadMultiplier => 1.5f;
	public override string Icon => "weapons/licensed/HQFPSWeapons/Icons/Inventory/Items/Equipment/Icon_DoubleBarrelShotgun.png";
	public override Color RarityColor => WeaponRarity.Uncommon;
	public override Transform ViewModelOffsetDuck => Transform.WithPosition( new Vector3( .5f, -1f, 1.5f ) ).WithRotation( new Angles( 0, -4, 20 ).ToRotation() );

	[Net, Predicted]
	public bool StopReloading { get; set; }

	// HEY!! THIS WEAPON HAS A SECOND SKIN THAT COULD BE USED AS A FASTER-FIRING VERSION!!!

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;
		AmmoReserve = AmmoMax;
	}

	public override bool CanPrimaryAttack()
	{
		// slow auto rate, can spam click to fire as fast as possible
		// this opens us up to cheating with autoclickers but it doesn't really matter in this game
		return base.CanPrimaryAttack() || Input.Pressed( InputButton.PrimaryAttack );
	}

	public override async void AttackPrimary()
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
		PlaySound( "shotgun1b.shoot" );
		PlaySound( "shotgun1.shoot.tail" );

		//
		// Shoot the bullets
		//
		ShootBullet( BulletSpread, 0.8f, 12.0f, 15.0f, 8 );
		Rand.SetSeed( Time.Tick );
		(Owner as HumanPlayer).ViewPunch( Rand.Float( -.5f ) + -4.5f, Rand.Float( 1f ) + 1f );

		await Task.Delay( 500 );
		if ( AmmoClip <= 0 )
		{
			Reload();
		}
	}

	[ClientRpc]
	protected override void ShootEffects()
	{
		Host.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
		//Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );

		ViewModelEntity?.SetAnimParameter( "fire", true );
		CrosshairLastShoot = 0;
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
}
