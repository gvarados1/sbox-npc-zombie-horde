namespace ZombieHorde;

[Library( "zom_revolver" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/handguns/revolver/w_revolver.vmdl" )]
[Title( "Magnum Revolver" ), Category( "Weapons" )]
partial class Revolver : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/handguns/revolver/w_revolver.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/handguns/revolver/v_revolver.vmdl";

	public override float PrimaryRate => 20.0f;
	public override float SecondaryRate => 1.0f;
	public override float ReloadTime => 2.8f;
	public override WeaponSlot WeaponSlot => WeaponSlot.Secondary;
	public override float BulletSpread => .05f;
	public override float ShotSpreadMultiplier => 5f;
	public override float ShotSpreadLerp => .1f;
	public override int ClipSize => 6;
	public override int AmmoMax => -1;
	public override string Icon => "weapons/licensed/HQFPSWeapons/Icons/Inventory/Items/Equipment/Icon_Revolver.png";
	public override Color RarityColor => WeaponRarity.Common;

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;

		if ( Rand.Int( 1 ) == 1 )
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
		PlaySound( "revolver.shoot" );
		PlaySound( "pistol.shoot.tail" );

		//
		// Shoot the bullets
		//
		ShootBullet( BulletSpread, 1, 40.0f);
		Rand.SetSeed( Time.Tick );
		//(Owner as HumanPlayer).ViewPunch( Rotation.FromYaw( Rand.Float( .5f ) - .25f ) * Rotation.FromPitch( Rand.Float( -.25f ) + -.25f ) );
		(Owner as HumanPlayer).ViewPunch( Rand.Float( -.25f ) + -1.0f, Rand.Float( .5f ) - .25f );

	}

	public async override void StartReloadEffects()
	{
		base.StartReloadEffects();
		await Task.Delay( 1000 );

		Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
		Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
		Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
		Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
	}

	public override void AdjustAccuracyMultiplier()
	{
		if ( Owner is HumanPlayer ply )
		{
			var controller = ply.Controller as HumanWalkController;
			var targetMultipler = 1f;

			// hack: floor velocity to limit prediction errors
			var adjustedVelocity = MathF.Floor( ply.Velocity.WithZ( 0 ).Length );

			targetMultipler = Math.Min( adjustedVelocity / controller.WalkSpeed + 1, 3.5f ) * 1.8f - .4f;


			if ( adjustedVelocity == 0 )
			{
				targetMultipler *= .5f;
			}
			if ( controller.GroundEntity == null )
			{
				targetMultipler *= 2.2f;
			}
			else if ( controller.Duck.IsActive )
			{
				targetMultipler *= .5f;
			}

			// prediction issue: velocity gets set to 0 when attacked. this can not be predicted! what do I do?
			SpreadMultiplier = SpreadMultiplier.LerpTo( targetMultipler, ShotSpreadLerp );

			//SpreadMultiplier = MathF.Floor( SpreadMultiplier * 1000 ) / 1000;
			SpreadMultiplier = SpreadMultiplier.Clamp( 0, 12 );

			//Log.Info( SpreadMultiplier + ", " + targetMultipler);
		}
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		if ( OverridingAnimator ) return;
		anim.SetAnimParameter( "holdtype", 1 );
		anim.SetAnimParameter( "holdtype_attack", 2 );
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
		anim.SetAnimParameter( "holdtype_handedness", 0 );
	}

	public override void SetCarryPosition()
	{
		// dumb hard-coded positions
		EnableDrawing = true;
		var transform = Transform.Zero;
		transform.Position += Vector3.Right * 0;
		transform.Position += Vector3.Up * 4.25f;
		transform.Position += Vector3.Forward * 3f;
		transform.Rotation *= Rotation.FromPitch( 0 );
		transform.Rotation *= Rotation.FromRoll( 270 );
		SetParent( Owner, "leg_upper_R", transform );
	}

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;

		var shootEase = SpreadMultiplier * 1f;
		var color = Color.Lerp( Color.Red, Color.White, lastReload.LerpInverse( 0.0f, 0.4f ) );

		draw.BlendMode = BlendMode.Lighten;
		draw.Color = color.WithAlpha( 0.2f + CrosshairLastShoot.Relative.LerpInverse( 1.2f, 0 ) * 0.5f );

		//var length = 3.0f + shootEase * 5.0f;
		var length = 3.0f + shootEase * 5.0f;

		draw.Ring( center, length, length - 3.0f );
	}

}
