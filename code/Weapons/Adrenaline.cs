namespace ZombieHorde;

/// <summary>
/// Provides a temporary speed boost, Allows the player to preform actions faster, and elimates effects that slow the player down
/// </summary>
[Library( "zom_adrenaline" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/healingitems/syringe/w_syringe.vmdl" )]
[Title( "Adrenaline" ), Category( "Healing" ), Icon( "favorite" )]
partial class Adrenaline : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/healingitems/syringe/w_syringe.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/healingitems/syringe/v_syringe.vmdl";

	public override float PrimaryRate => 1.0f;
	public override float SecondaryRate => 1.0f;
	public override float ReloadTime => 1.0f;
	public override int ClipSize => 1;
	//public override WeaponSlot WeaponSlot => WeaponSlot.Pills;
	public override WeaponSlot WeaponSlot => WeaponSlot.Medkit;
	public override int AmmoMax => 0;
	public override string Icon => "/ui/weapons/adrenaline.png";
	public override Color RarityColor => WeaponRarity.Rare;
	public override Transform ViewModelOffsetDuck => Transform.WithPosition( new Vector3( 0f, -4f, 2.5f ) ).WithRotation( new Angles( 0f, 0f, 160 ).ToRotation() );
	public override bool UseAlternativeSprintAnimation => true;

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

	public override bool CanPrimaryAttack()
	{
		return Input.Released( InputButton.PrimaryAttack );
	}

	public async override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;

		if ( Owner is not HumanPlayer player ) return;

		if ( !TakeAmmo( 1 ) )
		{
			Reload();
			return;
		}

		ViewModelEntity?.SetAnimParameter( "fire", true );
		PlaySound( "rust_syringe.inject" );


		Rand.SetSeed( Time.Tick );
		(Owner as HumanPlayer).ViewPunch( Rand.Float( .25f ) + .3f, Rand.Float( .25f ) + .25f );

		player.SetAnimParameter( "b_attack", true );

		await Task.Delay( 300 );
		if ( IsServer )
		{
			Owner.Health += 25;
			if ( Owner.Health > 100 )
				Owner.Health = 100;

			(Owner as HumanPlayer).Stamina += 25;
		}
		(Owner as HumanPlayer).TimeUntilAdrenalineExpires = 15;

		await Task.Delay( 700 );

		Reload();

		if ( IsServer && AmmoClip == 0 && AmmoReserve == 0 )
		{
			await Task.Delay( 730 );
			Delete();
			player.SwitchToBestWeapon();
		}
	}

	public override void Reload()
	{
		if ( IsReloading )
			return;

		if ( AmmoClip >= ClipSize )
			return;

		if ( AmmoReserve <= 0 && AmmoMax != -1 )
		{
			return;
		}

		TimeSinceReload = 0;

		IsReloading = true;

		StartReloadEffects();
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

	public override void SetCarryPosition()
	{
		base.SetCarryPosition();
		// dumb hard-coded positions
		EnableDrawing = true;
		var transform = Transform.Zero;
		transform.Position += Vector3.Right * 1;
		transform.Position += Vector3.Up * -4;
		transform.Position += Vector3.Forward * 0;
		transform.Rotation *= Rotation.FromPitch( -40 );
		transform.Rotation *= Rotation.FromRoll( 270 );
		SetParent( Owner, "leg_upper_L", transform );
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		if ( OverridingAnimator ) return;
		anim.SetAnimParameter( "holdtype", 4 );
		anim.SetAnimParameter( "aimat_weight", 1.0f );
		anim.SetAnimParameter( "holdtype_attack", 0.0f );
		anim.SetAnimParameter( "holdtype_handedness", 1 );
		anim.SetAnimParameter( "holdtype_pose_hand", .07f );
	}
}
