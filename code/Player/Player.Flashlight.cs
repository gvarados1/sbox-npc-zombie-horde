using Sandbox.Internal;

namespace ZombieHorde;
public partial class HumanPlayer
{
	private SpotLightEntity WorldLight;
	private SpotLightEntity ViewLight;
	private Particles LightParticle;

	[Net, Predicted]
	private bool FlashlightEnabled { get; set; } = false;

	[Net, Predicted]
	TimeSince TimeSinceLightToggled { get; set; }

	private void TickFlashlight()
	{
		if ( Input.Released( InputButton.Flashlight ) && TimeSinceLightToggled > 0.1f )
		{
			FlashlightEnabled = !FlashlightEnabled;

			PlaySound( FlashlightEnabled ? "flashlight-on" : "flashlight-off" );

			if ( IsServer )
			{
				if ( !WorldLight.IsValid() )
				{
					WorldLight = CreateLight();
					WorldLight.Transform = Transform;
					WorldLight.EnableHideInFirstPerson = true;
				}
				WorldLight.Enabled = FlashlightEnabled;
				if(LightParticle != null)
					LightParticle.SetPosition( 3, new Vector3( !FlashlightEnabled ? 0 : 1, 1, 0 ) );
			}

			if ( IsClient )
			{
				if ( !ViewLight.IsValid() )
				{
					var lightOffset = Vector3.Forward * 10;
					ViewLight = CreateLight();
					ViewLight.Transform = Transform;
					ViewLight.EnableViewmodelRendering = true;
				}
				ViewLight.Enabled = FlashlightEnabled;
			}

			

			TimeSinceLightToggled = 0;
		}

		if ( FlashlightEnabled )
		{
			var forward = EyeRotation.Forward;

			// let's assume if the worldmodel has a muzzle, the viewmodel also has one.
			if(ActiveChild is BaseZomWeapon gun )
			{
				var worldTrans = gun.GetAttachment( "muzzle" );
				if ( worldTrans != null )
				{

					if ( IsClient )
					{
						var viewTrans = gun.ViewModelEntity?.GetAttachment( "muzzle" );
						if ( viewTrans != null )
						{
							if ( !ViewLight.IsValid() )
							{
								var lightOffset = Vector3.Forward * 10;
								ViewLight = CreateLight();
								ViewLight.Transform = Transform;
								ViewLight.EnableViewmodelRendering = true;
							}
							ViewLight.Enabled = FlashlightEnabled;

							ViewLight.SetParent( null );
							ViewLight.Rotation = (Rotation)viewTrans?.Rotation;
							ViewLight.Position = (Vector3)viewTrans?.Position;
							ViewLight.SetParent( gun.ViewModelEntity, "muzzle" );
						}
					}

					if ( IsServer )
					{
						if ( !WorldLight.IsValid() )
						{
							WorldLight = CreateLight();
							WorldLight.Transform = Transform;
							WorldLight.EnableHideInFirstPerson = true;
						}
						WorldLight.Enabled = FlashlightEnabled;

						WorldLight.SetParent( null );
						WorldLight.Rotation = (Rotation)worldTrans?.Rotation;
						WorldLight.Position = (Vector3)worldTrans?.Position;
						WorldLight.SetParent( gun, "muzzle" );

						// lese flare particle
						if ( LightParticle == null )
						{
							LightParticle = Particles.Create( "particles/flashlight/flashlight.vpcf", gun, "muzzle" );
							LightParticle.SetPosition( 2, new Color( 0.9f, 0.87f, 0.6f ) );
						}
						//LightParticle.SetPosition( 3, new Vector3( shouldTurnOff ? 0 : 1, 1, 0 ) );
						//LightParticle.SetPosition( 3, new Vector3( 1, 1, 0 ) );
					}
					return;
				}
			}

			// the lights don't look good if I don't constantly set this?
			if ( IsClient )
			{
				ViewLight.SetParent( null );
				ViewLight.Rotation = EyeRotation;
				ViewLight.Position = EyePosition + forward * 1f;
				ViewLight.SetParent( this, "eyes" );
			}
			else
			{
				WorldLight.SetParent( null );
				WorldLight.Rotation = EyeRotation;
				WorldLight.Position = EyePosition + forward * 20f;
				WorldLight.SetParent( this, "eyes" );
			}
		}
		else
		{
			// I need to constantly set this or the flashlight will bug out on high ping.
			if(WorldLight.IsValid())
				WorldLight.Enabled = FlashlightEnabled;
			if(ViewLight.IsValid())
				ViewLight.Enabled = FlashlightEnabled;
		}
	}

	private SpotLightEntity CreateLight()
	{
		var light = new SpotLightEntity
		{
			Enabled = true,
			DynamicShadows = true,
			Range = 720,
			Falloff = 1.0f,
			LinearAttenuation = 0.0f,
			QuadraticAttenuation = 1.0f,
			Brightness = 1.5f,
			//Color = Color.White,
			Color = (Color)Color.Parse( "#EDEAD2" ),
			InnerConeAngle = 10,
			OuterConeAngle = 40,
			FogStrength = 2,
			Owner = this,
			LightCookie = Texture.Load( "materials/effects/lightcookie.vtex" )
		};
		light.UseFogNoShadows();

		return light;
	}
}
