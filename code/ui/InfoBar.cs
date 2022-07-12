using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace ZombieHorde;

internal class InfoBar : Panel
{
	public Label Timer;
	public Label State;

	public InfoBar()
	{
		State = Add.Label( string.Empty, "title-bar" );
		Timer = Add.Label( string.Empty, "description" );
	}

	public override void Tick()
	{
		base.Tick();

		var game = Game.Current as ZombieGame;
		if ( !game.IsValid() ) return;

		var gamemode = BaseGamemode.Ent;
		Timer.Text = "- " + gamemode.RoundInfo;
		State.Text = gamemode.RoundName;
	}

}

