using Sandbox;
using Sandbox.Internal;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace ZombieHorde;
public partial class StaminaBar : Panel
{
	//public Image Icon;
	public Label Bar, BarGray;
	public static StaminaBar Current { get; private set; }

	public StaminaBar()
	{
		Current = this;
		Bar = Add.Label( "", "bar" );
		BarGray = Add.Label( "", "bar-gray" );
	}

	public override void Tick()
	{
		var player = Local.Pawn as HumanPlayer;
		if ( player == null ) return;

		var width = 200;
		Bar.Style.Width = (width * (player.Stamina / player.MaxStamina).Clamp( 0, 1 ));

		SetClass( "active", player.Stamina < player.MaxStamina );
	}
}
