namespace ZombieHorde;

public partial class GameDirector : Entity
{
	[ConVar.Replicated]
	public static bool zom_disabledirector { get; set; }
	public TimeSince TimeSinceSpawnedZombie { get; set; }

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
		var playerCount = Entity.All.OfType<HumanPlayer>().Count();
		var difficultyMultiplier = .75f + playerCount * .25f;
		var zombieCount = Entity.All.OfType<BaseZombie>().ToList().Count;
		var currentWave = (BaseGamemode.Current as SurvivalGamemode).WaveNumber + 1;
		var maxZombies = BaseGamemode.Current.ZomMaxZombies;
		if ( (BaseGamemode.Current as SurvivalGamemode).RoundState != RoundState.WaveActive )
			maxZombies *= .5f;

		var spawnRate = 1 / BaseGamemode.Current.ZomSpawnRate * difficultyMultiplier;
		if(zombieCount > 3*difficultyMultiplier)
			spawnRate *= 2;
		if ( TimeSinceSpawnedZombie > spawnRate )
		{
			if ( zombieCount < maxZombies * difficultyMultiplier )
			{
				SpawnZombie();
				TimeSinceSpawnedZombie = 0 - Rand.Float(1f);
			}
		}

		// chance to spawn a ton of zombies if there aren't many
		if( zombieCount < 3 && currentWave >= 3 )
		{
			if ( Rand.Int( 300 ) == 1 )
			{
				var i = 0;
				for( i = 0; i < 2+playerCount; i++ )
				{
					SpawnZombie();
				}
				Log.Info( "Spawned Group of " + i );
				TimeSinceSpawnedZombie = 0;
			}
		}
		/*
		if ( Rand.Int( 100 ) == 1 )
		{
			var ZombieList = Entity.All
				.OfType<BaseZombie>()
				.ToList();

			if ( ZombieList.Count < BaseGamemode.Current.ZomMaxZombies )
				SpawnZombie();
		}
		*/
	}

	private int ZombieSpawnFails = 0;
	public BaseZombie SpawnZombie()
	{
		var spawnPos = Position;
		var tries = 0;
		var maxTries = 50;

		var ply = Entity.All.OfType<Player>().FirstOrDefault(); // just based on one player for now. todo: setup zombies to spawn out of los of ALL players.

		while ( tries <= maxTries )
		{
			tries += 1;
			var t = NavMesh.GetPointWithinRadius( ply.Position, 1000, 4000 );
			if ( t.HasValue )
			{
				spawnPos = t.Value;
				if ( spawnPos.Length > 30000 ) return null; // Sometimes GetPointWithinRadius returns a wacky value? check for that here.
				var addHeight = new Vector3( 0, 0, 70 );

				var playerPos = ply.EyePosition; 
				var tr = Trace.Ray( spawnPos + addHeight, playerPos )
							.UseHitboxes()
							.Run();

				if ( Vector3.DistanceBetween( tr.EndPosition, playerPos ) > 100 )
				{
					break;
				}
			}
		}
		if ( tries >= maxTries )
		{
			Log.Warning( "Can't Find Valid Zombie Spawn" );
			ZombieSpawnFails += 1;

			if ( ZombieSpawnFails > 10 ) Log.Error( "Map doesn't have a navmesh. Can't spawn zombies!" ); // do I really need to do this?
			return null;
		}

		var npc = new CommonZombie
		{
			Position = spawnPos,
			//Rotation = Rotation.Random // LOL this looks so stupid! The zombie usually spawns rotated underground and "rises from the grave" - note: probably only use this if spawning zombies in player los
			//Rotation = Rotation.LookAt( Owner.EyeRotation.Backward.WithZ( 0 ) )
		};
		if ( BaseGamemode.Current.PopulateZombiesAngry() )
		{
			npc.StartChase();
		}

		Log.Info( "Spawned Zombie. Population: " + Entity.All.OfType<BaseZombie>().ToList().Count() );

		ZombieSpawnFails = 0;
		return npc;
	}
}
