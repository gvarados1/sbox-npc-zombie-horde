namespace ZombieHorde;

partial class ZomInventory : BaseInventory
{
	public BaseZomWeapon Secondary { get; set; } // slot 1
	public BaseZomWeapon Primary1 { get; set; } //2
	public BaseZomWeapon Primary2 { get; set; } //3
	public BaseZomWeapon Grenade { get; set; } //4
	public BaseZomWeapon Medkit { get; set; } //5
	public BaseZomWeapon Pills { get; set; } //6

	public ZomInventory( Player player ) : base( player )
	{

	}

	public override bool Add( Entity ent, bool makeActive = false )
	{
		var player = Owner as HumanPlayer;
		var weapon = ent as BaseZomWeapon;
		var notices = !player.SupressPickupNotices;

		if ( weapon == null )
			return false;

		// figure out which weapon we have
		switch ( weapon.WeaponSlot )
		{
			case WeaponSlot.Secondary:
				if ( Secondary.IsValid() ) return false;
				Secondary = weapon;
				break;
			case WeaponSlot.Primary:
				if ( Secondary.IsValid() ) return false;
				Secondary = weapon;
				break;
			case WeaponSlot.Grenade:
				if ( Grenade.IsValid() ) return false;
				Secondary = weapon;
				break;
			case WeaponSlot.Medkit:
				if ( Medkit.IsValid() ) return false;
				Secondary = weapon;
				break;
			case WeaponSlot.Pills:
				if ( Pills.IsValid() ) return false;
				Secondary = weapon;
				break;

		}

		if ( !base.Add( ent, makeActive ) )
			return false;

		if ( weapon != null && notices )
		{
			var display = DisplayInfo.For( ent );

			Sound.FromWorld( "dm.pickup_weapon", ent.Position );
			PickupFeed.OnPickupWeapon( To.Single( player ), display.Name );
		}

		return true;
	}

	public bool IsCarryingType( Type t )
	{
		return List.Any( x => x.IsValid() && x.GetType() == t );
	}
}
