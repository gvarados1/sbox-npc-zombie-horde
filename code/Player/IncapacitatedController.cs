namespace ZombieHorde;

public partial class IncapacitatedController : WalkController
	{
	public IncapacitatedController()
	{
		// lol
		WalkSpeed = 0;
		SprintSpeed = 0;
		DefaultSpeed = 0;
		AirAcceleration = 0;
	}

	public override void Simulate()
	{
		base.Simulate();
		EyeLocalPosition *= .5f;
	}

	public override float GetWishSpeed()
	{
		return 0;
	}

	public override void CheckJumpButton()
	{
		// nothing
	}
}
