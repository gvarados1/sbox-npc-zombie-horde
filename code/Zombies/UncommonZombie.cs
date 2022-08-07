using Sandbox;
using System.IO;

namespace ZombieHorde;

public partial class UncommonZombie : CommonZombie
{
	public override void Spawn()
	{
		base.Spawn();
		Health = Health * 2;
		RunSpeed *= .4f;
	}
	public override void TakeDamage( DamageInfo info )
	{
		if ( GetHitboxGroup( info.HitboxIndex ) == 1 )
		{
			// undo headshot damage multiplier lol
			info.Damage /= 1.5f;
		}
		if ( info.Flags.HasFlag( DamageFlags.Bullet ) )
			info.Damage *= .5f;

		base.TakeDamage( info );
	}

	public override void DamagedEffects()
	{
		Velocity *= 0.75f;
		if ( Health > 0 )
			PlaySoundOnClient( "zombie.hurt" );
	}

	[ClientRpc]
	public override void PlaySoundOnClient( string sound )
	{
		//PlaySound( "zombie.hurt" );
		var snd = Sound.FromWorld( sound, Position + Vector3.Up * 60 );
		snd.SetPitch( .9f );
		//SetAnimParameter( "b_talking", true );
	}

	public override void UpdateClothes()
	{

		Clothing ??= new();
		Clothing item;
		String model;

		model = Rand.FromArray( new[]
			{
				"models/zombie/citizen_zombie/skins/skin_zombie01.clothing",
				"models/zombie/citizen_zombie/skins/skin_zombie02.clothing",
				"models/zombie/citizen_zombie/skins/skin_zombie03.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/trousers/smarttrousers/trousers.smart.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/shoes/sneakers/sneakers.clothing",
				"models/citizen_clothes/shoes/trainers/trainers.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/shirt/longsleeve_shirt/longsleeve_shirt.clothing",
				"models/citizen_clothes/jacket/hoodie/hoodie.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/vest/tactical_vest/models/tactical_vest.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/hat/tactical_helmet/tactical_helmet.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		if ( Rand.Int( 4 ) == 1 )
		{
			model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/hair/moustache/moustache_brown.clothing",
				"models/citizen_clothes/hair/moustache/moustache_grey.clothing",
				"models/citizen_clothes/hair/stubble/stubble.clothing"
			} );
			if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }
		}
	}
}
