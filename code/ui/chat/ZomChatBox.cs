using Sandbox.UI.Construct;
using Sandbox.UI;
using System;
using Sandbox.Internal;
using Sandbox;

namespace ZombieHorde
{
	public partial class ZomChatBox : Panel
	{
		static ZomChatBox Current;

		public Panel Canvas { get; protected set; }
		public TextEntry Input { get; protected set; }

		public ZomChatBox()
		{
			Current = this;

			StyleSheet.Load( "/resource/styles/_zomchatbox.scss" );

			Canvas = Add.Panel( "chat_canvas" );

			Input = Add.TextEntry( "" );
			Input.AddEventListener( "onsubmit", () => Submit() );
			Input.AddEventListener( "onblur", () => Close() );
			Input.AcceptsFocus = true;
			Input.AllowEmojiReplace = true;

			Sandbox.Hooks.Chat.OnOpenChat += Open;
		}

		void Open()
		{
			AddClass( "open" );
			Input.Focus();
		}

		void Close()
		{
			RemoveClass( "open" );
			Input.Blur();
		}

		void Submit()
		{
			Close();

			var msg = Input.Text.Trim();
			Input.Text = "";

			if ( string.IsNullOrWhiteSpace( msg ) )
				return;

			Say( msg );
		}

		public void AddEntry( string name, string message, string avatar, string color = null, string lobbyState = null )
		{
			var e = Canvas.AddChild<ZomChatEntry>();

			e.Message.Text = message;
			e.NameLabel.Text = name;
			e.Avatar.SetTexture( avatar );

			e.SetClass( "noname", string.IsNullOrEmpty( name ) );
			e.SetClass( "noavatar", string.IsNullOrEmpty( avatar ) );
			if ( color != null )
				e.NameLabel.Style.FontColor = color;

			if ( lobbyState == "ready" || lobbyState == "staging" )
			{
				e.SetClass( "is-lobby", true );
			}
		}


		[ConCmd.Client( "chat_add", CanBeCalledFromServer = true )]
		public static void AddChatEntry( string name, string message, string avatar = null, string color = null, string lobbyState = null )
		{
			Current?.AddEntry( name, message, avatar, color, lobbyState );

			// Only log clientside if we're not the listen server host
			if ( !Global.IsListenServer )
			{
				Log.Info( $"{name}: {message}" );
			}
		}

		[ConCmd.Client( "chat_addinfo", CanBeCalledFromServer = true )]
		public static void AddInformation( string message, string avatar = null, string color = null )
		{
			Current?.AddEntry( message, null, avatar, color );
		}

		[ConCmd.Server( "say" )]
		public static void Say( string message )
		{
			Assert.NotNull( ConsoleSystem.Caller );

			// todo - reject more stuff
			if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
			return;

			var color = "#7DFF8A";
			var player = ConsoleSystem.Caller.Pawn as HumanPlayer;
			if ( player != null )
			{
				// set healthbar color
				if ( player.LifeState == LifeState.Dying )
				{
					color = "#FF0000";
					if ( player.Health / player.MaxHealth <= .8f ) color = "#BD0000";
					if ( player.Health / player.MaxHealth <= .5f ) color = "#9C0000";
					if ( player.Health / player.MaxHealth <= .2f ) color = "#800000";
				}
				else if ( player.LifeState == LifeState.Dead )
				{
					color = "#90A4A6";
				}
				else
				{
					if ( player.Health / player.MaxHealth <= .8f ) color = "#FFFF8E";
					if ( player.Health / player.MaxHealth <= .5f ) color = "#FFC68B";
					if ( player.Health / player.MaxHealth <= .2f ) color = "#FF8588";
				}
			}

			Log.Info( $"{ConsoleSystem.Caller}: {message}" );
			AddChatEntry( To.Everyone, ConsoleSystem.Caller.Name, message, $"avatar:{ConsoleSystem.Caller.PlayerId}", color );
		}
	}
}
