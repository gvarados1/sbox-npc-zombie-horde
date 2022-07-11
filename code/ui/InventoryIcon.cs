using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ZombieHorde;

public class InventoryIcon : Panel
{
	public Entity TargetEnt;
	public Label Number;
	public Label Bullets; // note: "Ammo" is a class
	public Label BulletReserve;
	public Image Icon;
	public Label RarityBar;

	public InventoryIcon( int i, Panel parent )
	{
		Parent = parent;
		Number = Add.Label( $"{i}", "slot-number" );
		Bullets = Add.Label( "?/?", "ammo-count" );
		BulletReserve = Add.Label( "?/?", "ammo-reserve" );
		RarityBar = Add.Label( "", "right-bar" );
		Icon = Add.Image( null, "icon" );
		
	}

	public void Clear()
	{
		Bullets.Text = "";
		BulletReserve.Text = "";
		SetClass( "active", false );
		Icon.Texture = null;
	}
}
