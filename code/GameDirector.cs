namespace ZombieHorde;

public partial class GameDirector : Entity
{
	[ConVar.Replicated]
	public static bool zom_disabledirector { get; set; }

	[ConVar.Replicated]
	public static int zom_max_zombies { get; set; } = 15;

	public override void Spawn()
	{
		base.Spawn();

	}

	[Event.Tick.Server]
	public void Tick()
	{
		if ( zom_disabledirector ) return;

		PopulateZombies();
	}

	[ConCmd.Server( "zom_debug_spawnzombie" )] // can i just make this entire class static?
	public static void DebugSpawnZombie()
	{
		Entity.All.OfType<GameDirector>().FirstOrDefault().SpawnZombie();
	}


	private void PopulateZombies()
	{
		if ( Rand.Int( 100 ) == 1 )
		{
			var ZombieList = Entity.All
				.OfType<BaseZombie>()
				.ToList();

			if ( ZombieList.Count < zom_max_zombies )
				SpawnZombie();
		}
	}
	public BaseZombie SpawnZombie()
	{
		var SpawnPos = Position;
		var Tries = 0;
		var maxTries = 50;

		var ply = Entity.All.OfType<Player>().FirstOrDefault(); // just based on one player for now. todo: setup zombies to spawn out of los of ALL players.

		while ( Tries <= maxTries )
		{
			Tries += 1;
			var t = NavMesh.GetPointWithinRadius( ply.Position, 1000, 4000 );
			if ( t.HasValue )
			{
				SpawnPos = t.Value;
				var AddHeight = new Vector3( 0, 0, 70 );

				var PlayerPos = ply.EyePosition; 
				var tr = Trace.Ray( SpawnPos + AddHeight, PlayerPos )
							.UseHitboxes()
							.Run();

				if ( Vector3.DistanceBetween( tr.EndPosition, PlayerPos ) > 100 )
				{
					continue;
				}
			}
		}
		if ( Tries <= maxTries )
		{
			Log.Warning( "Can't Find Valid Zombie Spawn" );
			return null;
		}

		var npc = new CommonZombie
		{
			Position = SpawnPos,
			//Rotation = Rotation.Random // LOL this looks so stupid! The zombie usually spawns rotated underground and "rises from the grave" - note: probably only use this if spawning zombies in player los
			//Rotation = Rotation.LookAt( Owner.EyeRotation.Backward.WithZ( 0 ) )
		};
		Log.Info( "Spawned Zombie. Population: " + Entity.All.OfType<BaseZombie>().ToList().Count() );
		return npc;
	}
}
