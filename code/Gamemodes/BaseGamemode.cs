namespace ZombieHorde;

public partial class BaseGamemode : Entity
{
	public static BaseGamemode Ent { get; set; } // this feels like a hack. I just want a static class
	public static int TotalPlayers => Entity.All.OfType<Player>().Count(); // do I need this?
	public int ZombiesRemaining { get; set; } = 0;


	public override void Spawn()
	{
		base.Spawn();
		Ent = this;
	}

	[Event.Tick.Server]
	public virtual void Tick()
	{
		// empty for now :)
	}
	public int GetLivePlayerCount()
	{
		var livePlayers = 0;
		foreach (var ply in Entity.All.OfType<DeathmatchPlayer>())
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
