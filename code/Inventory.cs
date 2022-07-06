using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ZombieHorde;

partial class ZomInventory : BaseInventory
{
	// probably should have used an array or something for this
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
				if ( Primary1.IsValid() )
				{
					if( !Primary2.IsValid() )
					{
						Primary2 = weapon;
					}
					else
					{
						return false;
					}
				}
				else
				{
					Primary1 = weapon;
				}
				break;
			case WeaponSlot.Grenade:
				if ( Grenade.IsValid() ) return false;
				Grenade = weapon;
				break;
			case WeaponSlot.Medkit:
				if ( Medkit.IsValid() ) return false;
				Medkit = weapon;
				break;
			case WeaponSlot.Pills:
				if ( Pills.IsValid() ) return false;
				Pills = weapon;
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

		Log.Info( $"{Host.Name}: 1:{Secondary} 2:{Primary1} 3:{Primary2} 4:{Grenade} 5:{Medkit} 6:{Pills}" );

		return true;
	}

	public override Entity GetSlot( int i )
	{
		//Log.Info( Host.Name );
		switch ( i )
		{
			case 0: return Secondary;
			case 1: return Primary1;
			case 2: return Primary2;
			case 3: return Grenade;
			case 4: return Medkit;
			case 5: return Pills;
		}
		return base.GetSlot( i );
	}

	public override Entity DropActive()
	{
		// is there a better way to do this?
		var weapon = (Owner as HumanPlayer).ActiveChild;
		if ( weapon == Secondary ) Secondary = null;
		if ( weapon == Primary1 ) Primary1 = null;
		if ( weapon == Primary2 ) Primary2 = null;
		if ( weapon == Grenade ) Grenade = null;
		if ( weapon == Medkit ) Medkit = null;
		if ( weapon == Pills ) Pills = null;

		return base.DropActive();
	}

	public override void DeleteContents()
	{
		base.DeleteContents();
		Secondary = null;
		Primary1 = null;
		Primary2 = null;
		Grenade = null;
		Medkit = null;
		Pills = null;
	}


	public override void OnChildAdded( Entity child )
	{
		base.OnChildAdded( child );

		if ( Host.IsServer ) return;
		var weapon = child as BaseZomWeapon;
		if ( !weapon.IsValid() ) return;
		// figure out which weapon we have
		switch ( weapon.WeaponSlot )
		{
			case WeaponSlot.Secondary:
				Secondary = weapon;
				break;
			case WeaponSlot.Primary:
				if ( Primary1.IsValid() )
				{
					if ( !Primary2.IsValid() )
					{
						Primary2 = weapon;
					}
				}
				else
				{
					Primary1 = weapon;
				}
				break;
			case WeaponSlot.Grenade:
				Grenade = weapon;
				break;
			case WeaponSlot.Medkit:
				Medkit = weapon;
				break;
			case WeaponSlot.Pills:
				Pills = weapon;
				break;
		}
	}

	public override void OnChildRemoved( Entity child )
	{
		base.OnChildRemoved( child );

		var weapon = child as BaseZomWeapon;
		if ( weapon == Secondary ) Secondary = null;
		if ( weapon == Primary1 ) Primary1 = null;
		if ( weapon == Primary2 ) Primary2 = null;
		if ( weapon == Grenade ) Grenade = null;
		if ( weapon == Medkit ) Medkit = null;
		if ( weapon == Pills ) Pills = null;
	}

	public bool IsCarryingType( Type t )
	{
		return List.Any( x => x.IsValid() && x.GetType() == t );
	}
}
