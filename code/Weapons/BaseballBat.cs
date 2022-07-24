using System.ComponentModel.DataAnnotations;

namespace ZombieHorde;

[Library( "zom_baseballbat" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/meleeweapons/baseballbat/w_baseballbat.vmdl" )]
[Title( "Baseball Bat" ), Category( "Weapons" )]
partial class BaseballBat : BaseZomWeapon
{
	public static Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/meleeweapons/baseballbat/w_baseballbat.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/meleeweapons/baseballbat/v_baseballbat.vmdl";

	public override float PrimaryRate => 2.0f;
	public override float SecondaryRate => 1.0f;
	public override float ReloadTime => 3.0f;
	public override int ClipSize => 0;
	public override int AmmoMax => -2;
	public override WeaponSlot WeaponSlot => WeaponSlot.Secondary;
	public override string Icon => "weapons/licensed/HQFPSWeapons/Icons/Inventory/Items/Equipment/Icon_BaseballBat.png";
	public override Color RarityColor => WeaponRarity.Common;

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = 0;
		if(Rand.Int(1) == 1 )
		{
			SetMaterialGroup( 1 );
		}
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );
		if(GetMaterialGroup() == 1)
		ViewModelEntity?.SetMaterialGroup( 1 );
	}

	public override bool CanPrimaryAttack()
	{
		return base.CanPrimaryAttack();
	}

	public override void Simulate( Client player )
	{
		if ( CanReload() )
		{
			Reload();
		}

		//
		// Reload could have changed our owner
		//
		if ( !Owner.IsValid() )
			return;

		if ( CanPrimaryAttack() )
		{
			using ( LagCompensation() )
			{
				AttackPrimary();
			}
		}

		//
		// AttackPrimary could have changed our owner
		//
		if ( !Owner.IsValid() )
			return;

		if ( CanSecondaryAttack() )
		{
			using ( LagCompensation() )
			{
				TimeSinceSecondaryAttack = 0;
				AttackSecondary();
			}
		}
	}

	bool AltSwing = false;

	public override void AttackPrimary()
	{
		// woosh sound
		// screen shake
		PlaySound( "dm.crowbar_attack" );

		Rand.SetSeed( Time.Tick );

		var forward = Owner.EyeRotation.Forward;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * 0.1f;
		forward = forward.Normal;

		(Owner as HumanPlayer).ViewPunch( Rotation.FromYaw( Rand.Float( .25f ) + .25f ) * Rotation.FromPitch( Rand.Float( .5f ) + -.25f ) );
		foreach ( var tr in TraceBullet( Owner.EyePosition, Owner.EyePosition + forward * 100, 30 ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 32, 24 )
				.UsingTraceResult( tr )
				.WithAttacker( Owner )
				.WithWeapon( this );

			if ( tr.Entity is CommonZombie zom )
			{
				zom.Stun( 1f );
				zom.Velocity = forward * 100;
				if ( zom.ZombieState == ZombieState.Wander )
					damageInfo.Damage *= 1.5f;
			}

			tr.Entity.TakeDamage( damageInfo );
		}

		if ( TimeSincePrimaryAttack < .8f || TimeSinceSecondaryAttack < .8f )
			AltSwing = !AltSwing;
		else
			AltSwing = false;

		if ( !AltSwing )
		{
			ViewModelEntity?.SetAnimParameter( "fire", true );
		}
		else
		{
			ViewModelEntity?.SetAnimParameter( "reload", true );
		}

		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = .25f;


		if ( Owner is HumanPlayer player )
		{
			player.SetAnimParameter( "b_attack", true );
		}
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		anim.SetAnimParameter( "holdtype", 5 ); // TODO this is shit
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
	}

	public override void SetCarryPosition()
	{
		// dumb hard-coded positions
		EnableDrawing = true;
		var transform = Transform.Zero;
		transform.Position += Vector3.Right * 8.5f;
		transform.Position += Vector3.Down * -4;
		transform.Position += Vector3.Forward * 5;
		transform.Rotation *= Rotation.FromPitch( 220 );
		transform.Rotation *= Rotation.FromYaw( -15 );
		transform.Rotation *= Rotation.FromRoll( -10 );
		SetParent( Owner, "spine_2", transform );
	}
}
