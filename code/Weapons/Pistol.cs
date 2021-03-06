namespace ZombieHorde;

[Library( "zom_pistol" ), HammerEntity]
[EditorModel( "weapons/rust_pistol/rust_pistol.vmdl" )]
[Title( "Pistol" ), Category( "Weapons" )]
partial class Pistol : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/rust_pistol/rust_pistol.vmdl" );
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	public override float PrimaryRate => 12.0f;
	public override float SecondaryRate => 4.5f;
	public override float ReloadTime => 2.2f;
	public override WeaponSlot WeaponSlot => WeaponSlot.Secondary;
	public override float BulletSpread => .1f;
	public override float ShotSpreadMultiplier => 2f;
	public override int ClipSize => 14;
	public override int AmmoMax => -1;
	public override string Icon => "/ui/weapons/pistol.png";

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
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
		PlaySound( "rust_pistol.shoot" );

		//
		// Shoot the bullets
		//
		ShootBullet( BulletSpread, 1, 12.0f, 2.0f );

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
