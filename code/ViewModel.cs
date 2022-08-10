namespace ZombieHorde;
partial class ZomViewModel : BaseViewModel
{
	private float WalkBob = 0;
	private Transform MeleeOffset, MeleeTarget = Transform.Zero;
	private Transform ModelOffset, OffsetTarget = Transform.Zero;
	private float MeleeRotationLerpSpeed = .2f;
	private bool IsMeleeShoving = false;
	private bool WasSprinting = false;
	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		// camSetup.ViewModelFieldOfView = camSetup.FieldOfView + (FieldOfView - 80);

		AddCameraEffects( ref camSetup );

		var wep = (Owner as Player).ActiveChild as BaseZomWeapon;
		var isReloading = wep.IsReloading;
		var isDucking = Owner.LifeState == LifeState.Alive && ((Owner as HumanPlayer).Controller is BaseZomWalkController a) && a.Duck.IsActive;
		var isSprinting = Owner.LifeState == LifeState.Alive && ((Owner as HumanPlayer).Controller is BaseZomWalkController b) && b.IsSprinting;

		// rotation
		MeleeOffset.Rotation = Rotation.Lerp( MeleeOffset.Rotation, MeleeTarget.Rotation, MeleeRotationLerpSpeed * Time.Delta * 120 );
		MeleeOffset.Position = Vector3.Lerp( MeleeOffset.Position, MeleeTarget.Position, MeleeRotationLerpSpeed * Time.Delta * 120 );


		// position
		var speed = Owner.Velocity.Length.LerpInverse( 0, 500 );
		var left = camSetup.Rotation.Left;
		var up = camSetup.Rotation.Up;

		OffsetTarget.Rotation = wep.ViewModelOffset.Rotation;
		OffsetTarget.Position = wep.ViewModelOffset.Position * camSetup.Rotation;
		OffsetTarget.Position += up * MathF.Sin( WalkBob ) * speed * -2;
		OffsetTarget.Position += left * MathF.Sin( WalkBob * 0.5f ) * speed * -1.5f;

		if( IsMeleeShoving )
		{
			// if I vibrate the model maybe people won't notice how simple the animation is? 
			OffsetTarget.Position += up * MathF.Sin( Time.Delta * 20f ) * -4;
			OffsetTarget.Position += left * MathF.Sin( Time.Delta * 20f ) * -3f;
		}

		if ( Owner.GroundEntity == null )
		{
			var maxDist = 1.5f;
			OffsetTarget.Position += (Owner.Velocity * -.01f).Clamp( new Vector3( -maxDist, -maxDist, -maxDist ), new Vector3( maxDist, maxDist, maxDist ) );
			OffsetTarget.Position += up * MathF.Sin( MathF.Sin( Time.Delta * 50.0f * speed ) ) * speed * -2.5f;
			OffsetTarget.Position += left * MathF.Sin( MathF.Sin( Time.Delta * 50.0f * speed ) ) * speed * -2f;
		}


