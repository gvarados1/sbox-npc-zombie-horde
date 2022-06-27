namespace ZombieHorde;

public partial class SurvivalGamemode : BaseGamemode
{
	[ConVar.Replicated]
	public static float survival_round_length { get; set; } = 5;

	[Net]
	public TimeUntil TimeUntilNextState { get; set; }
	[Net]
	public int WaveNumber { get; set; } = 0;


	public RoundState RoundState { get; set; }
	public override void Spawn()
	{
		Log.Info( "Survival gamemode active!" );
		RoundState = RoundState.PreGame;
		TimeUntilNextState = 30;

		base.Spawn();
	}

	public override void Tick()
	{
		base.Tick();

		var roundName = "Unknown Game State";
		var roundTimeUntil = Math.Round(TimeUntilNextState, 2);

		if ( RoundState == RoundState.PreGame )
		{
			roundName = "Pre-Game";

			if( TimeUntilNextState <= 0 ) StartWave();
		}
		else if(RoundState == RoundState.WaveActive )
		{
			roundName = "Wave " + WaveNumber + " Active";

			if ( TimeUntilNextState <= 0 ) StartIntermission();
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
		TimeUntilNextState = survival_round_length;
		WaveNumber++;
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
		TimeUntilNextState = 20;
		RoundState = RoundState.Intermission;

		// lower hp / kill angry zombies!
		foreach(var zom in Entity.All.OfType<CommonZombie>() )
		{
			if(zom.ZombieState == ZombieState.Chase )
			{
				Velocity = 0;
				Sound.FromWorld( "rust_pumpshotgun.shootdouble", zom.Position );
				var damageInfo = DamageInfo.Explosion( zom.Position, Vector3.Up*20,40 );
				zom.TakeDamage( damageInfo );
			}
		}
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
