namespace ZombieHorde;

partial class ThrownMolotov : BasePhysics
{
	public static readonly Model WorldModel = Model.Load( "weapons/licensed/hqfpsweapons/fp_equipment/throwables/molotovcocktail/w_molotov.vmdl" );

	Particles GrenadeParticles;
	private TimeSince TimeSinceBeeped = 0;

	public override void Spawn()
	{
		base.Spawn();
		Tags.Add( "Grenade" );

		Model = WorldModel;
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		EnableTouch = true;

		GrenadeParticles = Particles.Create( "particles/grenade.vpcf", this, "trail_particle", true );
		GrenadeParticles.SetPosition( 0, Position );

		// explode in 10 seconds regardless
		_ = BlowIn( 10 );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );
		if ( other != Owner )
		{
			_ = BlowIn( 0 );
		}
	}

	public async Task BlowIn( float seconds )
	{
		await Task.DelaySeconds( seconds );

		if ( !IsValid ) return;

		Sound.FromWorld( "molotov.break", Position );
		Explode( this, Owner, Position, 700, 100, 1.5f );
		Delete();
	}

	public void Explode( Entity weapon, Entity owner, Vector3 position, float radius, float damage, float forceScale )
	{
		Particles.Create( "particles/explosion/barrel_explosion/explosion_barrel.vpcf", position );

		// same thing as regular explosion but only zombies, don't check los, and don't gib
		var overlaps = Entity.FindInSphere( position, radius ).OfType<CommonZombie>();

		foreach ( var overlap in overlaps )
		{
			if ( overlap is not ModelEntity ent || !ent.IsValid() )
				continue;

			if ( ent.LifeState != LifeState.Alive )
				continue;

			if ( !ent.PhysicsBody.IsValid() )
				continue;

			if ( ent.IsWorld )
				continue;

			var targetPos = ent.PhysicsBody.MassCenter;

			var dist = Vector3.DistanceBetween( position, targetPos );
			if ( dist > radius )
				continue;

			var distanceMul = 1.0f - Math.Clamp( dist / radius, 0.0f, 1.0f );
			var dmg = damage * distanceMul;
			var force = (forceScale * distanceMul) * ent.PhysicsBody.Mass;
			var forceDir = (targetPos - position - Vector3.Down*80).Normal;

			var damageInfo = DamageInfo.FromBullet( position, forceDir * force, dmg )
				.WithWeapon( weapon )
				.WithAttacker( owner );

			ent.TakeDamage( damageInfo );
		}
	}
}
