namespace ZombieHorde;

public partial class SurvivalGamemode : BaseGamemode
{
	[ConVar.Replicated]
	public static float survival_round_length { get; set; } = 30;

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
		var roundTimeUntil = TimeUntilNextState;

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
		TimeUntilNextState = survival_round_length;
		WaveNumber++;
		RoundState = RoundState.WaveActive;
	}
	public void StartIntermission()
	{
		TimeUntilNextState = 20;
		RoundState = RoundState.Intermission;
	}

}

public enum RoundState
{
	PreGame, // waiting for players
	WaveActive, // round is active, spawning zombies
	Intermission, // in-between rounds. maybe spawn a couple wandering zombies?
	End // everyone died and the game ended
}