		if ( isDucking && !isReloading && !IsMeleeShoving ) // big chonker to check if player is ducking
		{
			//* // test position
			var maxSwayAngle = 2;
			OffsetTarget.Rotation += Rotation.FromYaw( Math.Clamp( Input.MouseDelta.x * -.5f, -maxSwayAngle, maxSwayAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .5f, -maxSwayAngle, maxSwayAngle ) );

			OffsetTarget.Rotation += Rotation.FromRoll( -200 );
			OffsetTarget.Rotation += Rotation.FromYaw( -1 );
			OffsetTarget.Position += up * -4f;
			OffsetTarget.Position += left * 4f;
			OffsetTarget.Position += camSetup.Rotation.Forward * 2f;
			OffsetTarget.Position += camSetup.Rotation * wep.ViewModelOffsetDuck.Position;
			OffsetTarget.Rotation += wep.ViewModelOffsetDuck.Rotation;

			// */
			/*
			var maxSwayAngle = 2;
			OffsetTarget.Rotation = Rotation.FromYaw( Math.Clamp( Input.MouseDelta.x * -.5f, -maxSwayAngle, maxSwayAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .5f, -maxSwayAngle, maxSwayAngle ) );

			OffsetTarget.Rotation += Rotation.FromRoll( -50 );
			OffsetTarget.Rotation += Rotation.FromYaw( -5 );
			OffsetTarget.Position += up * -1f;
			OffsetTarget.Position += left * 2f;
			OffsetTarget.Position += camSetup.Rotation.Backward * 1f;

			// old duck weapon position. works with rust weapons.
			/*
			var maxSwayAngle = 2;
			OffsetTarget.Rotation = Rotation.FromYaw( Math.Clamp( Input.MouseDelta.x * -.5f, -maxSwayAngle, maxSwayAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .5f, -maxSwayAngle, maxSwayAngle ) );

			OffsetTarget.Rotation += Rotation.FromRoll( -30 );
			OffsetTarget.Rotation += Rotation.FromYaw( -5 );
			OffsetTarget.Position += up * -3f;
			OffsetTarget.Position += left * 2f;
			OffsetTarget.Position += camSetup.Rotation.Backward * 5f;
			*/
		}
		else if( isSprinting && !isReloading && !IsMeleeShoving )
		//else if( true )
		{
			if ( wep.UseAlternativeSprintAnimation )
			{
				var maxSwayAngle = 4;
				OffsetTarget.Rotation += Rotation.FromYaw( Math.Clamp( Input.MouseDelta.x * -.5f, -maxSwayAngle, maxSwayAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .5f, -maxSwayAngle, maxSwayAngle ) );

				OffsetTarget.Rotation += Rotation.FromPitch( 10 );
				OffsetTarget.Rotation += Rotation.FromRoll( 10 );
				OffsetTarget.Rotation += Rotation.FromYaw( -5 );
				OffsetTarget.Position += up * -.5f;
				OffsetTarget.Position += left * -.5f;
				OffsetTarget.Position += camSetup.Rotation.Forward * 1f;
			}
			else
			{
				var maxSwayAngle = 4;
				OffsetTarget.Rotation += Rotation.FromYaw( Math.Clamp( Input.MouseDelta.x * -.5f, -maxSwayAngle, maxSwayAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .5f, -maxSwayAngle, maxSwayAngle ) );

				OffsetTarget.Rotation += Rotation.FromPitch( 20 );
				OffsetTarget.Rotation += Rotation.FromRoll( -20 );
				OffsetTarget.Rotation += Rotation.FromYaw( 160 );
				OffsetTarget.Position += up * 0f;
				OffsetTarget.Position += left * -3.5f;
				OffsetTarget.Position += camSetup.Rotation.Forward * 1f;
			}
		}
		else
		{
			var maxSwayAngle = 3;
			OffsetTarget.Rotation += Rotation.FromYaw( Math.Clamp( Input.MouseDelta.x * -.25f, -maxSwayAngle, maxSwayAngle ) ) * Rotation.FromPitch( Math.Clamp( Input.MouseDelta.y * .25f, -maxSwayAngle, maxSwayAngle ) );
		}

		//ModelOffset.Position = Vector3.Lerp( ModelOffset.Position, OffsetTarget.Position, .05f * Time.Delta * 100 );
		//ModelOffset.Rotation = Rotation.Lerp( ModelOffset.Rotation, OffsetTarget.Rotation, .05f * Time.Delta * 100 );

		var disableLerp = false;
		// don't lerp position if we just attacked and were sprinting
		if ( wep.TimeSincePrimaryAttack < .25f && WasSprinting )
			disableLerp = true;

		if ( disableLerp )
		{
			ModelOffset.Position = OffsetTarget.Position;
			ModelOffset.Rotation = OffsetTarget.Rotation;
		}
		else
		{
			ModelOffset.Position = Vector3.Lerp( ModelOffset.Position, OffsetTarget.Position, 7f * Time.Delta );
			ModelOffset.Rotation = Rotation.Lerp( ModelOffset.Rotation, OffsetTarget.Rotation, 7f * Time.Delta );
		}

		// finally set it
		Position = camSetup.Position + ModelOffset.Position + MeleeOffset.Position;
		//Rotation = camSetup.Rotation * Rotation.FromPitch(5) * MeleeOffset.Rotation * ModelOffset.Rotation;
		Rotation = camSetup.Rotation * Rotation.FromPitch(1) * MeleeOffset.Rotation * ModelOffset.Rotation;

		//camSetup.ViewModel.FieldOfView = 60;
		camSetup.ViewModel.FieldOfView = 50;
		WasSprinting = isSprinting;
	}

	public async void PlayMeleeAnimation()
	{
		IsMeleeShoving = true;
		MeleeTarget.Rotation = Rotation.FromPitch( 1 ) * Rotation.FromYaw( 30 ) * Rotation.FromRoll( -15 );
		MeleeTarget.Position = Vector3.Up * -2 * MeleeTarget.Rotation;
		MeleeRotationLerpSpeed = .3f;
		await Task.Delay( 180 );
		MeleeTarget.Rotation = Rotation.Identity;
		MeleeTarget.Position = Vector3.Zero;
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
