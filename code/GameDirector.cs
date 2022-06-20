namespace ZombieHorde;

public partial class GameDirector : Entity
{
	public override void Spawn()
	{
		base.Spawn();

	}

	[Event.Tick.Server]
	public void Tick()
	{
	
	}
}
