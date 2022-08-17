namespace ZombieHorde;

/// <summary>
/// Alerts Zombies, Explodes.
/// </summary>
[Library( "zom_pipebomb" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/throwables/pipebomb/w_pipebomb.vmdl" )]
[Title( "Pipe Bomb" ), Category( "Grenades" )]
partial class PipeBomb : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/throwables/pipebomb/w_pipebomb.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/throwables/pipebomb/v_pipebomb.vmdl";

	public override float PrimaryRate => 1.0f;
	public override float SecondaryRate => 1.0f;
	public override float ReloadTime => 1.0f;
	public override int ClipSize => 1;
	public override WeaponSlot WeaponSlot => WeaponSlot.Grenade;
	public override int AmmoMax => 0;
	public override string Icon => "/ui/weapons/zom_pipebomb.png";
	public override Color RarityColor => WeaponRarity.Rare;
	public override Transform ViewModelOffsetDuck => Transform.WithPosition( new Vector3( 0f, -4f, 2.5f ) ).WithRotation( new Angles( 0f, 0f, 160 ).ToRotation() );
	public override Transform ViewModelOffset => Transform.WithPosition( new Vector3( 0f, -2f, 2.5f ) ).WithRotation( new Angles( 0f, 0f, 50f ).ToRotation() );
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

	public async override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;

		if ( Owner is not HumanPlayer player ) return;

		if ( !TakeAmmo( 1 ) )
		{
			Reload();
			return;
		}

		// woosh sound
		// screen shake

		//PlaySound( "dm.grenade_throw" );
		ViewModelEntity?.SetAnimParameter( "fire", true );
		PlaySound( "rust_boneknife.attack" );
		PlaySound( "pipebomb.activate" );

		player.SetAnimParameter( "b_attack", true );
		player.AddDamageResistance( 3 );

		Rand.SetSeed( Time.Tick );

		await Task.Delay( 300 );

		if ( IsServer )
			using ( Prediction.Off() )
			{
				var grenade = new ThrownPipeBomb
				{
					Position = Owner.EyePosition + Owner.EyeRotation.Forward * 3.0f,
					Owner = Owner
				};

				grenade.PhysicsBody.Velocity = Owner.EyeRotation.Forward * 600.0f + Owner.EyeRotation.Up * 200.0f + Owner.Velocity.WithZ(0);

				_ = grenade.BlowIn( 8.0f );
			}

		await Task.Delay( 100 );

		Reload();

		if ( IsServer && AmmoClip == 0 && AmmoReserve == 0 )
		{
			Delete();
			player.SwitchToBestWeapon();
		}
	}

	public override void SetCarryPosition()
	{
		base.SetCarryPosition();
		// dumb hard-coded positions
		EnableDrawing = true;
		var transform = Transform.Zero;
		transform.Position += Vector3.Right * 0;
		transform.Position += Vector3.Up * -4;
		transform.Position += Vector3.Forward * -3;
		transform.Rotation *= Rotation.FromPitch( 0 );
		transform.Rotation *= Rotation.FromRoll( 270 );
		SetParent( Owner, "leg_upper_L", transform );
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

	public override void SimulateAnimator( PawnAnimator anim )
	{
		if ( OverridingAnimator ) return;
		anim.SetAnimParameter( "holdtype", 4 );
		anim.SetAnimParameter( "aimat_weight", 1.0f );
		anim.SetAnimParameter( "holdtype_attack", 2.0f );
		anim.SetAnimParameter( "holdtype_handedness", 1 );
	}
}
