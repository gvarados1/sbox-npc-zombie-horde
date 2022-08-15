namespace ZombieHorde;

public partial class BaseNpc
{
	// TODO: clean this up
	public ClothingContainer Clothing { get; protected set; }
	public virtual void UpdateClothes()
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
				"models/citizen_clothes/trousers/jeans/jeans.clothing",
				"models/citizen_clothes/shorts/summer_shorts/summer shorts.clothing",
				"models/citizen_clothes/trousers/smarttrousers/trousers.smart.clothing",
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/shoes/slippers/slippers.clothing",
				"models/citizen_clothes/shoes/smartshoes/smartshoes.clothing",
				"models/citizen_clothes/shoes/sneakers/sneakers.clothing",
				"models/citizen_clothes/shoes/trainers/trainers.clothing",
				""
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/shirt/flannel_shirt/flannel_shirt.clothing",
				"models/citizen_clothes/shirt/hawaiian_shirt/hawaiian shirt.clothing",
				"models/citizen_clothes/shirt/longsleeve_shirt/longsleeve_shirt.clothing",
				"models/citizen_clothes/shirt/tanktop/tanktop.clothing",
				"models/citizen_clothes/shirt/v_neck_tshirt/v_neck_tshirt.clothing",
				"models/citizen_clothes/shirt/flannel_shirt/variations/blue_shirt/blue_shirt.clothing",
				"models/citizen_clothes/jacket/brown_leather_jacket/brown_leather_jacket.clothing",
				"models/citizen_clothes/jacket/longsleeve/longsleeve.clothing",
				"models/citizen_clothes/jacket/hoodie/hoodie.clothing",
				""
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/hair/hair_balding/hair_baldingbrown.clothing",
				"models/citizen_clothes/hair/hair_balding/hair_baldinggrey.clothing",
				"models/citizen_clothes/hair/hair_bobcut/hair_bobcut.clothing",
				"models/citizen_clothes/hair/hair_fade/hair_fade.clothing",
				"models/citizen_clothes/hair/hair_longbrown/models/hair_longbrown.clothing",
				"models/citizen_clothes/hair/hair_longcurly/hair_longcurly.clothing",
				"models/citizen_clothes/hair/hair_longbrown/models/hair_longgrey.clothing",
				"models/citizen_clothes/hair/hair_wavyblack/hair_wavyblack.clothing",
				"models/citizen_clothes/hair/hair_looseblonde/hair.loose.blonde.clothing",
				"models/citizen_clothes/hair/hair_looseblonde/hair.loose.brown.clothing",
				"models/citizen_clothes/hair/hair_looseblonde/hair.loose.grey.clothing",
				"models/citizen_clothes/hat/baseball_cap/baseball_cap.clothing",
				"models/citizen_clothes/hair/hair_shortscruffy/hair_shortscruffy_brown.clothing",
				"models/citizen_clothes/hair/hair_shortscruffy/hair_shortscruffy_grey.clothing",
				""
			} );
		if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }

		if ( Rand.Int( 4 ) == 1 )
		{
			model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/hair/moustache/moustache_brown.clothing",
				"models/citizen_clothes/hair/moustache/moustache_grey.clothing",
				"models/citizen_clothes/hair/scruffy_beard/scruffy_beard_brown.clothing",
				"models/citizen_clothes/hair/scruffy_beard/scruffy_beard_grey.clothing",
				"models/citizen_clothes/hair/stubble/stubble.clothing"
			} );
			if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }
		}

		if ( Rand.Int( 1 ) == 1 )
		{
			model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/hair/eyebrows/eyebrows.clothing"
			} );
			if ( ResourceLibrary.TryGet<Clothing>( model, out item ) ) { Clothing.Clothing.Add( item ); }
		}
	}
}
