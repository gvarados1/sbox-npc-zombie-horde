using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;

namespace ZombieHorde;

public class InventoryBar : Panel
{
	readonly List<InventoryIcon> slots = new();

	public InventoryBar()
	{
		for ( int i = 0; i < 6; i++ )
		{
			var icon = new InventoryIcon( i + 1, this );
			slots.Add( icon );
			icon.SetClass( "small", i >= 3 );
		}
	}

	public override void Tick()
	{
		base.Tick();

		var player = Local.Pawn as Player;
		if ( player == null ) return;
		if ( player.Inventory == null ) return;

		for ( int i = 0; i < slots.Count; i++ )
		{
			UpdateIcon( player.Inventory.GetSlot( i ), slots[i], i );
		}
	}

	private static void UpdateIcon( Entity ent, InventoryIcon inventoryIcon, int i )
	{
		var player = Local.Pawn as Player;

		if ( ent == null )
		{
			inventoryIcon.Clear();

			//if ( i >= 3 )
			{
				inventoryIcon.SetClass( "hidden", true );
			}
			return;
		}
		inventoryIcon.SetClass( "hidden", false );

		var di = DisplayInfo.For( ent );

		inventoryIcon.TargetEnt = ent;
		inventoryIcon.SetClass( "active", player.ActiveChild == ent );
		

		if (ent is BaseZomWeapon wep )
		{
			// format ammo count depending on single use, infite, or refillable reserve
			//var ammo = wep.AmmoMax == 0 ? wep.AmmoClip.ToString() : wep.AmmoMax == -1 ? $"{wep.AmmoClip}/∞" : $"{wep.AmmoClip}/{wep.AmmoReserve}";
			inventoryIcon.Bullets.Text = wep.AmmoClip.ToString();
			inventoryIcon.BulletReserve.Text = wep.AmmoMax == 0 ? "" : wep.AmmoMax == -1 ? "∞" : wep.AmmoReserve.ToString();
			inventoryIcon.Icon.SetTexture( wep.Icon );
			inventoryIcon.RarityBar.Style.BackgroundColor = wep.RarityColor;

			if(wep.AmmoMax == -2 )
			{
				inventoryIcon.Bullets.Text = "";
				inventoryIcon.BulletReserve.Text = "";
			}

			if(i >= 3 )
			{
				if(wep.AmmoMax > 0 )
				{
					inventoryIcon.Bullets.Text = $"{wep.AmmoClip + wep.AmmoReserve}/{wep.AmmoMax + wep.ClipSize}";
					inventoryIcon.BulletReserve.Text = "";
				}
				else if(wep.AmmoMax == 0)
				{
					inventoryIcon.Bullets.Text = "";
				}
			}
		}
	}

	[Event( "buildinput" )]
	public void ProcessClientInput( InputBuilder input )
	{
		var player = Local.Pawn as Player;
		if ( player == null )
			return;

		var inventory = player.Inventory;
		if ( inventory == null )
			return;

		if ( input.Pressed( InputButton.Slot1 ) ) SetActiveSlot( input, inventory, 0 );
		if ( input.Pressed( InputButton.Slot2 ) ) SetActiveSlot( input, inventory, 1 );
		if ( input.Pressed( InputButton.Slot3 ) ) SetActiveSlot( input, inventory, 2 );
		if ( input.Pressed( InputButton.Slot4 ) ) SetActiveSlot( input, inventory, 3 );
		if ( input.Pressed( InputButton.Slot5 ) ) SetActiveSlot( input, inventory, 4 );
		if ( input.Pressed( InputButton.Slot6 ) ) SetActiveSlot( input, inventory, 5 );
		if ( input.Pressed( InputButton.Slot7 ) ) SetActiveSlot( input, inventory, 6 );
		if ( input.Pressed( InputButton.Slot8 ) ) SetActiveSlot( input, inventory, 7 );
		if ( input.Pressed( InputButton.Slot9 ) ) SetActiveSlot( input, inventory, 8 );

		if ( input.MouseWheel != 0 ) SwitchActiveSlot( input, inventory, -input.MouseWheel );
	}

	private static void SetActiveSlot( InputBuilder input, IBaseInventory inventory, int i )
	{
		var player = Local.Pawn as Player;

		if ( player == null )
			return;

		var ent = inventory.GetSlot( i );
		if ( player.ActiveChild == ent )
			return;

		if ( ent == null )
			return;

		input.ActiveChild = ent;
	}

	private static void SwitchActiveSlot( InputBuilder input, IBaseInventory inventory, int idelta )
	{
		var count = inventory.Count();
		if ( count == 0 ) return;

		var slot = inventory.GetActiveSlot();
		var nextSlot = slot + idelta;

		while ( nextSlot < 0 ) nextSlot += count;
		while ( nextSlot >= count ) nextSlot -= count;

		SetActiveSlot( input, inventory, nextSlot );
	}
}
