namespace ZombieHorde;
partial class ZomViewModel : BaseViewModel
{
	private float WalkBob = 0;
	private Rotation MeleeRotationOffset, MeleeRotationTarget = Rotation.Identity;
	private Transform ModelOffset, OffsetTarget = Transform.Zero;
	private float MeleeRotationLerpSpeed = .2f;
	private bool IsMeleeShoving = false;
	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		// camSetup.ViewModelFieldOfView = camSetup.FieldOfView + (FieldOfView - 80);

		AddCameraEffects( ref camSetup );

		// rotation
		MeleeRotationOffset = Rotation.Lerp( MeleeRotationOffset, MeleeRotationTarget, MeleeRotationLerpSpeed );


		// position
		var speed = Owner.Velocity.Length.LerpInverse( 0, 400 );
		var left = camSetup.Rotation.Left;
		var up = camSetup.Rotation.Up;

		OffsetTarget.Position = Vector3.Zero;
		OffsetTarget.Position += up * MathF.Sin( WalkBob ) * speed * -3;
		OffsetTarget.Position += left * MathF.Sin( WalkBob * 0.5f ) * speed * -2f;

		if( IsMeleeShoving )
		{
			// if I vibrate the model maybe people won't notice how simple the animation is? 
			OffsetTarget.Position += up * MathF.Sin( Time.Delta * 1000f ) * -8;
			OffsetTarget.Position += left * MathF.Sin( Time.Delta * 1000f ) * -6f;
		}

		if ( Owner.GroundEntity == null )
		{
			var maxDist = 5;
			OffsetTarget.Position += (Owner.Velocity * -.01f).Clamp( new Vector3( -maxDist, -maxDist, -maxDist ), new Vector3( maxDist, maxDist, maxDist ) );
			OffsetTarget.Position += up * MathF.Sin( MathF.Sin( Time.Delta * 50.0f * speed ) ) * speed * -3;
			OffsetTarget.Position += left * MathF.Sin( MathF.Sin( Time.Delta * 50.0f * speed ) ) * speed * -2f;
		}

		
		if ( ((Owner as HumanPlayer).Controller as HumanWalkController).Duck.IsActive ) // big chonker to check if player is ducking
		{
			var maxSwayAngle = 2;
			OffsetTarget.Rotation = Rotation.FromYaw( Math.Clamp( Input.MouseDelta.x * -.5f, -maxSwayAngle, maxSwayAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .5f, -maxSwayAngle, maxSwayAngle ) );

			OffsetTarget.Rotation += Rotation.FromRoll( -30 );
			OffsetTarget.Rotation += Rotation.FromYaw( -5 );
			OffsetTarget.Position += up * -3f;
			OffsetTarget.Position += left * 2f;
			OffsetTarget.Position += camSetup.Rotation.Backward * 5f;
		}
		else
		{
			var maxSwayAngle = 5;
			OffsetTarget.Rotation = Rotation.FromYaw( Math.Clamp( Input.MouseDelta.x * -.5f, -maxSwayAngle, maxSwayAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .5f, -maxSwayAngle, maxSwayAngle ) );
		}
		

		ModelOffset.Position = Vector3.Lerp( ModelOffset.Position, OffsetTarget.Position, .05f );
		ModelOffset.Rotation = Rotation.Lerp( ModelOffset.Rotation, OffsetTarget.Rotation, .05f );

		// finally set it
		Position = camSetup.Position + ModelOffset.Position;
		Rotation = camSetup.Rotation * Rotation.FromPitch(5) * MeleeRotationOffset * ModelOffset.Rotation;

	}

	public async void PlayMeleeAnimation()
	{
		IsMeleeShoving = true;
		MeleeRotationTarget = Rotation.FromPitch( 1 ) * Rotation.FromYaw( 40 ) * Rotation.FromRoll( -10 );
		MeleeRotationLerpSpeed = .3f;
		await Task.Delay( 180 );
		MeleeRotationTarget = Rotation.Identity;
		MeleeRotationLerpSpeed = .1f;
		IsMeleeShoving = false;
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
			WalkBob += Time.Delta * 25.0f * speed;
		}

		Position += up * MathF.Sin( WalkBob ) * speed * -1;
		Position += left * MathF.Sin( WalkBob * 0.5f ) * speed * -0.5f;

		var uitx = new Sandbox.UI.PanelTransform();
		uitx.AddTranslateY( MathF.Sin( WalkBob * 1.0f ) * speed * -4.0f );
		uitx.AddTranslateX( MathF.Sin( WalkBob * 0.5f ) * speed * -3.0f );

		HudRootPanel.Current.Style.Transform = uitx;
	}
}
