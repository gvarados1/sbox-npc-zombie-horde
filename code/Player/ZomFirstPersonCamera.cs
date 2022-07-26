using System;

namespace ZombieHorde;

public partial class ZomFirstPersonCamera : CameraMode
{
	Vector3 lastPos;

	public override void Activated()
	{
		var pawn = Local.Pawn;
		if ( pawn == null ) return;

		Position = pawn.EyePosition;
		Rotation = pawn.EyeRotation;

		lastPos = Position;
	}

	public override void Update()
	{
		var pawn = Local.Pawn;
		if ( pawn == null ) return;

		var eyePos = pawn.EyePosition;
		if ( eyePos.Distance( lastPos ) < 300 ) // TODO: Tweak this, or add a way to invalidate lastpos when teleporting
		{
			Position = Vector3.Lerp( eyePos.WithZ( lastPos.z ), eyePos, 20.0f * Time.Delta );
		}
		else
		{
			Position = eyePos;
		}

		Rotation = pawn.EyeRotation;

		Viewer = pawn;
		lastPos = Position;
	}

	[Net, Predicted]
	public Vector3 PunchOffset { get; set; } = Vector3.Zero;
	[Net, Predicted]
	public Vector3 PunchVelocity { get; set; } = Vector3.Zero;

	public override void BuildInput( InputBuilder input )
	{
		if ( Input.Pressed( InputButton.Menu ) )
		{
			PunchVelocity += Vector3.Up * -5;
			Log.Info( PunchVelocity.ToString() );
		}

		PunchOffset += PunchVelocity;
		PunchOffset = Vector3.Lerp( PunchOffset, Vector3.Zero, Time.Delta * 8f );
		PunchVelocity = Vector3.Lerp( PunchVelocity, Vector3.Zero, Time.Delta * 4f );

		input.ViewAngles.pitch += PunchOffset.z;

		DebugOverlay.ScreenText( PunchOffset.x.ToString(), 11 );
		DebugOverlay.ScreenText( PunchOffset.y.ToString(), 12 );
		DebugOverlay.ScreenText( PunchOffset.z.ToString(), 13 );

		base.BuildInput( input );
	}
}
