namespace ZombieHorde;

[Library( "zom_crossbow" ), HammerEntity]
[EditorModel( "weapons/rust_crossbow/rust_crossbow.vmdl" )]
[Title( "Crossbow" ), Category( "Weapons" )]
partial class Crossbow : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/rust_crossbow/rust_crossbow.vmdl" );
	public override string ViewModelPath => "weapons/rust_crossbow/v_rust_crossbow.vmdl";

	public override float PrimaryRate => 1;
	public override int ClipSize => 5;

	public override WeaponSlot WeaponSlot => WeaponSlot.Primary;

	public override int AmmoMax => 40;
	public override float BulletSpread => 0.2f;
	public override float ShotSpreadMultiplier => 1.5f;
	public override string Icon => "/ui/weapons/dm_crossbow.png";

	[Net, Predicted]
	public bool Zoomed { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;
		AmmoReserve = AmmoMax;
	}

	public override void AttackPrimary()
	{
		if ( !TakeAmmo( 1 ) )
		{
			DryFire();

			if ( AvailableAmmo() > 0 )
			{
				Reload();
			}
			return;
		}

		ShootEffects();
		PlaySound( "rust_crossbow.shoot" );
		(Owner as AnimatedEntity).SetAnimParameter( "b_attack", true );

		// TODO - if zoomed in then instant hit, no travel, 120 damage


		if ( IsServer )
		{
			var eyeRot = Owner.EyeRotation;

			var bolt = new CrossbowBolt();
			bolt.Position = Owner.EyePosition;
			bolt.Rotation = eyeRot;
			bolt.Owner = Owner;
			bolt.Velocity = eyeRot.Forward * 100;
		}
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );
		if ( Input.Pressed( InputButton.Zoom ) )
		{
			Zoomed = !Zoomed;
		}
	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		if ( Zoomed )
		{
			camSetup.FieldOfView = 20;
			camSetup.ViewModel.FieldOfView = 40;
			Input.Rotation *= Rotation.FromPitch( 5 );
			camSetup.Rotation *= Rotation.FromPitch( 4 );
		}
	}

	public override void BuildInput( InputBuilder owner )
	{
		if ( Zoomed )
		{
			owner.ViewAngles = Angles.Lerp( owner.OriginalViewAngles, owner.ViewAngles, 0.2f );
		}
	}

	[ClientRpc]
	protected override void ShootEffects()
	{
		Host.AssertClient();

		ViewModelEntity?.SetAnimParameter( "fire", true );
		CrosshairLastShoot = 0;
	}

	TimeSince timeSinceZoomed;

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;

		if ( Zoomed )
			timeSinceZoomed = 0;

		var zoomFactor = timeSinceZoomed.Relative.LerpInverse( 0.4f, 0 );

		var color = Color.Lerp( Color.Red, Color.Yellow, lastReload.LerpInverse( 0.0f, 0.4f ) );
		draw.BlendMode = BlendMode.Lighten;
		draw.Color = color.WithAlpha( 0.2f + CrosshairLastShoot.Relative.LerpInverse( 1.2f, 0 ) * 0.5f );

		// outer lines
		{
			var shootEase = Easing.EaseInOut( lastAttack.LerpInverse( 0.4f, 0.0f ) );
			var length = 10.0f;
			var gap = 40.0f + shootEase * 50.0f;

			gap -= zoomFactor * 20.0f;


			draw.Line( 0, center + Vector2.Up * gap, length, center + Vector2.Up * (gap + length) );
			draw.Line( 0, center - Vector2.Up * gap, length, center - Vector2.Up * (gap + length) );

			draw.Color = draw.Color.WithAlpha( draw.Color.a * zoomFactor );

			for ( int i = 0; i < 4; i++ )
			{
				gap += 40.0f;

				draw.Line( 0, center - Vector2.Left * gap, length, center - Vector2.Left * (gap + length) );
				draw.Line( 0, center + Vector2.Left * gap, length, center + Vector2.Left * (gap + length) );

				draw.Color = draw.Color.WithAlpha( draw.Color.a * 0.5f );
			}
		}
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		anim.SetAnimParameter( "holdtype", 3 ); // TODO this is shit
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
	}
}
