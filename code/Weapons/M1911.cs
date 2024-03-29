﻿namespace ZombieHorde;

/// <summary>
/// Standard Pistol
/// </summary>
[Library( "zom_m1911" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/handguns/m1911/w_m1911.vmdl" )]
[Title( "Pistol" ), Category( "Pistols" )]
partial class M1911 : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/handguns/m1911/w_m1911.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/handguns/m1911/v_m1911.vmdl";

	public override float PrimaryRate => 12.0f;
	public override float SecondaryRate => 4.5f;
	public override float ReloadTime => 2.2f;
	public override WeaponSlot WeaponSlot => WeaponSlot.Secondary;
	public override float BulletSpread => .05f;
	public override float ShotSpreadMultiplier => 1.8f;
	public override int ClipSize => 14;
	public override int AmmoMax => -1;
	public override string Icon => "weapons/licensed/HQFPSWeapons/Icons/Inventory/Items/Equipment/Icon_M1911.png";
	public override Transform ViewModelOffsetDuck => Transform.WithPosition( new Vector3( 0f, 1f, -.5f ) ).WithRotation( new Angles( -10, -2, 0 ).ToRotation() );

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;
		if ( Rand.Int( 2 ) == 1 )
		{
			SetMaterialGroup( 1 );
		}
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );
		if ( GetMaterialGroup() == 1 )
			ViewModelEntity?.SetMaterialGroup( 1 );
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		if ( OverridingAnimator ) return;
		anim.SetAnimParameter( "holdtype", 1 );
		//anim.SetAnimParameter( "holdtype_handedness", 2 );
		anim.SetAnimParameter( "holdtype_attack", 1 );
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
		anim.SetAnimParameter( "holdtype_handedness", 0 );
	}

	public override bool CanPrimaryAttack()
	{
		return base.CanPrimaryAttack() && Input.Pressed( InputButton.PrimaryAttack );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;

		if ( !TakeAmmo( 1 ) )
		{
			DryFire();

			Reload();
			return;
		}


		//
		// Tell the clients to play the shoot effects
		//
		(Owner as AnimatedEntity).SetAnimParameter( "b_attack", true );
		ShootEffects();
		PlaySound( "pistol.shoot" );
		PlaySound( "pistol.shoot.tail" );

		//
		// Shoot the bullets
		//
		ShootBullet( BulletSpread, 1, 16.0f);
		Rand.SetSeed( Time.Tick );
		//(Owner as HumanPlayer).ViewPunch( Rotation.FromYaw( Rand.Float( .5f ) - .25f ) * Rotation.FromPitch( Rand.Float( -.25f ) + -.25f ) );
		(Owner as HumanPlayer).ViewPunch(Rand.Float( -.25f ) + -1.25f, Rand.Float( 1f ) - .5f );

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

	public override void SetCarryPosition()
	{
		base.SetCarryPosition();
		// dumb hard-coded positions
		EnableDrawing = true;
		var transform = Transform.Zero;
		transform.Position += Vector3.Right * 5;
		transform.Position += Vector3.Up * 4;
		transform.Position += Vector3.Forward * -3;
		transform.Rotation *= Rotation.FromPitch( 0 );
		transform.Rotation *= Rotation.FromRoll( 270 );
		SetParent( Owner, "leg_upper_R", transform );
	}

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;

		//var shootEase = Easing.EaseIn( lastAttack.LerpInverse( 0.2f, 0.0f ) );
		var shootEase = SpreadMultiplier*1f;
		var color = Color.Lerp( Color.Red, Color.White, lastReload.LerpInverse( 0.0f, 0.4f ) );

		draw.BlendMode = BlendMode.Lighten;
		draw.Color = color.WithAlpha( 0.2f + lastAttack.LerpInverse( 1.2f, 0 ) * 0.5f );

		var length = 8.0f - shootEase * 2.0f;
		var gap = 1.0f + shootEase * 20.0f;
		var thickness = 2.0f;

		draw.Line( thickness, center + Vector2.Left * gap, center + Vector2.Left * (length + gap) );
		draw.Line( thickness, center - Vector2.Left * gap, center - Vector2.Left * (length + gap) );

		draw.Line( thickness, center + Vector2.Up * gap, center + Vector2.Up * (length + gap) );
		draw.Line( thickness, center - Vector2.Up * gap, center - Vector2.Up * (length + gap) );
	}

}
