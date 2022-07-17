namespace ZombieHorde;

public partial class BaseGamemode : Entity
{
	public static BaseGamemode Current { get; set; } // this feels like a hack. I just want a static class
	//public static int TotalPlayers => Entity.All.OfType<Player>().Count(); // do I need this?
	[Net]
	public int ZombiesRemaining { get; set; } = 0;
	[Net]
	public string RoundInfo { get; set; } = "unknown";
	[Net]
	public string RoundName { get; set; } = "unknown";
	[Net]
	public int HumanMaxRevives { get; set; } = 3;
	public float ZomHealthMultiplier { get; set; } = 1;
	public float ZomSpeedMultiplier { get; set; } = 1;
	public float ZomSpawnRate { get; set; } = 1;
	public float ZomMaxZombies { get; set; } = 1;
 
	public BaseGamemode()
	{
		Current = this;
	}

	public override void Spawn()
	{
		base.Spawn();
		Transmit = TransmitType.Always;
	}

	[ConVar.Server]
	public static bool zom_debug_round_info { get; set; } = false;

	[Event.Tick]
	public virtual void Tick()
	{
		if ( zom_debug_round_info )
		{
			var i = 9;
			DebugOverlay.ScreenText( $"HealthMultiplier: {ZomHealthMultiplier}", i ); i++;
			DebugOverlay.ScreenText( $"SpeedMultiplier: {ZomSpeedMultiplier}", i ); i++;
			DebugOverlay.ScreenText( $"ExampleSpeed: {140 * ZomSpeedMultiplier}", i ); i++;
			DebugOverlay.ScreenText( $"ExampleHealth: {50 * ZomHealthMultiplier}", i ); i++;
			DebugOverlay.ScreenText( $"SpawnRate: {1 / ZomSpawnRate}", i ); i++;
			DebugOverlay.ScreenText( $"MaxZombies: {ZomMaxZombies}", i ); i++;
		}
		// empty for now :)
	}
	public int GetLivePlayerCount()
	{
		var livePlayers = 0;
		foreach (var ply in Entity.All.OfType<HumanPlayer>())
		{
			if ( ply.LifeState == LifeState.Alive) livePlayers++;
		}
		return livePlayers;
	}

	public virtual bool EnableRespawning()
	{
		return true;
	}

	public virtual bool PopulateZombiesAngry()
	{
		return false;
	}
}
