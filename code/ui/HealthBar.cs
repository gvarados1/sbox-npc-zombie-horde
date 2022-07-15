using Sandbox.Internal;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace ZombieHorde;
public partial class HealthBar : Panel
{
	public Image Icon;
	public Label CurrentHealth, MaxHealth, Bar, BarGray;

	public HealthBar()
	{
		//Icon = Add.Image( "ui/character.png", "icon" );
		CurrentHealth = Add.Label( "0", "health-current" );
		MaxHealth = Add.Label( "100", "health-max" );
		BarGray = Add.Label( "", "health-bar-gray" );
		Bar = Add.Label( "", "health-bar" );
		CreateAvatar();
	}

	public override void Tick()
	{
		var player = Local.Pawn as HumanPlayer;
		if ( player == null ) return;

		CurrentHealth.Text = $"{player.Health.CeilToInt()}";
		MaxHealth.Text = $"/{player.MaxHealth.CeilToInt()}";
		var width = 200;
		Bar.Style.Width = (width * (player.Health / player.MaxHealth).Clamp( 0, 1 ));

		// set healthbar color
		var color = Color.Green;
		if ( player.Health / player.MaxHealth <= .8f ) color = Color.Yellow;
		if ( player.Health / player.MaxHealth <= .5f ) color = Color.Orange;
		if ( player.Health / player.MaxHealth <= .2f ) color = Color.Red;

		Bar.Style.BackgroundColor = color;

		SetClass( "low", player.Health < 40.0f );
		SetClass( "empty", player.Health <= 0.0f );

		// probably a better way to do this. todo: research flexboxes ??
		var right = 158;
		var left = 125;
		var offset = -24;//-12;
		offset += 15 * (int)Math.Log10( player.Health.CeilToInt() );
		CurrentHealth.Style.Right = right - offset;
		MaxHealth.Style.Left = left + offset;
	}
}
