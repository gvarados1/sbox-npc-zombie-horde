using System.ComponentModel.DataAnnotations;

namespace ZombieHorde;

/// <summary>
/// Gives 35 health points.
/// </summary>
[Library( "dm_healthkit" ), HammerEntity]
[EditorModel( "models/gameplay/healthkit/healthkit.vmdl" )]
[Title( "HL2 Health Kit" ), Category( "World" ), Icon( "favorite" )]
partial class HealthKit : ModelEntity
{
	public static readonly Model WorldModel = Model.Load( "models/gameplay/healthkit/healthkit.vmdl" );

	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;

		PhysicsEnabled = true;
		UsePhysicsCollision = true;

		Tags.Add( "item" );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( other is not HumanPlayer pl ) return;
		if ( pl.Health >= pl.MaxHealth ) return;

		var newhealth = pl.Health + 35;

		newhealth = newhealth.Clamp( 0, pl.MaxHealth );

		pl.Health = newhealth;

		Sound.FromWorld( "dm.item_health", Position );
		if(IsServer)
			Delete();
	}
}
