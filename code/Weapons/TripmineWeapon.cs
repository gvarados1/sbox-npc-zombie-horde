using System.ComponentModel.DataAnnotations;

namespace ZombieHorde;

/// <summary>
/// Place on wall and explode
/// </summary>
[Library( "zom_tripmine" ), HammerEntity]
[EditorModel( "models/dm_tripmine.vmdl" )]
[Title( "Tripmine" ), Category( "Grenades" )]
partial class TripmineWeapon : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "models/dm_tripmine.vmdl" );
	public override string ViewModelPath => "models/v_dm_tripmine.vmdl";

	public override float PrimaryRate => 100.0f;
	public override float SecondaryRate => 100.0f;
	public override float ReloadTime => 0.1f;
	public override int ClipSize => 1;
	public override WeaponSlot WeaponSlot => WeaponSlot.Grenade;
	public override int AmmoMax => 3;
	public override float BulletSpread => 0.2f;
	public override float ShotSpreadMultiplier => 1.5f;
	public override string Icon => "/ui/weapons/tripmine.png";
	public override Color RarityColor => WeaponRarity.Rare;
	public override Transform ViewModelOffsetDuck => Transform.WithPosition( new Vector3( 0f, -4f, 2.5f ) ).WithRotation( new Angles( 0f, 0f, 160 ).ToRotation() );
	public override bool UseAlternativeSprintAnimation => true;

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;
		AmmoReserve = AmmoMax;
	}

	public override bool CanPrimaryAttack()
	{
		return Input.Released( InputButton.PrimaryAttack );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if ( Owner is not HumanPlayer player ) return;

		// woosh sound
		// screen shake

		Rand.SetSeed( Time.Tick );

		var tr = Trace.Ray( Owner.EyePosition, Owner.EyePosition + Owner.EyeRotation.Forward * 150 )
				.Ignore( Owner )
				.WithoutTags("trigger")
				.Run();

		if ( !tr.Hit )
			return;

		if ( !tr.Entity.IsWorld )
			return;

		if ( IsServer )
		{
			var grenade = new Tripmine
			{
				Position = tr.EndPosition,
				Rotation = Rotation.LookAt( tr.Normal, Vector3.Up ),
				Owner = Owner
			};

			_ = grenade.Arm( 1.0f );
		}

		TakeAmmo( 1 );
		Reload();

		if ( IsServer && AmmoClip == 0 && AmmoReserve == 0 )
		{
			Delete();
			player.SwitchToBestWeapon();
		}
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		if ( OverridingAnimator ) return;
		anim.SetAnimParameter( "holdtype", 4 ); // TODO this is shit
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
	}
}
