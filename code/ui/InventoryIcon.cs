using Sandbox.UI;

namespace ZombieHorde;

class InventoryIcon : Panel
{
	public BaseZomWeapon Weapon;
	public Panel Icon;

	public InventoryIcon( BaseZomWeapon weapon )
	{
		Weapon = weapon;
		Icon = Add.Panel( "icon" );

		AddClass( weapon.ClassName );
	}

	internal void TickSelection( BaseZomWeapon selectedWeapon )
	{
		SetClass( "active", selectedWeapon == Weapon );
		SetClass( "empty", !Weapon?.IsUsable() ?? true );
	}

	public override void Tick()
	{
		base.Tick();

		if ( !Weapon.IsValid() || Weapon.Owner != Local.Pawn )
			Delete( true );
	}
}
