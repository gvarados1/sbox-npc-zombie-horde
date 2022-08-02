using Sandbox;
using Sandbox.Internal;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace ZombieHorde;
public partial class HealthBar : Panel
{
	public Image Icon;
	public Label CurrentHealth, MaxHealth, Bar, BarGray;
	public static HealthBar Current { get; private set; }

	public HealthBar()
	{
		Current = this;
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

		TickAvatar();

		CurrentHealth.Text = $"{player.Health.CeilToInt()}";
		MaxHealth.Text = $"/{player.MaxHealth.CeilToInt()}";
		var width = 200;
		Bar.Style.Width = (width * (player.Health / player.MaxHealth).Clamp( 0, 1 ));

		// probably a better way to do this. todo: research flexboxes ??
		var right = 158;
		var left = 125;
		var offset = -24;//-12;
		offset += 15 * (int)Math.Log10( player.Health.CeilToInt() );
		CurrentHealth.Style.Right = right - offset;
		CurrentHealth.Style.FontColor = (Color)Color.Parse( "#C5E0E3" );
		MaxHealth.Style.Left = left + offset;

		var lifeState = player.LifeState;
		if(player.CameraMode is SpectatePlayerCamera cam)
		{
			lifeState = cam.SpectateTarget.LifeState;
		}

		var color = Color.Green;
		// set healthbar color
		if ( lifeState == LifeState.Dying )
		{
			color = (Color)Color.Parse( "#FF0000" );
			if ( player.Health / player.MaxHealth <= .8f ) color = (Color)Color.Parse( "#BD0000" );
			if ( player.Health / player.MaxHealth <= .5f ) color = (Color)Color.Parse( "#9C0000" );
			if ( player.Health / player.MaxHealth <= .2f ) color = (Color)Color.Parse( "#800000" );
			CurrentHealth.Text = $"Incapacitated!";
			CurrentHealth.Style.Right = 41;
			CurrentHealth.Style.FontColor = (Color)Color.Parse( "#FF5B71" );
			MaxHealth.Text = $"";
			MaxHealth.Style.Left = 240;
			Bar.Style.Width = (width * (player.Health / 200).Clamp( 0, 1 ));
		}
		else if( lifeState == LifeState.Dead){
			CurrentHealth.Text = $"R.I.P.";
			CurrentHealth.Style.Right = 140;
			CurrentHealth.Style.FontColor = (Color)Color.Parse( "#90A4A6" );
			MaxHealth.Text = $"";
			MaxHealth.Style.Left = 142;
			Bar.Style.Width = 0;
		}
		else
		{
			color = Color.Green;
			if ( player.Health / player.MaxHealth <= .8f ) color = Color.Yellow;
			if ( player.Health / player.MaxHealth <= .5f ) color = Color.Orange;
			if ( player.Health / player.MaxHealth <= .2f ) color = Color.Red;
		}
		

		Bar.Style.BackgroundColor = color;

		SetClass( "low", player.Health < 40.0f );
		SetClass( "empty", player.Health <= 0.0f );
	}
}
