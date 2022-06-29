using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace ZombieHorde;

internal class GameHud : Panel
{
	public Label Timer;
	public Label State;

	public GameHud()
	{
		State = Add.Label( string.Empty, "game-state" );
		Timer = Add.Label( string.Empty, "game-timer" );
	}

	public override void Tick()
	{
		base.Tick();

		var game = Game.Current as ZombieGame;
		if ( !game.IsValid() ) return;

		var gamemode = BaseGamemode.Ent;
		Timer.Text = gamemode.RoundInfo;
		State.Text = gamemode.RoundName;
	}

}

