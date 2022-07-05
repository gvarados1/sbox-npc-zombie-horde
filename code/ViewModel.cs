namespace ZombieHorde;
partial class ZomViewModel : BaseViewModel
{
	float walkBob = 0;
	private Rotation MeleeRotationOffset, MeleeRotationTarget = Rotation.Identity;
	private Rotation RotationOffset, RotationTarget = Rotation.Identity;
	private float MeleeRotationLerpSpeed = .2f;
	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		// camSetup.ViewModelFieldOfView = camSetup.FieldOfView + (FieldOfView - 80);

		AddCameraEffects( ref camSetup );

		MeleeRotationOffset = Rotation.Lerp( MeleeRotationOffset, MeleeRotationTarget, MeleeRotationLerpSpeed );

		var maxAngle = 5;
		RotationTarget = Rotation.FromYaw( Math.Clamp(Input.MouseDelta.x * -.5f, -maxAngle, maxAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .5f, -maxAngle, maxAngle ) );
		RotationOffset = Rotation.Lerp( RotationOffset, RotationTarget, .05f );

		Position = camSetup.Position;
		Rotation = camSetup.Rotation * Rotation.FromPitch(5) * MeleeRotationOffset * RotationOffset;

	}

	public async void PlayMeleeAnimation()
	{
		MeleeRotationTarget = Rotation.FromPitch( 1 ) * Rotation.FromYaw( 40 ) * Rotation.FromRoll( -10 );
		MeleeRotationLerpSpeed = .3f;
		await Task.Delay( 180 );
		MeleeRotationTarget = Rotation.Identity;
		MeleeRotationLerpSpeed = .1f;
	}

	private void AddCameraEffects( ref CameraSetup camSetup )
	{
		//Rotation = Local.Pawn.EyeRotation;

		if ( Local.Pawn.LifeState == LifeState.Dead )
			return;


		//
		// Bob up and down based on our walk movement
		//
		var speed = Owner.Velocity.Length.LerpInverse( 0, 400 );
		var left = camSetup.Rotation.Left;
		var up = camSetup.Rotation.Up;

		if ( Owner.GroundEntity != null )
		{
			walkBob += Time.Delta * 25.0f * speed;
		}

		Position += up * MathF.Sin( walkBob ) * speed * -1;
		Position += left * MathF.Sin( walkBob * 0.5f ) * speed * -0.5f;

		var uitx = new Sandbox.UI.PanelTransform();
		uitx.AddTranslateY( MathF.Sin( walkBob * 1.0f ) * speed * -4.0f );
		uitx.AddTranslateX( MathF.Sin( walkBob * 0.5f ) * speed * -3.0f );

		HudRootPanel.Current.Style.Transform = uitx;
	}
}
