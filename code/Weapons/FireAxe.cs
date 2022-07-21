using System.ComponentModel.DataAnnotations;

namespace ZombieHorde;

[Library( "zom_fireaxe" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/meleeweapons/fireaxe/w_fireaxe.vmdl" )]
[Title( "Fire Axe" ), Category( "Weapons" )]
partial class FireAxe : BaseZomWeapon
{
	public static Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/meleeweapons/fireaxe/w_fireaxe.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/meleeweapons/fireaxe/v_fireaxe.vmdl";

	public override float PrimaryRate => .9f;
	public override float SecondaryRate => 1.0f;
	public override float ReloadTime => 3.0f;
	public override int ClipSize => 0;
	public override int AmmoMax => -2;
	public override WeaponSlot WeaponSlot => WeaponSlot.Secondary;
	public override string Icon => "weapons/licensed/HQFPSWeapons/Icons/Inventory/Items/Equipment/Icon_OldFireAxe.png";
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
		TimeSinceSecondaryAttack = 0;

		// woosh sound
		// screen shake
		PlaySound( "dm.crowbar_attack" );

		Rand.SetSeed( Time.Tick );

		var forward = Owner.EyeRotation.Forward;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * 0.1f;
		forward = forward.Normal;

		(Owner as HumanPlayer).ViewPunch( Rotation.FromYaw( Rand.Float( .1f ) + .5f ) * Rotation.FromPitch( Rand.Float( 1f ) + -.5f ) );
		foreach ( var tr in TraceBullet( Owner.EyePosition, Owner.EyePosition + forward * 120, 15 ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 32, 40 )
				.UsingTraceResult( tr )
				.WithAttacker( Owner )
				.WithWeapon( this );

			tr.Entity.TakeDamage( damageInfo );
		}

		ViewModelEntity?.SetAnimParameter( "fire", true );

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
}
