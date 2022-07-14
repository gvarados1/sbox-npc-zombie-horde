using Sandbox;
using Sandbox.Component;
using Sandbox.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;

namespace ZombieHorde;

/// <summary>
/// Resupplies weapon ammunition
/// </summary>
[Library( "zom_ammopile" ), HammerEntity]
[EditorModel( "assets/ammobox/ammo_box.vmdl" )]
[Title( "Ammo Pile" )]
partial class AmmoPile : ModelEntity, IUse
{
	public static readonly Model WorldModel = Model.Load( "assets/ammobox/ammo_box.vmdl" );

	/// <summary>
	/// 100 Refills 1 weapon completely. 400 Refills 4.
	/// </summary>
	[Net, Property(Title = "Total Ammo (if not infinite)")]
	public float AmmoRemaining { get; set; } = 400;

	[Property(Title = "Infinite Ammo")]
	public bool IsInfinite { get; set; } = false;

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;

		PhysicsEnabled = true;
		UsePhysicsCollision = true;

		var glow = Components.GetOrCreate<Glow>();
		glow.Active = true;
		glow.Color = Color.Yellow;
		glow.RangeMin = 0;
		glow.RangeMax = int.MaxValue;

		Tags.Add( "item" );
	}

	[Event.Tick]
	public void Tick()
	{
		DebugOverlay.Text(AmmoRemaining + "%", Position + Vector3.Up * 8, 0, 180);
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}

	public bool OnUse( Entity user )
	{
		if ( (user as HumanPlayer).ActiveChild is BaseZomWeapon weapon )
		{
			if(weapon.WeaponSlot == WeaponSlot.Primary ) // only primary weapons use ammo
			{
				// figure out how much ammo to actually give
				var maxAmmo = weapon.AmmoMax + weapon.ClipSize;
				var currentAmmo = weapon.AmmoClip + weapon.AmmoReserve;

				if ( currentAmmo == maxAmmo ) return false;

				var requestAmount = maxAmmo - currentAmmo;
				var requestPercent = MathF.Ceiling( (float)requestAmount / (float)maxAmmo * 100f );

				if( !IsInfinite )
				{
					AmmoRemaining -= requestPercent;
					if ( AmmoRemaining < 0 )
					{
						requestPercent += AmmoRemaining;
						requestAmount = (int)(requestPercent / 100 * maxAmmo);
					}
				}

				// congrats! enjoy your ammo!
				PlaySound( "ammobox.replenish" );
				weapon.AmmoReserve += requestAmount;
				CheckForDeletion();
			}
		}
		return false;
	}

	public void CheckForDeletion()
	{
		if ( !IsInfinite && AmmoRemaining <= 0 )
		{
			// maybe puff some smoke or play a sound?
			Delete();
		}
	}
}
