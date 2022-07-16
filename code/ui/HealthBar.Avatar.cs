using Sandbox.Internal;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.ComponentModel;

namespace ZombieHorde;
public partial class HealthBar
{
	public ScenePanel AvatarScene { get; set; }
	public SceneWorld AvatarWorld { get; set; }

	private SceneModel CitizenModel;
	private List<SceneModel> ClothingObjects = new();
	public ClothingContainer ClothingContainer = new();

	void CreateAvatar()
	{
		ClothingObjects?.Clear();
		AvatarScene?.Delete();
		AvatarScene = null;

		AvatarWorld?.Delete();
		AvatarWorld = new SceneWorld();

		CitizenModel = new SceneModel( AvatarWorld, "models/citizen/citizen.vmdl", Transform.Zero );
		DressModel();

		Angles angles = new( -2 + Rand.Float(4), 170 + Rand.Float(20), -1 + Rand.Float(2) );
		Vector3 pos = Vector3.Up * 63 + Vector3.Left * 2 + angles.Direction * -40;

		CitizenModel.SetAnimGraph( "models/zombie/citizen_zombie/citizen_avatar.vanmgrph" );
		CitizenModel.Update( RealTime.Delta );

		AvatarScene = Add.ScenePanel( AvatarWorld, Vector3.Zero, Rotation.Identity, 35, "avatar");

		AvatarScene.CameraPosition = pos;
		AvatarScene.CameraRotation = Rotation.From( angles );
		AvatarScene.AmbientColor = Color.Gray * 0.2f;
		AvatarScene.RenderOnce = true;

		var light1  = new SceneSpotLight( AvatarWorld, Vector3.Up * 100.0f + Vector3.Forward * -100.0f + Vector3.Right * 100, new Color( 0.1f, 0.1f, .2f ) * 80.0f );
		light1.Rotation = Rotation.LookAt( -light1.Position );
		light1.SpotCone = new SpotLightCone { Inner = 90, Outer = 90 };

		var light2 = new SceneSpotLight( AvatarWorld, Vector3.Up * 100.0f + Vector3.Forward * 100.0f + Vector3.Right * -200, new Color( 1.0f, 0.95f, 0.8f ) * 80.0f );
		light2.Rotation = Rotation.LookAt( -light2.Position );
		light2.SpotCone = new SpotLightCone { Inner = 90, Outer = 90 };
	}

	public override void OnHotloaded()
	{
		base.OnHotloaded();

		CreateAvatar();
	}

	[Event( "avatar.changed" )]
	void DressModel()
	{
		ClothingContainer ??= new();
		//ClothingContainer.LoadFromClient( Local.Client );

		ClothingContainer.Deserialize( ConsoleSystem.GetValue( "avatar" ) );

		foreach ( var model in ClothingObjects )
		{
			model?.Delete();
		}

		ClothingObjects = ClothingContainer.DressSceneObject( CitizenModel );
		Log.Info(ClothingObjects.Count );
		foreach ( var model in ClothingObjects )
		{
			Log.Info(model?.ToString());
		}
	}
}

