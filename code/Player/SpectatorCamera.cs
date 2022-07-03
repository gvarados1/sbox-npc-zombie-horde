using Sandbox.UI;

namespace ZombieHorde
{
	public class SpectatorCamera : CameraMode
	{
		Angles LookAngles;
		Vector3 MoveInput;

		Vector3 TargetPos;
		Rotation TargetRot;

		bool PivotEnabled;
		Vector3 PivotPos;
		float PivotDist;

		float MoveSpeed;
		float BaseMoveSpeed = 300.0f;
		float FovOverride = 0;

		float LerpMode = 0;

		/// <summary>
		/// On the camera becoming activated, snap to the current view position
		/// </summary>
		public override void Activated()
		{
			base.Activated();

			TargetPos = CurrentView.Position;
			TargetRot = CurrentView.Rotation;

			Position = TargetPos;
			Rotation = TargetRot;
			LookAngles = Rotation.Angles();
			FovOverride = 80;
		}

		public override void Deactivated()
		{
			base.Deactivated();
		}

		public override void Update()
		{
			var player = Local.Client;
			if ( player == null ) return;

			FieldOfView = FovOverride;

			Viewer = null;

			if ( PivotEnabled )
			{
				PivotMove();
			}
			else
			{
				FreeMove();
			}
		}

		public override void BuildInput( InputBuilder input )
		{
			MoveInput = input.AnalogMove;

			MoveSpeed = 1;
			if ( input.Down( InputButton.Run ) ) MoveSpeed = 5;
			if ( input.Down( InputButton.Duck ) ) MoveSpeed = 0.2f;

			if ( input.Down( InputButton.Slot1 ) ) LerpMode = 0.0f;
			if ( input.Down( InputButton.Slot2 ) ) LerpMode = 0.5f;
			if ( input.Down( InputButton.Slot3 ) ) LerpMode = 0.9f;

			if ( input.Pressed( InputButton.Walk ) )
			{
				var tr = Trace.Ray( Position, Position + Rotation.Forward * 4096 ).Run();

				if ( tr.Hit )
				{
					PivotPos = tr.EndPosition;
					PivotDist = Vector3.DistanceBetween( tr.EndPosition, Position );
					PivotEnabled = true;
				}
			}

			if ( input.Down( InputButton.SecondaryAttack ) )
			{
				FovOverride += input.AnalogLook.pitch * (FovOverride / 30.0f);
				FovOverride = FovOverride.Clamp( 5, 150 );
				input.AnalogLook = default;
			}

			LookAngles += input.AnalogLook * (FovOverride / 80.0f);
			LookAngles.roll = 0;

			PivotEnabled = PivotEnabled && input.Down( InputButton.Walk );

			if ( PivotEnabled )
			{
				MoveInput.x += input.MouseWheel * 10.0f;
			}
			else
			{
				BaseMoveSpeed += input.MouseWheel * 10.0f;
				BaseMoveSpeed = BaseMoveSpeed.Clamp( 10, 1000 );
			}

			input.Clear();
			input.StopProcessing = true;
		}

		void FreeMove()
		{
			var mv = MoveInput.Normal * BaseMoveSpeed * RealTime.Delta * Rotation * MoveSpeed;

			TargetRot = Rotation.From( LookAngles );
			TargetPos += mv;

			Position = Vector3.Lerp( Position, TargetPos, 10 * RealTime.Delta * (1 - LerpMode) );
			Rotation = Rotation.Slerp( Rotation, TargetRot, 10 * RealTime.Delta * (1 - LerpMode) );
		}

		void PivotMove()
		{
			PivotDist -= MoveInput.x * RealTime.Delta * 100 * (PivotDist / 50);
			PivotDist = PivotDist.Clamp( 1, 1000 );

			TargetRot = Rotation.From( LookAngles );
			Rotation = Rotation.Slerp( Rotation, TargetRot, 10 * RealTime.Delta * (1 - LerpMode) );

			TargetPos = PivotPos + Rotation.Forward * -PivotDist;
			Position = TargetPos;
		}
	}
}
