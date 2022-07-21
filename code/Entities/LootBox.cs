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
	}

	public override void OnKilled()
	{
		// EPIC LOOT TABLES
		var lootTable = new[]
			{
				"HealthKit",
				//"Magnum",
				"F1",
				"AKM",
				"M1A",
				"Mp5",
				"BaseballBat",
				"FireAxe",
				"R870",
				//"GrenadeWeapon",
				"TripmineWeapon",
				"AmmoPile",
				"PipeBomb"
			};

		if ( (BaseGamemode.Current as SurvivalGamemode).WaveNumber < 3 )
		{
			lootTable = new[]
			{
				"HealthKit",
				//"Magnum",
				"BaseballBat",
				"FireAxe",
				//"GrenadeWeapon",
				"TripmineWeapon",
				"PipeBomb"
			};
		}

		// lol just 1 item for now
		for ( var i = 0; i < Rand.Int( 0 ) + 1; i++ )
		{
			var index = Rand.Int( lootTable.Length - 1 );
			Type t = Type.GetType( lootTable[index] );
			var prize = TypeLibrary.Create( lootTable[index], t ) as Entity;
			prize.Position = Position + Vector3.Up * 8;
			prize.Velocity = Vector3.Random * 100;
		}

		// always spawn a medkit
		var medkit = new HealthKit();
		medkit.Position = Position;
		medkit.Velocity = Vector3.Random * 100;

		base.OnKilled();
	}

	[ConCmd.Admin]
	public static void zom_clear_lootboxs()
	{
		foreach ( var box in Entity.All.OfType<LootBox>() )
			box.Delete();
	}
}
