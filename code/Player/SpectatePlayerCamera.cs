using Sandbox;
using Sandbox.UI;
using System.Runtime.InteropServices;

namespace ZombieHorde;

public class SpectatePlayerCamera : CameraMode
{
	Vector3 FocusPoint;
	Rotation FocusRotation;
	public HumanPlayer SpectateTarget;

	public override void Activated()
	{
		base.Activated();

		FocusPoint = CurrentView.Position;
		FieldOfView = CurrentView.FieldOfView;
	}

	public override void Update()
	{
		if ( SpectateTarget == null ) FindNewTarget();
		if( SpectateTarget is HumanPlayer ply )
		{
			if ( ply.LifeState == LifeState.Dead ) FindNewTarget();
			if ( Local.Pawn is Player localPly )
			{
				localPly.Health = SpectateTarget.Health; // why doesn't this update the death postprocessing??
				//Log.Info( $"{Host.Name}, {localPly.Health}" );
			}
		}

		if ( Input.Pressed( InputButton.PrimaryAttack ) || Input.Pressed( InputButton.SecondaryAttack ) )
		{
			// find another random player, don't do it in order lol
			FindNewTarget();
		}

		var player = Local.Client; // this was here by default. do I need this?
		if ( player == null ) return;

		// lerp the focus point
		FocusPoint = Vector3.Lerp( FocusPoint, SpectateTarget.Position, Time.Delta * 10.0f );
		//FocusRotation = Rotation.Lerp( FocusRotation, SpectateTarget.EyeRotation, Time.Delta * 5.0f );
		FocusRotation = Input.Rotation;


		var ViewOffset = FocusRotation.Forward * (-150 * 1) + FocusRotation.Right * 15 + Vector3.Up * (52 * 1);
		Position = FocusPoint + ViewOffset;
		Rotation = FocusRotation;
		FocusPoint = Vector3.Lerp( FocusPoint, SpectateTarget.Position, Time.Delta * 10.0f );
		FieldOfView = FieldOfView.LerpTo( 50, Time.Delta * 3.0f );

		Viewer = null;
	}

	public void FindNewTarget()
	{
		foreach(var targ in Entity.All.OfType<HumanPlayer>().OrderBy( x => Guid.NewGuid() ).ToList() )
		{
			if( targ.LifeState != LifeState.Dead )
			{
				if(SpectateTarget != targ )
				{
					SpectateTarget = targ;
					break;
				}
			}
		}

		if ( SpectateTarget == null ) SpectateTarget = Local.Pawn as HumanPlayer;
	}
}

