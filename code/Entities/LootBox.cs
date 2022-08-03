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

		// I have to enable this dumb broken glow to get the crate to render through the world
		var glow = Components.GetOrCreate<Glow>();
		glow.Active = true;
		glow.Color = Color.Yellow;
		glow.RangeMin = 0;
		//glow.RangeMax = int.MaxValue;
		glow.RangeMax = 1;

		Transmit = TransmitType.Always;
		EnableDrawOverWorld = true;
		SetGlowMaterial();

		AsyncPing( .5f );

		Health = 15;
	}

	public async void AsyncPing(float time )
	{
		// need a slight delay to make sure the parent gets set properly on clients!
		await Task.DelaySeconds( time );
		PingMarker.Ping( To.Everyone, Position, PingType.Lootbox, "Treasure!", -1, this );
	}

	[ClientRpc]
	public void SetGlowMaterial()
	{
		SetMaterialOverride( Material.Load( "models/sbox_props/wooden_crate/wooden_crate_glow.vmat" ) );
		var glow = Components.GetOrCreate<Glow>();
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
				"Revolver",
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
				"Revolver",
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

		// always spawn a healing item
		lootTable = new[]
		{
				"HealthKit",
				"HealthSyringe",
		};
		var index1 = Rand.Int( lootTable.Length - 1 );
		Type t1 = Type.GetType( lootTable[index1] );
		var medkit = TypeLibrary.Create( lootTable[index1], t1 ) as Entity;
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
