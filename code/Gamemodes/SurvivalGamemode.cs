namespace ZombieHorde;

public partial class SurvivalGamemode : BaseGamemode
{
	[Net]
	public TimeUntil TimeUntilNextState { get; set; }
	public int WaveNumber { get; set; } = 0;

	public RoundState RoundState { get; set; }
	public override void Spawn()
	{
		Log.Info( "Survival gamemode active!" );
		RoundState = RoundState.PreGame;
		TimeUntilNextState = 60;

		base.Spawn();
	}

	public override void Tick()
	{
		base.Tick();

		var roundName = "Unknown Game State";
		var roundTimeUntil = Math.Round(TimeUntilNextState, 2).ToString();

		if ( RoundState == RoundState.PreGame )
		{
			roundName = "Pre-Game";

			if( TimeUntilNextState <= 0 ) StartWave();
		}
		else if(RoundState == RoundState.WaveActive )
		{
			roundName = "Wave " + WaveNumber + ": Zombies Remaining: ";
			roundTimeUntil = ZombiesRemaining.ToString();

			if ( ZombiesRemaining <= 0 ) StartIntermission();
			if ( GetLivePlayerCount() <= 0 ) RestartGame();
		}
		else if ( RoundState == RoundState.Intermission )
		{
			roundName = "Intermission";

			if ( TimeUntilNextState <= 0 ) StartWave();
		}

		DebugOverlay.ScreenText( roundName + ": " + roundTimeUntil );
	}
	public void StartWave()
	{
		PlaySound( "wave.start" );
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
		PlaySound( "wave.end" );
		TimeUntilNextState = 40;
		RoundState = RoundState.Intermission;

		// lower hp / kill angry zombies!
		foreach(var zom in Entity.All.OfType<CommonZombie>() )
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

	public void RestartGame()
	{
		PlaySound( "bell" );
		WaveNumber = 0;

		foreach ( var npc in Entity.All.OfType<BaseZombie>().ToArray() )
			npc.Delete();

		foreach ( var item in Entity.All.OfType<DeathmatchWeapon>().ToArray() )
			item.Delete();

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
	End // everyone died and the game ended
}
