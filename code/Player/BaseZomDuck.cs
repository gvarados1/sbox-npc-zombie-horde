
using static Sandbox.Package;
using ZombieHorde;

namespace Sandbox
{
	[Library]
	public class BaseZomDuck : BaseNetworkable
	{
		[ConVar.Replicated]
		public static bool zom_toggleduck { get; set; } = false;

		public BasePlayerController Controller;

		public bool IsActive; // replicate
		public bool Wants = false;

		public BaseZomDuck( BasePlayerController controller )
		{
			Controller = controller;
		}

		public virtual void PreTick() 
		{
			if ( zom_toggleduck )
			{
				if ( Input.Pressed( InputButton.Duck ) )
					Wants = !Wants;
			}
			else
			{
				Wants = Input.Down( InputButton.Duck );
			}

			if ( Wants != IsActive ) 
			{
				if ( Wants ) TryDuck();
				else TryUnDuck();
			}

			if ( IsActive )
			{
				Controller.SetTag( "ducked" );
				// setting this in the controller now.
				//Controller.EyeLocalPosition *= 0.6f;
			}
		}

		protected virtual void TryDuck()
		{
			Sound.FromWorld( "player.crouch", Controller.Position );
			IsActive = true;

			// viewpunch when ducking
			Rand.SetSeed( Time.Tick );

			if ( Host.IsServer && (Controller as BaseZomWalkController).Pawn is HumanPlayer ply )
				ply.ViewPunch( Rand.Float( .1f ) + .4f, Rand.Float( .3f ) - .15f );
			if ( Host.IsClient && Local.Pawn is HumanPlayer ply1 )
				ply1.ViewPunch( Rand.Float( .1f ) + .4f, Rand.Float( .3f ) - .15f );
		}

		protected virtual void TryUnDuck()
		{
			if ( (Controller as BaseZomWalkController).TimeSinceClimb < Time.Delta * 2 ) return;

			var pm = Controller.TraceBBox( Controller.Position, Controller.Position, originalMins, originalMaxs );
			if ( pm.StartedSolid ) return;

			Sound.FromWorld( "player.stand", Controller.Position );
			IsActive = false;

			// viewpunch when ducking
			Rand.SetSeed( Time.Tick );

			if ( Host.IsServer && (Controller as BaseZomWalkController).Pawn is HumanPlayer ply )
				ply.ViewPunch( Rand.Float( .1f ) - .4f, Rand.Float( .3f ) - .15f );
			if ( Host.IsClient && Local.Pawn is HumanPlayer ply1 )
				ply1.ViewPunch( Rand.Float( .1f ) - .4f, Rand.Float( .3f ) - .15f );
		}

		// Uck, saving off the bbox kind of sucks
		// and we should probably be changing the bbox size in PreTick
		Vector3 originalMins;
		Vector3 originalMaxs;

		public virtual void UpdateBBox( ref Vector3 mins, ref Vector3 maxs, float scale )
		{
			originalMins = mins;
			originalMaxs = maxs;

			if ( IsActive )
				//maxs = maxs.WithZ( 42 * scale ); //36 default
				maxs = maxs.WithZ( 52 * scale ); //36 default
		}

		//
		// Coudl we do this in a generic callback too?
		//
		public virtual float GetWishSpeed()
		{
			if ( !IsActive ) return -1;
			return 64.0f;
		}
	}
}
