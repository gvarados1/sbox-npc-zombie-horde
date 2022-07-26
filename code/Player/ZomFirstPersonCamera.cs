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

		if(Local.Pawn is HumanPlayer ply )
		{
			Rotation *= ply.ViewPunchOffset.ToRotation();
		}

		Viewer = pawn;
		lastPos = Position;
	}
}
