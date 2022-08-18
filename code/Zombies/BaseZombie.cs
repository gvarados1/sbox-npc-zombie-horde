namespace ZombieHorde;

public partial class BaseZombie : BaseNpc
{
	[ConVar.Replicated]
	public static bool nav_drawpath { get; set; }

	[ConCmd.Server( "npc_clear" )]
	public static void NpcClear()
	{
		foreach ( var npc in Entity.All.OfType<BaseZombie>().ToArray() )
			npc.Delete();
	}

	public float Speed { get; set; }
	public Entity Target;

	NavPath Path = new NavPath();
	public NavSteer Steer;

	public ZombieState ZombieState = ZombieState.Wander;
	public float WalkSpeed = Rand.Float( 40, 50 );
	//public float RunSpeed = Rand.Float( 260, 280 );
	public float RunSpeed = Rand.Float( 130, 150 ); // player speed = 240
	public TimeSince TimeSinceAttacked = 0;
	public float AttackSpeed = 1.0f;
	public TimeUntil TimeUntilUnstunned = 0;
	public TimeSince TimeSinceBurnTicked = 0;
	public float AttackDamage = 6;

	public override void Spawn()
	{
		base.Spawn();

		//SetModel( "models/zombie/citizen_zombie.vmdl" );
		//SetModel( "models/zombie/citizen_zombie_test.vmdl" );
		SetModel( "models/zombie/citizen_zombie_mixamo.vmdl" );
		EyePosition = Position + Vector3.Up * 64;
		SetupPhysicsFromCapsule( PhysicsMotionType.Keyframed, Capsule.FromHeightAndRadius( 72, 8 ) );

		EnableHitboxes = true;

		Speed = Rand.Float( 270, 320 );
		WalkSpeed = Rand.Float( 40, 50 );
		Health = 50;

		// add "Zombie" tag for collisions
		Tags.Add( "Zombie" );
	}

	public Sandbox.Debug.Draw Draw => Sandbox.Debug.Draw.Once;

	Vector3 InputVelocity;

	Vector3 LookDir;


	//bool AltTick = false;
	//public static bool UsingAltTick = false;
	[Event.Tick.Server]
	public virtual void Tick()
	{
		// only update zombie every other tick
		//AltTick = !AltTick;
		//if ( UsingAltTick && !AltTick ) return;

		//SetAnimParameter( "b_grounded", true ); ;
		InputVelocity = 0;

		if ( Steer != null )
		{
			Steer.Tick( Position, Velocity );

			if ( !Steer.Output.Finished )
			{
				InputVelocity = Steer.Output.Direction.Normal;
				//if( UsingAltTick )
				//	Velocity = Velocity.AddClamped( 2 * InputVelocity * Time.Delta * 200, Speed ); //500
				//else
					Velocity = Velocity.AddClamped( InputVelocity * Time.Delta * 200, Speed ); //500
			}

			if ( nav_drawpath )
			{
				DebugOverlay.Text( ((int)Velocity.Length).ToString(), EyePosition + Vector3.Up * 16 );
				Steer.DebugDrawPath();
			}
		}

		// multiply delta * 2 because every other frame
		//if( UsingAltTick )
		//	Move( Time.Delta * 2 );
		//else
			Move( Time.Delta);

		var walkVelocity = Velocity.WithZ( 0 );
		if ( walkVelocity.Length > 1f )
		{
			var turnSpeed = walkVelocity.Length.LerpInverse( 0, 250, true ); // 100
			var targetRotation = Rotation.LookAt( walkVelocity.Normal, Vector3.Up );
			Rotation = Rotation.Lerp( Rotation, targetRotation, turnSpeed * Time.Delta * 20.0f );
		}

		var animHelper = new CitizenAnimationHelper( this );

		LookDir = Vector3.Lerp( LookDir, InputVelocity.WithZ( 0 ) * 1000, Time.Delta * 100.0f );
		animHelper.WithLookAt( EyePosition + LookDir );
		animHelper.WithVelocity( Velocity );
		animHelper.WithWishVelocity( InputVelocity );
	}

