namespace zombies.util;

// ty alex instant gib
public static class ClothingExtensions
{
	public static void LoadRandom( this ClothingContainer clothingContainer )
	{
		var clothingItems = ResourceLibrary.GetAll<Clothing>().GroupBy( x => x.Category ).ToList();
		var randomClothingItems = clothingItems.Select( x => Rand.FromArray( x.ToArray() ) );

		foreach ( var clothingItem in randomClothingItems )
		{
			// Random chance to not fill this slot (10%)
			if ( Rand.Int( 0, 9 ) == 0 )
				continue;

			// Check if we have anything we can't wear with this
			if ( clothingContainer.Clothing.Where( x => !x.CanBeWornWith( clothingItem ) ).Any() )
				continue;

			clothingContainer.Clothing.Add( clothingItem );
		}
	}
}
