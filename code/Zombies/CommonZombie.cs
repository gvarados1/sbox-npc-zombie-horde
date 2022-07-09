using Sandbox;
using System.IO;

namespace ZombieHorde;

public partial class CommonZombie : BaseZombie
{
	[ConCmd.Server( "zom_forcehorde" )]
	public static void ForceHorde()
	{
		foreach ( var npc in Entity.All.OfType<CommonZombie>().ToArray() )
		{
			//npc.Target = Entity.All.OfType<Player>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault(); // find a random player
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
	public float RunSpeed = Rand.Float( 260, 280 );
	public TimeSince TimeSinceAttacked = 0;
	public float AttackSpeed = .8f;
	public TimeUntil TimeUntilUnstunned = 0;

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
			if(Steer == null )
			{
				if ( !Target.IsValid() ) FindTarget();
				if ( Target.LifeState == LifeState.Dead ) FindTarget();
				Steer = new NavSteer();
				Steer.Target = Target.Position;
			}
			if ( Target != null )
			{
				// don't do anything if stunned
				if(TimeUntilUnstunned < 0 )
				{
					//  temporary move npc
					var distanceToTarget = (Position - Steer.Target).Length;

					// update more often if close to target
					if ( distanceToTarget < 100 )
					{
						if ( !Target.IsValid() ) FindTarget();
						if ( Target.LifeState == LifeState.Dead ) FindTarget();
						Steer.Target = Target.Position;
					}
					else if ( Rand.Int( 10 ) == 1 )
					{
						if ( !Target.IsValid() ) FindTarget();
						if ( Target.LifeState == LifeState.Dead ) FindTarget();
						Steer = new NavSteer();
						//npc.Steer.Target = tr.EndPos;
						Steer.Target = Target.Position;
					}

					//  randomly play sounds
					if ( Rand.Int( 300 ) == 1 )
						PlaySound( "zombie.attack" );

					// attack if near target
					if ( TimeSinceAttacked > AttackSpeed ) // todo: scale attack speed with difficulty or the amount of zombies attacking
					{
						if ( (Position - Target.Position).Length < 80 )
						{
							MeleeAttack();
							TimeSinceAttacked = 0;
						}
					}
				}
			}
			else
			{
				// do something if we have an invalid target?
				// probably return to wander state or find a new target after x time
			}
		}
		else if ( ZombieState == ZombieState.Lure )
		{
			if ( Steer == null )
			{
				if(BaseGamemode.Ent is SurvivalGamemode )
				{
					StartChase();
				}
				else
				{
					StartWander();
				}
			}
			if ( Target != null )
			{
				// don't do anything if stunned
				if ( TimeUntilUnstunned < 0 )
				{
					//  randomly play sounds
					if ( Rand.Int( 300 ) == 1 )
						PlaySound( "zombie.attack" );
				}
			}
			else
			{
				// do something if we have an invalid target?
				// probably return to wander state or find a new target after x time
			}
		}


		// random deletion checks
		if ( Rand.Int( 500 ) == 1 )
		{
			CheckForDeletion();	
		}
		base.Tick();
	}

	public void StartLure(Vector3 position )
	{
		ZombieState = ZombieState.Lure;
		Speed = RunSpeed;

		Steer = new NavSteer();
		Steer.Target = position;
	}
	public void Stun(float seconds )
	{
		TimeUntilUnstunned = seconds;
		Steer = null;
	}

	// start chase with existing target
	public void StartChase()
	{
		if(Target == null ) FindTarget();
		StartChase( Target );
	}
	public void StartChase( Entity targ )
	{
		Target = targ;
		if ( Target == null )
		{
			Log.Warning( "Invalid Target for: " + this );
			return;
		}

			if ( ZombieState == ZombieState.Chase || ZombieState == ZombieState.Lure )
			return;
		SetAnimParameter( "b_jump", true );
		PlaySound( "zombie.attack" );

		ZombieState = ZombieState.Chase;
		Speed = RunSpeed;

		Steer = new NavSteer();
		Steer.Target = Target.Position;

		// chance to alert nearby zombies
		TryAlertNearby( Target, .1f, 800 ); // 800 good range??
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

	public override void HitBreakableObject()
	{
		//base.HitBreakableObject();
		if ( TimeSinceAttacked > AttackSpeed )
		{
			TimeSinceAttacked = 0;
			MeleeAttack();
		}	
	}

	public async void MeleeAttack()
	{
		// initial delay too?
		await Task.Delay( 100 );
		if ( !IsValid ) return;
		PlaySound( "zombie.attack" );
		SetAnimParameter("b_attack", true);
		Velocity = 0;

		// I don't like using Task.Delay, but it seems like the best option here?. I want the damage to come in slightly after the animation starts. This also gives the player a chance to block
		await Task.Delay( 200 );
		if ( !IsValid ) return;
		Velocity = 0;

		var forward = Rotation.Forward;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * 0.1f;
		forward = forward.Normal;

		foreach ( var tr in TraceMelee( EyePosition, EyePosition + forward * 90, 50 ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 320, 8 )
				.UsingTraceResult( tr )
				.WithAttacker( this )
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
		// todo: prioritize alive players instead of incompacitated, but still sometimes go for incompacitated
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
		TryAlert( info.Attacker, .5f );
		base.TakeDamage( info );
		Velocity = 0;
	}

	public void CheckForDeletion()
	{
		// check if too far away from players
		var ply = Entity.FindInSphere( Position, 5000 )
			.OfType<Player>()
			.Count();

		if(ply == 0 )
		{
			Log.Info( "Zombie too far away from players, deleting: " + this );
			Delete();
		}
	}

	public bool TryAlert(Entity target, float percent)
	{
		if ( ZombieState == ZombieState.Lure ) return false;

		if (Rand.Float(1) < percent )
		{
			StartChase(target);

			return true;
		}
		return false;
	}

	public void TryAlertNearby(Entity target, float percent, float radius )
	{
		foreach( CommonZombie zom in Entity.FindInSphere(Position, radius).OfType<CommonZombie>() )
		{
			var chance = percent; // todo: decrease chance further away from position;
			zom.TryAlert( target, chance );
		}
	}
}

public enum ZombieState
{
	Wander,
	Chase,
	Lure
}
