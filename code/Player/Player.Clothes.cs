namespace ZombieHorde;
public partial class HumanPlayer
{
	public ClothingContainer Clothing { get; protected set; }

	/// <summary>
	/// Set the clothes to whatever the player is wearing
	/// </summary>
	public void UpdateClothes( Client cl )
	{
		Clothing ??= new();
		if ( cl.IsBot )
		{
			RandomizeClothes();
		}
		else
		{
			Clothing.LoadFromClient( cl );
			if ( IsClient )
			{
				Clothing.Deserialize( ConsoleSystem.GetValue( "avatar" ) );
			}
		}
	}

	public void RandomizeClothes()
	{

		Clothing ??= new();
		Clothing item;
		String model;

		model = Rand.FromArray( new[]
			{
				"models/citizen_clothes/skin01.clothing",
				"models/citizen_clothes/skin02.clothing",
				"models/citizen_clothes/skin03.clothing",
				"models/citizen_clothes/skin04.clothing",
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
