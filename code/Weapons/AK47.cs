namespace ZombieHorde;

[Library( "zom_ak47"), HammerEntity]
[EditorModel( "weapons/ak47/ak47.vmdl" )]
[Title( "AK-47" ), Category( "Weapons" )]
partial class AK47 : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/ak47/ak47.vmdl" );
	public override string ViewModelPath => "weapons/ak47/v_ak47.vmdl";

	public override float PrimaryRate => 9.0f;
	public override float SecondaryRate => 1.0f;
	public override int ClipSize => 30;
	public override float ReloadTime => 2.0f;
	public override WeaponSlot WeaponSlot => WeaponSlot.Primary;

	public override int AmmoMax => 210;
	public override float BulletSpread => .1f;
	public override float ShotSpreadMultiplier => 1.5f;
	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;
		AmmoReserve = AmmoMax;
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;

		//* infinite ammo
		if ( !TakeAmmo( 1 ) )
		{
			DryFire();

			if ( AvailableAmmo() > 0 )
			{
				Reload();
			}
			return;
		}
		//*/

		(Owner as AnimatedEntity).SetAnimParameter( "b_attack", true );

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();
		PlaySound( "ak47.shoot" );

		//
		// Shoot the bullets
		//
		ShootBullet( BulletSpread, .5f, 12.0f, 3.0f );

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

	public override void SimulateAnimator( PawnAnimator anim )
	{
		anim.SetAnimParameter( "holdtype", 2 ); // TODO this is shit
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
	}

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		// one day I will make my own crosshairs!
		var draw = Render.Draw2D;

		var color = Color.Lerp( Color.Red, Color.Yellow, lastReload.LerpInverse( 0.0f, 0.4f ) );
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
			var length = 3.0f + shootEase * 2.0f;
			//var gap = 30.0f + shootEase * 50.0f;
			var gap = 5.0f + shootEase * 20.0f;
			var thickness = 2.0f;

			draw.Line( thickness, center + Vector2.Up * gap + Vector2.Left * length, center + Vector2.Up * gap - Vector2.Left * length );
			draw.Line( thickness, center - Vector2.Up * gap + Vector2.Left * length, center - Vector2.Up * gap - Vector2.Left * length );

			draw.Line( thickness, center + Vector2.Left * gap + Vector2.Up * length, center + Vector2.Left * gap - Vector2.Up * length );
			draw.Line( thickness, center - Vector2.Left * gap + Vector2.Up * length, center - Vector2.Left * gap - Vector2.Up * length );
		}
	}


}
