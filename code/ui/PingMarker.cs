using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Buffers;
using System.Numerics;
using System.Security;

namespace ZombieHorde;

public partial class PingMarker : Entity
{
	public string Title;
	HudMarker Marker;
	PingType PingType;

	public PingMarker( Vector3 pos, PingType type = PingType.Generic, string title = null, float durration = 10, Entity parent = null )
	{
		Title = title;
		Position = pos;
		PingType = type;

		Sound.FromScreen( "ui.popup.message.open" );
		Transmit = TransmitType.Always;

		Marker = new HudMarker( this );
		HudRootPanel.Current.AddChild( Marker );

		//DebugOverlay.Sphere( Position, 20, Color.Blue, 10 );

		if ( parent != null )
			SetParent( parent );

		if(durration > 0)
			DeleteAsync( durration );
	}

	[Event.Tick]
	public void Tick()
	{
		if(Parent != null)
		Position = Parent.Position;

		if(Parent is HumanPlayer ply )
		{
			if(PingType == PingType.DownedPlayer && ply.LifeState != LifeState.Dying )
			{
				Delete();
			}
		}
	}

	[ClientRpc]
	public static void Ping( Vector3 pos, PingType type = PingType.Generic, string title = null, float durration = 10, Entity parent = null )
	{
		_ = new PingMarker(pos, type, title, durration, parent );
	}
}

public partial class HudMarker : Panel
{
	public Label Title;
	public PingMarker Owner;

	public Vector3 Position;

	public HudMarker( PingMarker marker)
	{
		Owner = marker;
		Title = Add.Label( Owner.Title, "title" );
		Position = Owner.Position;
	}

	public override void Tick()
	{
		if ( Owner.IsValid )
		{
			Position = Owner.Position;
			SetPosition();
		}
		else
		{
			Delete();
		}
	}

	public void SetPosition()
	{
		var hudPos = Position.ToScreen();

		var buffer = .04f;
		hudPos.x = hudPos.x.Clamp( buffer, 1 - buffer );
		hudPos.y = hudPos.y.Clamp( buffer, 1 - buffer );

		Style.Left = Length.Fraction( hudPos.x );
		Style.Top = Length.Fraction( hudPos.y );
	}
}

public enum PingType
{
	Generic,
	Danger,
	Item,
	Saftey,
	Lootbox,
	DownedPlayer
}
