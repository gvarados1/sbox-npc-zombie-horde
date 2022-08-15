using Sandbox;
using System.IO;

namespace ZombieHorde;

public partial class ChargerZombie : SpecialZombie
{
	public override void Spawn()
	{
		base.Spawn();
		SetModel( "models/zombie/charger/charger_zombie.vmdl" );
		Scale = 1.25f;
		Health = Health * 2;
		RunSpeed *= .4f;
		AttackDamage = 12;

		// disable torso and feet manually. not sure why it's not getting auto-disabled.
		SetBodyGroup( 1, 1 );
		SetBodyGroup( 4, 1 );
	}
	public override void TakeDamage( DamageInfo info )
	{
		if ( GetHitboxGroup( info.HitboxIndex ) == 1 )
		{
			// reduce headshot damage multiplier
			info.Damage /= 1.25f;
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
				"models/zombie/charger/charger_zombie01.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/shorts/summer_shorts/summer shorts.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/shoes/trainers/trainers.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/shirt/tanktop/tanktop.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/hair/hair_shortscruffy/hair_shortscruffy_brown.clothing",
				"models/citizen_clothes/hair/hair_shortscruffy/hair_shortscruffy_black.clothing",
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
