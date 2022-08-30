using Sandbox;
using System.IO;
using ZombieHorde.Nav;

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

	public override void Spawn()
	{
		base.Spawn();

		//RenderColor = new Color32( (byte)(105 + Rand.Int( 20 )), (byte)(174 + Rand.Int( 20 )), (byte)(59 + Rand.Int( 20 )), 255 ).ToColor();

		UpdateClothes();
		Dress();

		Health = 50;

		StartWander();

		var gm = BaseGamemode.Current;
		Health *= gm.ZomHealthMultiplier;
		RunSpeed *= gm.ZomSpeedMultiplier;
		TimeSinceMoan = -Rand.Float( 1f );
	}

	public TimeSince TimeSinceMoan = 0;
	public bool JustSpawned = true;

	public override void Tick()
	{
		if ( ZombieState == ZombieState.Wander )
		{
			if(Steer == null )
			{
				StartWander();
			}
			else
			{
				if ( JustSpawned )
				{
					if ( Steer is Wander wander )
					{
						wander.FindNewTarget( Position );
						JustSpawned = false;
					}
				}

				//  randomly play sounds
				if ( TimeSinceMoan > 2.4f )
				{
					TimeSinceMoan = 0 - Rand.Float( .5f );
					PlaySoundOnClient( "zombie.moan" );
				}

				if ( Steer.Path.IsEmpty && TimeSinceLongIdle > 5f )
				{
					if ( Rand.Int( 30 ) == 0 )
					{
						Steer.TimeUntilCanMove = 5;
						SetAnimParameter( "b_longidle", true );
						TimeSinceLongIdle = 0;
					}
				}
			}
		}
		else if ( ZombieState == ZombieState.Chase )
		{
			if (Steer == null )
			{
				if ( !Target.IsValid() ) FindTarget();
				if ( Target.LifeState == LifeState.Dead ) FindTarget();
				Steer = new NavSteer();
				Steer.Target = Target.Position;
			}
			if ( Target.IsValid() )
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
						if ( TimeSinceClimb > .5f )
							Steer.Target = Target.Position;
					}
					else if ( Rand.Int( 10 ) == 1 )
					{
						if ( !Target.IsValid() ) FindTarget();
						if ( Target.LifeState == LifeState.Dead ) FindTarget();
						if( TimeSinceClimb > .5f )
						{
							Steer = new NavSteer();
							//npc.Steer.Target = tr.EndPos;
							Steer.Target = Target.Position;
						}
					}

					// check if we're on the navmesh
					//if(Steer.Output.Direction == 0 )
					//if(NavMesh.GetClosestPoint(Position) == null)
					Vector3 pNearestPosOut = Vector3.Zero;
					NavArea closestNav = NavArea.GetClosestNav( Position, NavAgentHull.Default, GetNavAreaFlags.NoFlags, ref pNearestPosOut, 200, 600, 70, 16 );
					if(!closestNav.Valid)
					{
						Steer = null;
						TryPathOffNav();
					}
					else if( Steer.Output.Finished && (Position - Target.Position).Length > 70 )
					{
						Steer = null;
						TryPathOffNav();
					}
					//  randomly play sounds
					if(TimeSinceMoan > 1.4f )
					{
						TimeSinceMoan = 0 - Rand.Float( .5f );
						PlaySoundOnClient( "zombie.moan" );
					}

					// attack if near target
					if ( TimeSinceAttacked > AttackSpeed && TimeUntilUnstunned < 0 ) // todo: scale attack speed with difficulty or the amount of zombies attacking
					{
						var range = 60;
						if ( (Position - Target.Position).Length < range || (EyePosition - Target.Position ).Length < range )
						{
							TryMeleeAttack();
							TimeSinceAttacked = -3;
						}
					}
				}
			}
			else
			{
				// do something if we have an invalid target?
				// probably return to wander state or find a new target after x time
				FindTarget();
			}
		}
		else if ( ZombieState == ZombieState.Lure )
		{
			if ( Steer == null || Steer.Output.Finished )
			{
				if(BaseGamemode.Current is SurvivalGamemode )
				{
					StartWander();
					StartChase();
				}
				else
				{
					StartWander();
				}
			}
			if ( Target.IsValid() )
			{
				// don't do anything if stunned
				if ( TimeUntilUnstunned < 0 )
				{
					//  randomly play sounds
					if ( Rand.Int( 300 ) == 1 )
						PlaySoundOnClient( "zombie.attack" );
				}
			}
			else
			{
				// do something if we have an invalid target?
				// probably return to wander state or find a new target after x time
			}
		}
		else if (ZombieState == ZombieState.Burning )
		{
			if(TimeSinceBurnTicked > .5f )
			{
				TimeSinceBurnTicked = 0;
				SetAnimParameter( "b_jump", true );
				Velocity *= .9f;

				RenderColor = Color.Lerp( RenderColor, Color.Black, .15f );

				foreach ( var clothing in Children.OfType<ModelEntity>() )
				{
					if ( clothing.Tags.Has( "clothes" ) )
					{
						clothing.RenderColor = RenderColor;
					}
				}

				PlaySoundOnClient( "zombie.hurt" );

				Health *= .75f;
				if ( Health <= 5 )
				{
					OnKilled();
					BaseGamemode.Current.ZombiesRemaining--;
				}
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
		if ( ZombieState == ZombieState.Burning ) return;

		ZombieState = ZombieState.Lure;
		Speed = RunSpeed * 1.2f;
		SetAnimParameter( "b_jump", true );

		Steer = new NavSteer();
		Steer.Target = position;
	}
	public void Stun(float seconds )
	{
		SetAnimParameter( "b_shoved", true );
		TimeUntilUnstunned = seconds;
		Steer = null;
	}

	// start chase with existing target
	public void StartChase()
	{
		if(!Target.IsValid()) FindTarget();
		StartChase( Target );
	}
	public void StartChase( Entity targ )
	{
		Target = targ;
		if ( !Target.IsValid() )
		{
			Log.Warning( "Invalid Target for: " + this );
			return;
		}

		if ( ZombieState == ZombieState.Chase || ZombieState == ZombieState.Lure || ZombieState == ZombieState.Burning )
			return;
		SetAnimParameter( "b_jump", true );
		PlaySoundOnClient( "zombie.attack" );

		ZombieState = ZombieState.Chase;
		Speed = RunSpeed;

		Steer = new NavSteer();
		Steer.Target = Target.Position;

		// chance to alert nearby zombies
		TryAlertNearby( Target, .1f, 800 ); // 800 good range??
	}

	public void Ignite()
	{
		if ( ZombieState == ZombieState.Burning ) return;
		ZombieState = ZombieState.Burning;
		Steer = new NavSteer();
		Steer.Target = Position + Rotation.Forward * Velocity.Length * 3f;
		//Speed = Speed * .25f;
		Speed = 0;
		TimeSinceBurnTicked = Rand.Float(.5f);

		Particles.Create( "particles/fire_zombie.vpcf", this );
	}

	public void StartWander()
	{
		ZombieState = ZombieState.Wander;
		Speed = WalkSpeed;

		var wander = new Nav.Wander();
		wander.MinRadius = 150;
		wander.MaxRadius = 300;
		Steer = wander;
	}

	public override void HitBreakableObject()
	{
		//base.HitBreakableObject();
		if ( TimeSinceAttacked > AttackSpeed )
		{
			TimeSinceAttacked = -3;
			TryMeleeAttack();
		}	
	}

	public void TryMeleeAttack()
	{
		if ( TimeSinceClimb < 1 ) return;
		if ( TimeUntilUnstunned > 0 ) return;
		PlaySoundOnClient( "zombie.attack" );
		SetAnimParameter( "b_attack", true );
		Velocity *= .1f;
	}

	public TimeSince TimeSinceLongIdle = -10;
	public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
	{
		base.OnAnimEventGeneric( name, intData, floatData, vectorData, stringData );
		if(name == "Attack" )
		{
			MeleeAttack();
		}
		else if(name == "IdleEnded" )
		{
			TimeSinceLongIdle = 0;
		}
	}

	public void MeleeAttack()
	{
		// initial delay too?
		//await Task.Delay( 100 );
		//if ( !IsValid ) return;
		//if ( TimeUntilUnstunned > 0 ) return;
		//PlaySoundOnClient( "zombie.attack" );
		//SetAnimParameter("b_attack", true);
		//Velocity = 0;

		// I don't like using Task.Delay, but it seems like the best option here?. I want the damage to come in slightly after the animation starts. This also gives the player a chance to block
		//await Task.Delay( 200 );
		Rand.SetSeed( Time.Tick );
		if ( !IsServer ) return;
		if ( !IsValid ) return;
		if ( TimeUntilUnstunned > 0 ) return;
		Velocity = 0;
		TimeSinceAttacked = 0 - Rand.Float( 1 );

		var forward = Rotation.Forward;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * 0.1f;
		forward = forward.Normal;

		foreach ( var tr in TraceMelee( EyePosition, EyePosition + forward * 70, 50 ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 320, AttackDamage )
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
				.Ignore( Owner )
				.WithoutTags("Zombie")
				.EntitiesOnly() // should we hit the world? probably not.
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

		if ( !Target.IsValid() )
		{
			Log.Warning( $"Couldn't find target for {this}!" );
		}
	}

	public override void TakeDamage( DamageInfo info )
	{
		TryAlert( info.Attacker, .5f );
		base.TakeDamage( info );
		DamagedEffects();
	}

	public override void OnKilled()
	{
		base.OnKilled();
		PlaySoundOnClient( "zombie.death" );
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
		if ( ZombieState == ZombieState.Lure || ZombieState == ZombieState.Burning ) return false;

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
	Lure,
	Burning
}
