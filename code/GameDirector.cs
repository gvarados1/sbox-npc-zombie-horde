using System.Linq.Expressions;

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
		var difficultyMultiplier = .5f + playerCount * .5f;
		var zombieMultiplier = .75f + playerCount * .25f;
		var zombieCount = Entity.All.OfType<BaseZombie>().ToList().Count;
		var currentWave = (BaseGamemode.Current as SurvivalGamemode).WaveNumber + 1;
		var maxZombies = BaseGamemode.Current.ZomMaxZombies;
		if ( (BaseGamemode.Current as SurvivalGamemode).RoundState != RoundState.WaveActive )
			maxZombies *= .5f;

		var spawnRate = 1 / BaseGamemode.Current.ZomSpawnRate / difficultyMultiplier;
		if(zombieCount > 3*difficultyMultiplier)
			spawnRate *= 2;
		if ( TimeSinceSpawnedZombie > spawnRate )
		{
			// hard-limit 20 zombies
			if ( zombieCount < maxZombies.Clamp(maxZombies * zombieMultiplier, 20) )
			{
				SpawnZombie();
				TimeSinceSpawnedZombie = 0 - Rand.Float(1f);

				// less random time if above wave 15
				if ( currentWave > 15 )
					TimeSinceSpawnedZombie = 0 - Rand.Float( .5f );

				// spawn instantly above wave 18 lol
				if ( currentWave > 18 )
					TimeSinceSpawnedZombie = 0;
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
					var target = Entity.All.OfType<HumanPlayer>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();
					SpawnZombie( target );
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

		//if ( zombieCount > 10 )
		//	CommonZombie.UsingAltTick = true;
		//else
		//	CommonZombie.UsingAltTick = false;
	}

	private int ZombieSpawnFails = 0;
	public BaseZombie SpawnZombie( Entity ply = null)
	{
		Rand.SetSeed( Time.Tick );
		var spawnPos = Position;
		var tries = 0;
		var maxTries = 30;
		var maxRange = 3000;

		if( ply == null )
		{
			ply = Entity.All.OfType<HumanPlayer>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault(); // random player. todo: smarter spawns, target the player with the least zombies chasing them
			if ( ply == null ) return null;
			if ( ply.LifeState != LifeState.Alive )
				ply = Entity.All.OfType<HumanPlayer>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault(); // reroll once if we don't get an alive player
		}
		if ( ply == null ) return null;

		// might want to twitch these to a static method using enums instead of strings some time. The strings work but can be hard to remember.
		var minRadius = 1000;
		if ( Trace.TestPoint(ply.Position, "AllowCommonZombieSpawn", 500 ))
			minRadius = 500;

		while ( tries <= maxTries )
		{
			tries += 1;
			var t = NavMesh.GetPointWithinRadius( ply.Position, minRadius, maxRange );
			if ( t.HasValue )
			{
				spawnPos = t.Value;
				if ( spawnPos.Length > 30000 ) return null; // Sometimes GetPointWithinRadius returns a wacky value? check for that here.

				if ( Trace.TestPoint( t.Value, "BlockCommonZombieSpawn", 20 ) )
					continue;
				if ( Trace.TestPoint( t.Value, "AllowCommonZombieSpawn", 20 ) )
					break; // skip LOS trace

				if ( Trace.TestPoint( t.Value, "player", 500 ) )
					continue; // don't spawn on top of a player lol

				var addHeight = new Vector3( 0, 0, 70 );

				foreach(var ply1 in Entity.All.OfType<HumanPlayer>().ToList() )
				{
					var playerPos = ply1.EyePosition;
					var tr1 = Trace.Ray( spawnPos + addHeight, playerPos )
								//.UseHitboxes()
								.WithoutTags("trigger")
								.Run();

					if ( Vector3.DistanceBetween( tr1.EndPosition, playerPos ) < 20 )
					{
						continue;
					}
				}

				break;
			}
		}
		if ( tries >= maxTries )
		{
			var foundSpawn = false;
			// couldn't find a valid spawn. try to spawn in the center of an appropriate spawn blocker...
			foreach ( var blocker in Entity.All.OfType<HammerSpawnBlocker>().ToList() )
			{
				if ( blocker.Tags.Has( "AllowCommonZombieSpawn" ) )
				{
					spawnPos = blocker.Position;
					foundSpawn = true;
					break;
				}
			}
			if ( !foundSpawn )
			{
				// should we spawn on the player if we can't find a position?
				Log.Warning( "Can't Find Valid Zombie Spawn" );
				ZombieSpawnFails += 1;

				if ( ZombieSpawnFails > 10 ) Log.Error( "Can't spawn zombies! Map doesn't have a navmesh or is too small." ); // do I really need to do this?
				return null;
			}
		}

		CommonZombie npc = null;

		var currentWave = (BaseGamemode.Current as SurvivalGamemode).WaveNumber + 1;
		// change to spawn uncommon zombies! (armored)
		if ( currentWave > 4)
		{
			if(Rand.Int(5 + (30/currentWave)) == 0 )
			{
				npc = new UncommonZombie();
				Log.Info( "spawning uncommon zombie!" );
			}
		}

		// default to regular common zombie
		if(npc == null)
			npc = new CommonZombie();

		npc.Position = spawnPos;
		if ( BaseGamemode.Current.PopulateZombiesAngry() )
		{
			npc.StartChase( ply );
		}

		Log.Info( "Spawned Zombie. Population: " + Entity.All.OfType<BaseZombie>().ToList().Count() );

		ZombieSpawnFails = 0;
		return npc;
	}
}
