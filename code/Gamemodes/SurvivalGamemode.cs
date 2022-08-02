namespace ZombieHorde;

public partial class SurvivalGamemode : BaseGamemode
{
	[ConCmd.Admin]
	public static void zom_skipround()
	{
		Log.Info( "Skipping round!" );

		var gamemode = BaseGamemode.Current as SurvivalGamemode;

		gamemode.ZombiesRemaining = 0;
		gamemode.TimeUntilNextState = 0;
	}

	[Net]
	public TimeUntil TimeUntilNextState { get; set; }
	public int WaveNumber { get; set; } = 0;

	public RoundState RoundState { get; set; }
	public override void Spawn()
	{
		Log.Info( "Survival gamemode active!" );
		Log.Info( Host.Name );
		RoundState = RoundState.PreGame;
		TimeUntilNextState = 60;
		HumanMaxRevives = 1;

		base.Spawn();
	}

	public override void Tick()
	{
		base.Tick();

		// surely there's an easier way to do this
		var roundAmount = TimeUntilNextState > 10 ? 0 : 1;
		var roundedAmount = Math.Round( TimeUntilNextState, roundAmount );
		var suffix = roundedAmount < 10 && roundedAmount % 1 == 0 ? ".0s" : "s";
		RoundInfo = roundedAmount.ToString() + suffix;

		if ( RoundState == RoundState.PreGame )
		{
			RoundName = "Get Ready to Survive the Horde!";
			RoundInfo = "Wave coming in " + RoundInfo;

			if( TimeUntilNextState <= 0 ) StartWave();
		}
		else if(RoundState == RoundState.WaveActive )
		{
			RoundName = "Wave " + WaveNumber;
			RoundInfo = ZombiesRemaining.ToString() + " Remain";

			if ( ZombiesRemaining <= 0 ) StartIntermission();
			if ( GetLivePlayerCount() <= 0 ) StartPostGame();
		}
		else if ( RoundState == RoundState.Intermission )
		{
			RoundName = "Intermission";
			RoundInfo = $"Wave {WaveNumber + 1} coming in " + RoundInfo;

			if ( TimeUntilNextState <= 0 ) StartWave();
		}
		else if ( RoundState == RoundState.PostGame )
		{
			RoundName = "Game over! Waves Survived: " + (WaveNumber-1);

			if ( TimeUntilNextState <= 0 ) RestartGame();
		}
	}
	public void StartWave()
	{
		if(Host.IsServer) PlaySound( "wave.start" );
		WaveNumber++;

		var playerCount = Entity.All.OfType<HumanPlayer>().Count();
		var difficultyMultiplier = .5f + playerCount * .5f;

		ZombiesRemaining += 10 + (int)(3 * (WaveNumber - 1) * difficultyMultiplier);
		if ( ZombiesRemaining < 5 ) ZombiesRemaining = 5;
		RoundState = RoundState.WaveActive;

		// anger all zombies!
		foreach ( var npc in Entity.All.OfType<CommonZombie>().ToArray() )
		{
			npc.StartChase();
		}
	}
	public void StartIntermission()
	{
		if ( Host.IsServer ) PlaySound( "wave.end" );
		TimeUntilNextState = 40;
		RoundState = RoundState.Intermission;

		UpdateZombieStats();

		if ( Host.IsClient ) 
		{
			//HealthBar.RefreshAvatar();
			return; 
		}

		// revive all incapacitated players!
		foreach ( var ply in Entity.All.OfType<HumanPlayer>() )
		{
			ply.RevivesRemaining = HumanMaxRevives;
			if(ply.LifeState == LifeState.Dying )
			{
				ply.Revive();
			}
		}

		// kill angry zombies!
		foreach ( var zom in Entity.All.OfType<CommonZombie>().ToList() )
		{
			//if(zom.ZombieState == ZombieState.Chase )
			{
				Velocity = 0;
				Sound.FromWorld( "rust_pumpshotgun.shootdouble", zom.Position );
				var damageInfo = DamageInfo.Explosion( zom.Position, Vector3.Zero,10000 );
				zom.TakeDamage( damageInfo );
			}
		}

		ZombiesRemaining = 0;

		foreach ( var ply in Entity.All.OfType<Player>().ToList())
		{
			var t = NavMesh.GetPointWithinRadius( ply.Position, 1000, 4000 );
			if ( t.HasValue )
			{
				var box = new LootBox();
				box.Position = t.Value;
			}
		}
	}

