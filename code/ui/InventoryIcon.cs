using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ZombieHorde;

public class InventoryIcon : Panel
{
	public Entity TargetEnt;
	public Label Number;
	public Label Bullets; // note: "Ammo" is a class
	public Image Icon;

	public InventoryIcon( int i, Panel parent )
	{
		Parent = parent;
		Number = Add.Label( $"{i}", "slot-number" );
		Bullets = Add.Label( "?/?", "ammo-count" );
		Add.Label( "", "right-bar" );
		Icon = Add.Image( null, "icon" );
	}

	public void Clear()
	{
		Bullets.Text = "";
		SetClass( "active", false );
		// can I not just set the texture to null? why do I have to use a blank png?
		Icon.SetTexture( "/ui/weapons/empty.png" );
	}
}
