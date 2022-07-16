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

	public void CreateAvatar()
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

		foreach ( SceneModel model in AvatarWorld.SceneObjects.OfType<SceneModel>() )
		{
			model.Update( RealTime.Delta );
		}

		AvatarScene = Add.ScenePanel( AvatarWorld, Vector3.Zero, Rotation.Identity, 35, "avatar");

		AvatarScene.CameraPosition = pos;
		AvatarScene.CameraRotation = Rotation.From( angles );
		AvatarScene.AmbientColor = Color.Gray * 0.2f;
		AvatarScene.RenderOnce = true;

		var randpos1 = Vector3.Up * (100.0f + Rand.Float( 20 )) + Vector3.Forward * (-100.0f + Rand.Float( 30 )) + Vector3.Right * (100 + Rand.Float( 20 ));
		var light1  = new SceneSpotLight( AvatarWorld, randpos1, new Color( 0.05f + Rand.Float( .1f ), 0.05f + Rand.Float( .1f ), .1f + Rand.Float( .1f ) ) * (80.0f + Rand.Float( 10 )) );
		light1.Rotation = Rotation.LookAt( -light1.Position );
		light1.SpotCone = new SpotLightCone { Inner = 90, Outer = 90 };

		var randpos2 = Vector3.Up * (50.0f + Rand.Float( 100 )) + Vector3.Forward * (100.0f + Rand.Float( 20 )) + Vector3.Right * (-200 + Rand.Float(150));
		var light2 = new SceneSpotLight( AvatarWorld, randpos2, new Color( .8f + Rand.Float(.2f), 0.8f + +Rand.Float( .2f ), 0.8f + Rand.Float( .2f ) ) * (80.0f + Rand.Float(10)) );
		light2.Rotation = Rotation.LookAt( -light2.Position );
		light2.SpotCone = new SpotLightCone { Inner = 90, Outer = 90 };
	}

	public void TickAvatar()
	{
		var ply = Local.Pawn as HumanPlayer;
		if(ply.LifeState == LifeState.Alive )
		{
			AvatarScene.Style.FilterSepia = 0;
			AvatarScene.Style.FilterSaturate = 1;
			AvatarScene.Style.FilterTint = Color.White;
		}
		else if ( ply.LifeState == LifeState.Dying )
		{
			AvatarScene.Style.FilterSepia = 0;
			AvatarScene.Style.FilterSaturate = 1;
			AvatarScene.Style.FilterTint = Color.Parse( "#EB3F3F" );
		}
		else if ( ply.LifeState == LifeState.Dead )
		{
			AvatarScene.Style.FilterSepia = 1;
			AvatarScene.Style.FilterSaturate = 0;
			AvatarScene.Style.FilterTint = Color.White;
		}
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
	}

	[ClientRpc]
	public static void RefreshAvatar()
	{
		Current.CreateAvatar();
	}
}

