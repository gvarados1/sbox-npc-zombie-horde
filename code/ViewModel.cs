namespace ZombieHorde;
partial class ZomViewModel : BaseViewModel
{
	float walkBob = 0;
	private Rotation RotationOffset, RotationTarget = Rotation.Identity;
	private float RotationLerpSpeed = .2f;
	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		// camSetup.ViewModelFieldOfView = camSetup.FieldOfView + (FieldOfView - 80);

		AddCameraEffects( ref camSetup );

		Position = camSetup.Position;
		Rotation = camSetup.Rotation * Rotation.FromPitch(5) * RotationOffset;
		RotationOffset = Rotation.Lerp( RotationOffset, RotationTarget, RotationLerpSpeed );

	}

	public async void PlayMeleeAnimation()
	{
		RotationTarget = Rotation.FromPitch( 1 ) * Rotation.FromYaw( 40 ) * Rotation.FromRoll( -10 );
		RotationLerpSpeed = .3f;
		await Task.Delay( 180 );
		RotationTarget = Rotation.Identity;
		RotationLerpSpeed = .1f;
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
