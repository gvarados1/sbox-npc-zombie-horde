using System.IO;

namespace ZombieHorde;

public partial class CommonZombie : BaseZombie
{
	[ConCmd.Server( "zom_forcehorde" )]
	public static void ForceHorde()
	{
		foreach ( var npc in Entity.All.OfType<CommonZombie>().ToArray() )
		{
			npc.Target = Entity.All.OfType<Player>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault(); // find a random player
			npc.StartChase();
		}
	}

	[ConCmd.Server( "zom_forcewander" )]
	public static void ForceWander()
	{
		foreach ( var npc in Entity.All.OfType<CommonZombie>().ToArray() )
		{
			npc.StartWander();
		}
	}

	public ZombieState ZombieState = ZombieState.Wander;
	public virtual float WalkSpeed => Rand.Float( 40, 50 );
	public float RunSpeed = Rand.Float( 270, 320 );
	public TimeSince TimeSinceAttacked = 0;

	public override void Spawn()
	{
		base.Spawn();

		Tags.Add( "Zombie" );
		SetMaterialGroup( 4 );
		RenderColor = new Color32( (byte)(105 + Rand.Int( 20 )), (byte)(174 + Rand.Int( 20 )), (byte)(59 + Rand.Int( 20 )), 255 ).ToColor();

		UpdateClothes();
		Clothing.DressEntity( this );

		Health = 50;

		StartWander();
	}

	public override void Tick()
	{
		if ( ZombieState == ZombieState.Wander )
		{
			//  randomly play sounds
			if ( Rand.Int( 200 ) == 1 )
				PlaySound( "zombie.attack" );
		}
		else if ( ZombieState == ZombieState.Chase )
		{
			//  temporary move npc
			var distanceToTarget = (Position - Steer.Target).Length;

			// update more often if close to target
			if ( distanceToTarget < 100 )
			{
				if ( !Target.IsValid() ) FindTarget();
				if ( Target.Health <= 0 ) FindTarget();
				Steer.Target = Target.Position;
			}
			else if ( Rand.Int( 10 ) == 1 )
			{
				if ( !Target.IsValid() ) FindTarget();
				if ( Target.Health <= 0 ) FindTarget();
				Steer = new NavSteer();
				//npc.Steer.Target = tr.EndPos;
				Steer.Target = Target.Position;
			}

			//  randomly play sounds
			if ( Rand.Int( 300 ) == 1 )
				PlaySound( "zombie.attack" );

			// attack if near target
			if ( TimeSinceAttacked > .8f ) // todo: scale attack speed with difficulty or the amount of zombies attacking
			{
				if((Position - Target.Position).Length < 80 )
				{
					MeleeAttack();
					TimeSinceAttacked = 0;
				}
			}
		}

		base.Tick();
	}

	// start chase with existing target
	public void StartChase()
	{
		StartChase( Target );
	}
	public void StartChase( Entity targ )
	{
		var target = targ;
		if ( ZombieState == ZombieState.Chase || ZombieState == ZombieState.Lure )
			return;
		SetAnimParameter( "b_jump", true );
		PlaySound( "zombie.attack" );

		ZombieState = ZombieState.Chase;
		Speed = RunSpeed;

		//if ( !target.IsValid() ) FindTarget();
		//if ( target.Health <= 0 ) FindTarget();
		Steer = new NavSteer();
		Steer.Target = target.Position;
	}

	public void StartWander()
	{
		ZombieState = ZombieState.Wander;
		Speed = WalkSpeed;

		var wander = new Nav.Wander();
		wander.MinRadius = 50;
		wander.MaxRadius = 120;
		Steer = wander;
	}

	public void MeleeAttack()
	{
		PlaySound( "zombie.attack" );
		SetAnimParameter("b_attack", true);

		// I don't like using Task.Delay, but it seems like the best option here?. I want the damage to come in slightly after the animation starts. This also gives the player a chance to block
		Task.Delay( 100 );
		Velocity = 0;

		var forward = Rotation.Forward;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * 0.1f;
		forward = forward.Normal;

		foreach ( var tr in TraceMelee( EyePosition, EyePosition + forward * 90, 25 ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 320, 8 )
				.UsingTraceResult( tr )
				.WithAttacker( Owner )
				.WithWeapon( this );

			tr.Entity.TakeDamage( damageInfo );
		}
	}

	public virtual IEnumerable<TraceResult> TraceMelee( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool InWater = Map.Physics.IsPointWater( start );

		var tr = Trace.Ray( start, end )
				.UseHitboxes()
				.HitLayer( CollisionLayer.Water, !InWater )
				.HitLayer( CollisionLayer.Debris )
				.Ignore( Owner )
				.WithoutTags("Zombie")
				.Ignore( this )
				.Size( radius )
				.Run();

		if ( tr.Hit )
			yield return tr;

		//
		// Another trace, bullet going through thin material, penetrating water surface?
		//
	}

	public void FindTarget()
	{
		Target = Entity.All
			.OfType<Player>()               // get all Player entities
			.OrderBy( x => Guid.NewGuid() )     // order them by random
			.FirstOrDefault();                  // take the first one

		if ( Target == null )
		{
			Log.Warning( $"Couldn't find target for {this}!" );
		}
	}

	public override void TakeDamage( DamageInfo info )
	{
		base.TakeDamage( info );
		Velocity = 0;
	}
}

public enum ZombieState
{
	Wander,
	Chase,
	Lure
}
