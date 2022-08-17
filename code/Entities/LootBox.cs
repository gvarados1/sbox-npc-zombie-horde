using Sandbox;
using Sandbox.Component;
using System;
using System.Collections.Generic;

namespace ZombieHorde;

partial class LootBox : Prop
{
	public int WaveNumber = 100;
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
		WaveNumber = (BaseGamemode.Current as SurvivalGamemode).WaveNumber - 1;
	}

	[Event.Tick.Server]
	public void Tick()
	{
		// delete box if it somehow fell out of the map
		if ( Position.z < -20000 )
		Delete();
	}

	public async void AsyncPing(float time )
	{
		// need a slight delay to make sure the parent gets set properly on clients!
		await Task.DelaySeconds( time );
		if ( !IsValid ) return;
		// set the ping time to 5 minutes instead of infinite. If you don't collect it by then it's probably inaccessable.
		PingMarker.Ping( To.Everyone, Position, PingType.Lootbox, "Treasure!", 300, this );
	}

	[ClientRpc]
	public void SetGlowMaterial()
	{
		SetMaterialOverride( Material.Load( "models/sbox_props/wooden_crate/wooden_crate_glow.vmat" ) );
		var glow = Components.GetOrCreate<Glow>();
	}

	DamageInfo LastDamage;
	public override void TakeDamage( DamageInfo info )
	{
		LastDamage = info;
		base.TakeDamage( info );
	}

	public override void OnKilled()
	{
		// EPIC LOOT TABLES
		var lootTable = new List<Type>
			{
				typeof(F1),
				typeof(AKM),
				typeof(M1A),
				typeof(MP5),

				typeof(BaseballBat),
				typeof(FireAxe),
				typeof(Revolver),
				typeof(Shovel),

				typeof(R870),
				typeof(CompactShotgun),
				typeof(DoubleBarrel),
				typeof(HuntingRifle),

				typeof(AmmoPile),

				typeof(TripmineWeapon),
				typeof(TripmineWeapon),
				typeof(PipeBomb),
				typeof(PipeBomb),
				typeof(Molotov),
				typeof(Molotov),
			};

		if ( WaveNumber < 1 )
		{
			lootTable = new List<Type>
			{
				typeof(F1),
				typeof(M1A),
				typeof(MP5),
			};
		}
		else if ( WaveNumber < 2 )
		{
			lootTable = new List<Type>
			{
				typeof(Revolver),
				typeof(Revolver),
				typeof(BaseballBat),
				typeof(FireAxe),
				typeof(Shovel),
			};
		}
		else if ( WaveNumber < 3 )
		{
			lootTable = new List<Type>
			{
				typeof(TripmineWeapon),
				typeof(TripmineWeapon),
				typeof(PipeBomb),
				typeof(Molotov),
				typeof(Molotov),
			};
		}
		else if ( WaveNumber < 4 )
		{
			lootTable = new List<Type>
			{
				typeof(F1),
				typeof(M1A),
				typeof(MP5),
				typeof(Revolver),
				typeof(TripmineWeapon),
				typeof(Molotov),
			};
		}

		if(LastDamage.Attacker is HumanPlayer ply)
			foreach( var wep in ply.Children.OfType<BaseZomWeapon>().ToList() )
			{
				lootTable.Remove( wep.GetType() );
				lootTable.Remove( wep.GetType() );
				lootTable.Remove( wep.GetType() );
				lootTable.Remove( wep.GetType() );
			}

		// lol just 1 item for now
		for ( var i = 0; i < Rand.Int( 0 ) + 1; i++ )
		{
			var prize = TypeLibrary.Create<Entity>( lootTable.OrderBy( x => Guid.NewGuid() ).FirstOrDefault() );
			prize.Position = Position + Vector3.Up * 24;
			prize.Velocity = Vector3.Random * 100;
		}

		// always spawn a healing item
		lootTable = new List<Type>
			{
				typeof(HealthKit),
				typeof(HealthSyringe),
				typeof(Adrenaline),
			};

		var medkit = TypeLibrary.Create<Entity>( lootTable.OrderBy( x => Guid.NewGuid() ).FirstOrDefault() );
		medkit.Position = Position + Vector3.Up * 16;
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
