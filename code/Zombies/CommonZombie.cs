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

	public override void Spawn()
	{
		base.Spawn();

		SetMaterialGroup( 4 );
		RenderColor = new Color32( (byte)(105 + Rand.Int( 20 )), (byte)(174 + Rand.Int( 20 )), (byte)(59 + Rand.Int( 20 )), 255 ).ToColor();

		UpdateClothes();
		Clothing.DressEntity( this );

		SetBodyGroup( 1, 0 );

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
			if ( Rand.Int( 10 ) == 1 )
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
}

public enum ZombieState
{
	Wander,
	Chase,
	Lure
}
