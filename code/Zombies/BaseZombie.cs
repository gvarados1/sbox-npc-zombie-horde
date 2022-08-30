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
	public static float StepSize = 20;

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

		SetAnimParameter( "b_climbing", false );
		if ( TimeSinceClimb < .5f )
		{
			if( TimeUntilUnstunned < 0 )
			{
				ClimbMove();
				if( ClimbDistanceRemaining > 50 )
				{
					SetAnimParameter( "b_climbing", true );
				}
				SetAnimParameter( "b_grounded", true );
			}
		}
		else
		{
			Move( Time.Delta );
		}

		var walkVelocity = Velocity.WithZ( 0 );
		if ( walkVelocity.Length > 1f )
		{
			var turnSpeed = walkVelocity.Length.LerpInverse( 0, 250, true ); // 100
			var targetRotation = Rotation.LookAt( walkVelocity.Normal, Vector3.Up );
			if( TimeSinceClimb < .5f )
			{
				targetRotation = Rotation.LookAt( ClimbForward, Vector3.Up );
				Rotation = targetRotation;
			}
			else if ( TimeUntilUnstunned > 0 )
			{
				if ( Rotation.Distance( targetRotation ) > 160 )
				{
					targetRotation = Rotation.LookAt( -walkVelocity.Normal, Vector3.Up );
				}
				Rotation = Rotation.Lerp( Rotation, targetRotation, turnSpeed * Time.Delta * 5.0f );
				
			}
			// do I really need to do this????
			//else if (Target != null && Position.Distance(Target.Position) < 60 )
			//else if ( Steer != null && Position.Distance(Steer.Target) < 60 )
			//{
			//	//targetRotation = Rotation.LookAt( (Target.Position - Position).Normal, Vector3.Up );
			//	targetRotation = Rotation.LookAt( (Steer.Target - Position).Normal.WithZ(0), Vector3.Up );
			//	Rotation = Rotation.Lerp( Rotation, targetRotation, turnSpeed * Time.Delta * 20.0f );
			//}
			else
			{
				Rotation = Rotation.Lerp( Rotation, targetRotation, turnSpeed * Time.Delta * 20.0f );
			}
		}

		var animHelper = new CitizenAnimationHelper( this );

		LookDir = Vector3.Lerp( LookDir, InputVelocity.WithZ( 0 ) * 1000, Time.Delta * 100.0f );
		animHelper.WithLookAt( EyePosition + LookDir );
		animHelper.WithVelocity( Velocity );
		animHelper.WithWishVelocity( InputVelocity );
	}

	protected virtual void Move( float timeDelta )
	{
		var bbox = BBox.FromHeightAndRadius( 60, 4 );
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
				move.TryMoveWithStep( timeDelta, StepSize );
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

			if(TimeUntilUnstunned < 0 && TimeSinceClimb > .5f )
			{
				// hit a wall or prop/glass. need to jump over or break it
				if ( GroundEntity != null && move.HitWall )
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
					else if( !TestClimb() )
					{
						//basic jump
						GroundEntity = null;

						move.Velocity = new Vector3( 0, 0, 330f );
						move.Position += new Vector3( 0, 0, 4f );

						SetAnimParameter( "b_jump", true );
					}
				}
			}
		}

		Position = move.Position;
		Velocity = move.Velocity;
	}

	public virtual void TryJump()
	{

	}

	TimeSince TimeSinceSeenTarget;

	public virtual void TryPathOffNav()
	{
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

			if ( nav_drawpath )
			{
				DebugOverlay.Sphere( EyePosition, 10, Color.Yellow );
				DebugOverlay.Text( $"{(int)TimeSinceSeenTarget}", EyePosition + 10 );
			}
		}
	}

	public Vector3 ClimbForward;
	public Vector3 ClimbTargetPos;
	public TimeSince TimeSinceClimb = 10;

	public virtual bool TestClimb()
	{
		// check if we're trying to go forward while in the air
		if ( TimeSinceClimb < 0 ) return false;

		// simple bbox trace
		var tr = TraceBBoxIgnoreZom( Position + Vector3.Up * StepSize, Position + Vector3.Up * StepSize + Rotation.Forward.WithZ( 0 ).Normal * 10, 2 );
		if ( tr.Hit )
		{

			ClimbForward = -tr.Normal.WithZ(0);
			//ClimbForward = Rotation.Forward.WithZ( 0 ).Normal;

			// another trace up to see how far we should go
			var maxHeight = 500; //110;
			var minHeight = StepSize;
			var adjustedMaxHeight = maxHeight + 72;
			var trCheck = TraceBBoxIgnoreZom( Position, Position + Vector3.Up * adjustedMaxHeight );

			if ( trCheck.Distance < minHeight ) return false;

			// note: we should probably do incremental traces at different heights to check for windows that we can climb through.
			// this shouldn't really matter with such a low climb height though?

			// trace forward from hit level
			var trCheck2 = TraceBBoxIgnoreZom( trCheck.EndPosition, trCheck.EndPosition + ClimbForward * 10 );
			// if we hit less than 10 units forwards that means it's a dumb ledge that we shouldn't climb to
			if ( trCheck2.Hit ) return false;

			// final check, trace down to find height
			var trCheck3 = TraceBBoxIgnoreZom( trCheck2.EndPosition, trCheck2.EndPosition + Vector3.Down * adjustedMaxHeight );
			var ClimbHeight = trCheck3.EndPosition.z - Position.z;
			// make sure we're not climbing down somehow lol
			if ( trCheck3.EndPosition.z - Position.z < 0 ) return false;
			if ( ClimbHeight > maxHeight ) return false;

			ClimbTargetPos = trCheck3.HitPosition;
			TimeSinceClimb = 0;
			Sound.FromWorld( "player.crouch", Position );
			//ClimbMove();
			return true;
		}
		return false;
	}

	public float ClimbDistanceRemaining = 0;
	public void ClimbMove()
	{
		// move forward once we reached the top
		if ( TimeSinceClimb > Time.Delta * 2 )
		{
			Velocity = ClimbForward * 60;
			if ( TimeSinceClimb > Time.Delta * 4 )
				Velocity += Vector3.Down * 60;
			Move( Time.Delta );
			return;
		}

		var climbVelocity = 50; // 200
		Velocity = Vector3.Up * climbVelocity;
		Velocity += ClimbForward * 60;

		Move( Time.Delta );

		// something went wrong if we're above our target position...
		//if ( ClimbTargetPos.z + 8 < Position.z )
		if( ClimbDistanceRemaining < -8)
			return;

		ClimbDistanceRemaining = ClimbTargetPos.z - Position.z;
		if ( ClimbDistanceRemaining < 79 && ClimbDistanceRemaining > 40 ) //85
		{
			SetAnimParameter( "b_climbing_end_top", true );
		}


		// constantly check if we should still be climbing. Will prevent getting stuck floating forever lol
		var trCheck = TraceBBoxIgnoreZom( Position + Vector3.Down * 4, Position + Vector3.Down * 4 + ClimbForward * 10, 2 );
		if ( trCheck.Hit )
		{
			var trCheck2 = TraceBBoxIgnoreZom( Position, Position + Vector3.Up * StepSize, 2 );
			if ( !trCheck2.Hit )
				TimeSinceClimb = 0;
		}
	}

	public virtual TraceResult TraceBBoxIgnoreZom( Vector3 start, Vector3 end, float liftFeet = 0.0f )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			//maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		var bbox = BBox.FromHeightAndRadius( 60, 4 );
		var tr = Trace.Ray( start, end )
					.Size( bbox )
					.WithAnyTags( "solid", "playerclip", "passbullets" )
					.WithoutTags( "gib" )
					.Ignore( this )
					.Run();
		return tr;
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
