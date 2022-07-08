using Sandbox;
using Sandbox.Component;
using System;
using System.Collections.Generic;

namespace ZombieHorde;

partial class LootBox : Prop
{
	public override void Spawn()
	{
		base.Spawn();
		SetModel( "models/sbox_props/wooden_crate/wooden_crate.vmdl" );
		var glow = Components.GetOrCreate<Glow>();
		glow.Active = true;
		glow.Color = Color.Yellow;
		glow.RangeMin = 0;
		glow.RangeMax = int.MaxValue;
		Transmit = TransmitType.Always;
		EnableDrawOverWorld = true;

		Health = 15;

		//CollisionGroup = CollisionGroup.Debris;
		//SetInteractsAs( CollisionLayer.Debris ); 
	}

	public override void OnKilled()
	{
		// EPIC LOOT TABLES
		var lootTable = new[]
			{
				"HealthKit",
				"HealthKit",
				"HealthKit",
				"Magnum",
				"Shotgun",
				"SMG",
				"AK47",
				"Crossbow",
				//"GrenadeWeapon",
				//"GrenadeWeapon",
				"TripmineWeapon",
				"AmmoPile"
			};

		for (var i = 0; i < Rand.Int(2)+1; i++ )
		{
			var index = Rand.Int( lootTable.Length - 1 );
			Type t = Type.GetType( lootTable[index] );
			var prize = TypeLibrary.Create( lootTable[index], t ) as Entity;
			prize.Position = Position;
			prize.Velocity = Vector3.Random * 100;
		}

		// always spawn a medkit
		var medkit = new HealthKit();
		medkit.Position = Position;
		medkit.Velocity = Vector3.Random * 100;

		base.OnKilled();
	}
}