	protected virtual void Move( float timeDelta )
	{
		var bbox = BBox.FromHeightAndRadius( 64, 4 );
		//DebugOverlay.Box( Position, bbox.Mins, bbox.Maxs, Color.Green );

		ZomMoveHelper move = new( Position, Velocity );
		move.MaxStandableAngle = 50;
		move.Trace = move.Trace.Ignore( this ).Size( bbox );

		if ( !Velocity.IsNearlyZero( 0.001f ) )
		{
			//	Sandbox.Debug.Draw.Once
			//						.WithColor( Color.Red )
			//						.IgnoreDepth()
			//						.Arrow( Position, Position + Velocity * 2, Vector3.Up, 2.0f );
			move.TryUnstuck();

			if(GroundEntity != null )
			{
				move.TryMoveWithStep( timeDelta, 20 );
			}
			else
			{
				move.TryMove( timeDelta );
			}
		}

		//using ( Sandbox.Debug.Profile.Scope( "Ground Checks" ) )
		{

			var tr = move.TraceDirection( Vector3.Down * 10.0f );

			if (Velocity.z < 5 && move.IsFloor( tr ) )
			{
				SetAnimParameter( "b_grounded", true );
				GroundEntity = tr.Entity;

				if ( !tr.StartedSolid )
				{
					move.Position = tr.EndPosition;
				}

				if ( InputVelocity.Length > 0 )
				{
					var movement = move.Velocity.Dot( InputVelocity.Normal );
					move.Velocity = move.Velocity - movement * InputVelocity.Normal;
					move.ApplyFriction( tr.Surface.Friction * 10.0f, timeDelta );
					move.Velocity += movement * InputVelocity.Normal;

				}
				else
				{
					move.ApplyFriction( tr.Surface.Friction * 10.0f, timeDelta );
				}
			}
			else
			{
				GroundEntity = null;
				move.Velocity += Vector3.Down * 900 * timeDelta;
				SetAnimParameter( "b_grounded", false );

				if ( nav_drawpath )
				{
					Sandbox.Debug.Draw.Once.WithColor( Color.Red ).Circle( Position, Vector3.Up, 10.0f );
				}
			}

			// hit a wall or prop/glass. need to jump over or break it
			if (GroundEntity != null && move.HitWall )
			{
				// trace at jump height
				var jumpTrace = Trace.Ray( Position + Vector3.Up * 100, EyePosition + Vector3.Up * 40 + Rotation.Forward * 60 )
				.UseHitboxes()
				.WithoutTags( "Zombie" )
				.EntitiesOnly()
				.Ignore( this )
				.Size( 10 )
				//.WorldOnly()
				.Run();

				if ( jumpTrace.Hit )
				{
					HitBreakableObject();
				}
				else
				{
					//basic jump
					GroundEntity = null;

					move.Velocity = new Vector3( 0, 0, 330f );
					move.Position += new Vector3( 0, 0, 4f );

					SetAnimParameter( "b_jump", true );
				}
			}
		}

		Position = move.Position;
		Velocity = move.Velocity;
	}

	TimeSince TimeSinceSeenTarget;

	public virtual void TryPathOffNav()
	{
		if ( nav_drawpath )
			DebugOverlay.Sphere( EyePosition, 10, Color.Yellow );
		// let's only deal with players for now
		if ( Target is HumanPlayer )
		{
			var tr = Trace.Ray( EyePosition, Target.EyePosition )
				.WorldOnly()
				.WithAnyTags( "player", "solid" )
				.UseHitboxes()
				.Run();

			//if ( nav_drawpath )
			//	DebugOverlay.TraceResult( tr, .1f );

			// I should probably use a LastScenePosition, but it doesn't really matter.
			InputVelocity = (Target.Position - Position).WithZ( 0 ).Normal;

			if (tr.Entity == Target )
			{
				TimeSinceSeenTarget = 0;
			}

			// try getting back on the navmesh if we lost the player
			if(TimeSinceSeenTarget > 5 )
			{
				var pos = NavMesh.GetClosestPoint( Position );
				if( pos!= null )
					InputVelocity = ((Vector3)pos - Position).WithZ( 0 ).Normal;
			}

			// zombies gain a ton of speed while in the air. not sure what's going on there.
			Velocity = Velocity.AddClamped( InputVelocity * Time.Delta * 2000, Speed ); //500
		}
	}

	public virtual void HitBreakableObject()
	{
		// override me!
	}

	public override void TakeDamage( DamageInfo info )
	{
		base.TakeDamage( info );

		if ( info.Attacker is HumanPlayer attacker )
		{
			attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, Health.LerpInverse( 100, 0 ) );

			if ( Health <= 0 )
			{
				info.Attacker.Client.AddInt( "kills" );
				BaseGamemode.Current.ZombiesRemaining--;
			}
		}
	}
}
