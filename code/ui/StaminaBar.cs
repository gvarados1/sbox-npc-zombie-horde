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
		BarGray = Add.Label( "", "bar-gray" );
		Bar = Add.Label( "", "bar" );
		Add.Image( "", "icon" );
	}

	public override void Tick()
	{
		var player = Local.Pawn as HumanPlayer;
		if ( player == null ) return;

		var width = 250;
		Bar.Style.Width = ((width * (player.Stamina / player.MaxStamina).Clamp( 0, 1 )).Floor());

		SetClass( "hidden", player.Stamina >= player.MaxStamina );
	}
}
