
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

		[ConVar.ClientData]
		public static string test_uservar { get; set; } = "Client User Var";

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
				Log.Info( Host.Name );
			}

			if ( Wants != IsActive ) 
			{
				if ( Wants ) TryDuck();
				else TryUnDuck();
			}
			Log.Info( $"{Host.Name}: {Wants}" );
			if ( IsActive )
			{
				Controller.SetTag( "ducked" );
				Controller.EyeLocalPosition *= 0.6f;
			}
		}

		protected virtual void TryDuck()
		{
			IsActive = true;
		}

		protected virtual void TryUnDuck()
		{
			var pm = Controller.TraceBBox( Controller.Position, Controller.Position, originalMins, originalMaxs );
			if ( pm.StartedSolid ) return;

			IsActive = false;
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
				maxs = maxs.WithZ( 42 * scale ); //36 default
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
