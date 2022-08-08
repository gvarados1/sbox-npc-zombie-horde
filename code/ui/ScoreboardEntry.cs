
using Sandbox;
using Sandbox.Hooks;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;

namespace ZombieHorde
{
	public partial class ZomScoreboardEntry : ScoreboardEntry
	{
		//public Client Client;

		//public Label PlayerName;
		//public Label Kills;
		//public Label Deaths;
		//public Label Health;
		//public Label Ping;

		public ZomScoreboardEntry()
		{
			//Health = Add.Label( "", "health" );
		}

		RealTimeSince TimeSinceUpdate = 0;

		public override void Tick()
		{
			base.Tick();

			if ( !IsVisible )
				return;

			if ( !Client.IsValid() )
				return;

			if ( TimeSinceUpdate < 0.1f )
				return;

			TimeSinceUpdate = 0;
			UpdateData();
		}

		public override void UpdateData()
		{
			PlayerName.Text = Client.Name;
			Kills.Text = Client.GetInt( "kills" ).ToString();
			Deaths.Text = Client.GetInt( "deaths" ).ToString();
			//Health.Text = ((int)Client.Pawn.Health).ToString();
			Ping.Text = Client.Ping.ToString();
			SetClass( "me", Client == Local.Client );
		}

		public override void UpdateFrom( Client client )
		{
			Client = client;
			UpdateData();
		}
	}
}
