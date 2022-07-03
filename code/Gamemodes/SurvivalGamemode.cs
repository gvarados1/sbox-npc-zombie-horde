namespace ZombieHorde;

public partial class SurvivalGamemode : BaseGamemode
{
	[ConCmd.Admin]
	public static void zom_skipround()
	{
		Log.Info( "Skipping round!" );

		var gamemode = BaseGamemode.Ent as SurvivalGamemode;

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
			RoundName = "Pre-Game";

			if( TimeUntilNextState <= 0 ) StartWave();
		}
		else if(RoundState == RoundState.WaveActive )
		{
			RoundName = "Wave " + WaveNumber;
			RoundInfo = ZombiesRemaining.ToString() + " remain";

			if ( ZombiesRemaining <= 0 ) StartIntermission();
			if ( GetLivePlayerCount() <= 0 ) StartPostGame();
		}
		else if ( RoundState == RoundState.Intermission )
		{
			RoundName = "Intermission";

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

		ZombiesRemaining += 10 + 4 * (WaveNumber - 1);
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

		if ( Host.IsClient ) return;

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
			if(zom.ZombieState == ZombieState.Chase )
			{
				Velocity = 0;
				Sound.FromWorld( "rust_pumpshotgun.shootdouble", zom.Position );
				var damageInfo = DamageInfo.Explosion( zom.Position, Vector3.Zero,50 );
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
				ply.OnKilled();
			}
		}
	}

	public void RestartGame()
	{
		if ( Host.IsServer )
		{
			PlaySound( "bell" );


			// surely there's a better way of doing this
			foreach ( var ply in Entity.All.OfType<HumanPlayer>().ToList() )
				ply.OnKilled();

			foreach ( var npc in Entity.All.OfType<BaseZombie>().ToArray() )
				npc.Delete();

			foreach ( var item in Entity.All.OfType<DeathmatchWeapon>().ToArray() )
				item.Delete();

			foreach ( var item in Entity.All.OfType<LootBox>().ToArray() )
				item.Delete();

			foreach ( var item in Entity.All.OfType<Coffin>().ToArray() )
				item.Delete();

			foreach ( var item in Entity.All.OfType<BaseAmmo>().ToArray() )
				item.Delete();

			foreach ( var item in Entity.All.OfType<HealthKit>().ToArray() )
				item.Delete();
		}
		WaveNumber = 0;
		ZombiesRemaining = 0;
		TimeUntilNextState = 60;
		RoundState = RoundState.PreGame;
	}
	public override bool EnableRespawning()
	{
		return RoundState == RoundState.PreGame || RoundState == RoundState.Intermission;
	}

	public override bool PopulateZombiesAngry()
	{
		return RoundState == RoundState.WaveActive;
	}

}

public enum RoundState
{
	PreGame, // waiting for players
	WaveActive, // round is active, spawning zombies
	Intermission, // in-between rounds. maybe spawn a couple wandering zombies?
	PostGame // everyone died and the game ended
}