	public void StartPostGame()
	{
		TimeUntilNextState = 20;
		RoundState = RoundState.PostGame;

		if ( Host.IsServer )
		{
			foreach ( var ply in Entity.All.OfType<HumanPlayer>().ToList() )
			{
				var damageInfo = DamageInfo.Generic( 10000 );
				ply.TakeDamage( damageInfo );
				//ply.OnKilled();
			}
		}
	}

	public void RestartGame()
	{
		if ( Host.IsServer )
		{
			PlaySound( "bell" );
			HealthBar.RefreshAvatar(To.Everyone);

			// surely there's a better way of doing this
			foreach ( var ply in Entity.All.OfType<HumanPlayer>().ToList() )
				ply.TakeDamage( DamageInfo.Generic( 10000 ) );

			foreach ( var npc in Entity.All.OfType<BaseZombie>().ToArray() )
				npc.Delete();

			foreach ( var item in Entity.All.OfType<BaseZomWeapon>().ToArray() )
				item.Delete();

			foreach ( var item in Entity.All.OfType<LootBox>().ToArray() )
				item.Delete();

			foreach ( var item in Entity.All.OfType<AmmoPile>().ToArray() )
				item.Delete();

			foreach ( var item in Entity.All.OfType<HealthKit>().ToArray() )
				item.Delete();
		}
		WaveNumber = 0;
		ZombiesRemaining = 0;
		TimeUntilNextState = 60;
		RoundState = RoundState.PreGame;

		UpdateZombieStats();
	}
	public override bool EnableRespawning()
	{
		return RoundState == RoundState.PreGame || RoundState == RoundState.Intermission;
	}

	public override bool PopulateZombiesAngry()
	{
		return RoundState == RoundState.WaveActive;
	}

	public void UpdateZombieStats()
	{
		// would be nice to have a graph view of this
		switch ( WaveNumber +1)
		{
			case 0:
			case 1:
				ZomHealthMultiplier = 1;
				ZomSpeedMultiplier = 1;
				ZomSpawnRate = 1;
				ZomMaxZombies = 5;
				break;
			case 2:
				ZomSpeedMultiplier = 1.5f;
				ZomMaxZombies = 7;
				break;
			case 3:
				ZomSpeedMultiplier = 1.75f;
				ZomSpawnRate = 1.5f;
				ZomMaxZombies = 6;
				break;
			case 4:
				ZomSpeedMultiplier = 2f;
				ZomMaxZombies = 7;
				break;
			case 5:
				break;
			case 6:
				ZomSpawnRate = 1.8f;
				ZomMaxZombies = 10;
				break;
			case 10:
				ZomMaxZombies = 12;
				break;
			case 13:
				ZomSpawnRate = 2f;
				break;
			case 14:
				ZomSpawnRate = 3f;
				break;
			case 15:
				ZomSpawnRate = 1000f;
				break;
			case 16:
				ZomMaxZombies = 13;
				break;
			default:
				ZomSpeedMultiplier += .005f;
				ZomHealthMultiplier += .02f;
				break;
		}
		ZomMaxZombies.Clamp( 5, 20 );
	}

}

public enum RoundState
{
	PreGame, // waiting for players
	WaveActive, // round is active, spawning zombies
	Intermission, // in-between rounds. maybe spawn a couple wandering zombies?
	PostGame // everyone died and the game ended
}
