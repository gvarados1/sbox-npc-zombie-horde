namespace ZombieHorde;

public partial class HumanWalkController : BaseZomWalkController
{
	public HumanWalkController()
	{
		WalkSpeed = 140;
		SprintSpeed = 240;
		DefaultSpeed = 160;
		AirAcceleration = 10;
	}

	public override float GetWishSpeed()
	{
		IsSprinting = false;
		var speedMultiplier = 1f;
		var adrenalineTime = (Pawn as HumanPlayer).TimeUntilAdrenalineExpires;

		if( adrenalineTime >= 1)
			speedMultiplier = 1.25f;
		else if ( adrenalineTime > 0 )
			speedMultiplier = 1 + adrenalineTime * .25f;
		else if ( Pawn.Health < 21 )
			speedMultiplier = .8f;

		var ws = Duck.GetWishSpeed();
		if ( ws >= 0 ) return ws;

		if ( Input.Down( InputButton.Run ) && Input.Forward > 0 )
		{
			IsSprinting = true;
			return SprintSpeed * speedMultiplier;
		}
		if ( Input.Down( InputButton.Walk ) ) return WalkSpeed * speedMultiplier;

		return DefaultSpeed * speedMultiplier;
	}
}

