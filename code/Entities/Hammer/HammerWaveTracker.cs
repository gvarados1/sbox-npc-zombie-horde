using System.Linq.Expressions;

namespace ZombieHorde;

/// <summary>
/// Fires outputs when waves start/end
/// </summary>
[Library( "zom_wavetracker" ), HammerEntity]
[Title( "Wave Tracker" ), Category( "Logic" ), Icon( "settings" )]
public partial class HammerWaveTracker : Entity
{
	/// <summary>
	/// Outputs will be fired when this wave is reached
	/// </summary>
	[Property]
	public int TargetWave { get; set; } = 1;

	/// <summary>
	/// Fired when any wave ends
	/// </summary>
	protected Output OnWaveEnd { get; set; }

	/// <summary>
	/// Fired when the specified wave starts
	/// </summary>
	protected Output OnTargetWaveEnd { get; set; }

	public void WaveEnd()
	{
		OnWaveEnd.Fire(this);
		if( (BaseGamemode.Current as SurvivalGamemode ).WaveNumber == TargetWave)
			OnTargetWaveEnd.Fire( this );
	}

	/// <summary>
	/// Fired when any wave starts
	/// </summary>
	protected Output OnWaveStart { get; set; }

	/// <summary>
	/// Fired when the specified wave starts
	/// </summary>
	protected Output OnTargetWaveStart { get; set; }

	public void WaveStart()
	{
		OnWaveStart.Fire( this );
		if ( (BaseGamemode.Current as SurvivalGamemode).WaveNumber == TargetWave )
			OnTargetWaveStart.Fire( this );
	}

	/// <summary>
	/// Fired when the game ends, at the start of the "Victory" or "Defeat" screen.
	/// </summary>
	protected Output OnGameEnd { get; set; }

	public void GameEnd()
	{
		OnGameEnd.Fire( this );
	}
}
