namespace ZombieHorde;

public partial class HumanWalkController : BaseZomWalkController
{
	public HumanWalkController()
	{
		WalkSpeed = 240;
		SprintSpeed = 140;
		DefaultSpeed = 240;
		AirAcceleration = 10;
	}

	public override float GetWishSpeed()
	{
		var speedMultiplier = 1f;
		if ( Pawn.Health < 21 )
			speedMultiplier = .75f;

		var ws = Duck.GetWishSpeed();
		if ( ws >= 0 ) return ws;

		if ( Input.Down( InputButton.Run ) ) return SprintSpeed * speedMultiplier;
		if ( Input.Down( InputButton.Walk ) ) return WalkSpeed * speedMultiplier;

		return DefaultSpeed * speedMultiplier;
	}
}

