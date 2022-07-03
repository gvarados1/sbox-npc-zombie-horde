namespace ZombieHorde;
public partial class DeathmatchHud : HudEntity<HudRootPanel>
{
	[ClientRpc]
	public void OnPlayerDied( HumanPlayer player )
	{
		Host.AssertClient();
	}

	[ClientRpc]
	public void ShowDeathScreen( string attackerName )
	{
		Host.AssertClient();
	}
}
