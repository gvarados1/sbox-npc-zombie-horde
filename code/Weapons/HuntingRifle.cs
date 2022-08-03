namespace ZombieHorde;


/// <summary>
/// Semi-Auto Rifle
/// </summary>
[Library( "zom_huntingrifle" ), HammerEntity]
[EditorModel( "weapons/licensed/hqfpsweapons/fp_equipment/sniperrifles/huntingrifle/w_huntingrifle.vmdl" )]
[Title( "Hunting Rifle" ), Category( "Primary Weapons" )]
partial class HuntingRifle : BaseZomWeapon
{
	public static readonly Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/sniperrifles/huntingrifle/w_huntingrifle.vmdl" );
	public override string ViewModelPath => "weapons/licensed/hqfpsweapons/fp_equipment/sniperrifles/huntingrifle/v_huntingrifle.vmdl";

	public override float PrimaryRate => .5f;
	public override float SecondaryRate => 1.0f;
	public override int ClipSize => 5;
	public override int AmmoMax => 80;
	public override float ReloadTime => 3.2f;
	public override WeaponSlot WeaponSlot => WeaponSlot.Primary;
	public override float BulletSpread => .01f;
	public override float ShotSpreadMultiplier => 2.5f;
	public override string Icon => "weapons/licensed/HQFPSWeapons/Icons/Inventory/Items/Equipment/Icon_M1A.png";
	public override Color RarityColor => WeaponRarity.Uncommon;

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;
		AmmoClip = ClipSize;
		AmmoReserve = AmmoMax;
	}

	public override bool CanPrimaryAttack()
	{
		// slow auto rate, can spam click to fire as fast as possible
		// this opens us up to cheating with autoclickers but it doesn't really matter in this game
		//return base.CanPrimaryAttack() || Input.Pressed( InputButton.PrimaryAttack );
		return base.CanPrimaryAttack();
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;

		if ( !TakeAmmo( 1 ) )
		{
			DryFire();

			if ( AvailableAmmo() > 0 )
			{
				Reload();
			}
			return;
		}

		(Owner as AnimatedEntity).SetAnimParameter( "b_attack", true );

		// Tell the clients to play the shoot effects
		ShootEffects();
		PlaySound( "sniper1.shoot" );
		//PlaySound( "ar3.shoot.tail" );

		// Shoot the bullets
		ShootBullet( BulletSpread, 1f, 60.0f, 12);
		Rand.SetSeed( Time.Tick );
		(Owner as HumanPlayer).ViewPunch(Rand.Float( -.5f ) + -3f, Rand.Float( 1f ) - .5f );
	}

	[ClientRpc]
	protected override async void ShootEffects()
	{
		Host.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

		ViewModelEntity?.SetAnimParameter( "fire", true );
		CrosshairLastShoot = 0;

		await Task.Delay( 1250 );
		if(!IsReloading)
		Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		if ( OverridingAnimator ) return;
		anim.SetAnimParameter( "holdtype", 3 );
		anim.SetAnimParameter( "aim_body_weight", 1.0f );
	}

	public override void SetCarryPosition()
	{
		// dumb hard-coded positions
		EnableDrawing = true;
		var transform = Transform.Zero;
		transform.Position += Vector3.Right * 8.2f;
		transform.Position += Vector3.Down * 3;
		transform.Position += Vector3.Forward * -0;
		transform.Rotation *= Rotation.FromPitch( 220 );
		transform.Rotation *= Rotation.FromYaw( -15 );
		transform.Rotation *= Rotation.FromRoll( -10 );
		SetParent( Owner, "spine_2", transform );
	}

	// todo: set this up better.
	public override IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool underWater = Trace.TestPoint( start, "water" );

		var trace = Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc", "glass", "gib" )
				.Ignore( this )
				.Size( radius );

		//
		// If we're not underwater then we can hit water
		//
		if ( !underWater )
			trace = trace.WithAnyTags( "water" );

		var tr = trace.Run();

		if ( tr.Hit )
			yield return tr;

		// penetrate 5 objects
		var startPos = tr.EndPosition;
		var direction = tr.Direction;
		var hitEnt = tr.Entity;
		for ( int i = 0; i < 5; i++ )
		{
			// penetrate through the world too!
			//if ( tr.Entity is not WorldEntity )
			{
				var trace2 = Trace.Ray( startPos + direction * 15, end )
					.UseHitboxes()
					.WithAnyTags( "solid", "npc", "glass", "gib" )
					.Ignore( this )
					.Ignore( hitEnt )
					.Size( radius );

				var tr2 = trace2.Run();
				if ( tr2.Hit )
					yield return tr2;
				hitEnt = tr2.Entity;
				startPos = tr2.EndPosition;
				direction = tr2.Direction;
				//DebugOverlay.TraceResult( tr2, 5 );
			}
		}
	}

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		// one day I will make my own crosshairs!
		var draw = Render.Draw2D;

		var color = Color.Lerp( Color.Red, Color.White, lastReload.LerpInverse( 0.0f, 0.4f ) );
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
			var gap = -5.0f + shootEase * 22.0f;
			var thickness = 2.0f;

			draw.Line( thickness, center + Vector2.Up * gap + Vector2.Left * length, center + Vector2.Up * gap - Vector2.Left * length );
			draw.Line( thickness, center - Vector2.Up * gap + Vector2.Left * length, center - Vector2.Up * gap - Vector2.Left * length );

			draw.Line( thickness, center + Vector2.Left * gap + Vector2.Up * length, center + Vector2.Left * gap - Vector2.Up * length );
			draw.Line( thickness, center - Vector2.Left * gap + Vector2.Up * length, center - Vector2.Left * gap - Vector2.Up * length );
		}
	}

}
