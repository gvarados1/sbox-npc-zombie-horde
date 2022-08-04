using SandboxEditor;

namespace ZombieHorde;

/// <summary>
///  Prevents Zombies/Lootboxes from spawning OR Enables spawning in direct sight of players.
/// </summary>
[Library( "zom_spawnblocker" )]
[AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
[HammerEntity, Solid, VisGroup( VisGroup.Trigger ), HideProperty( "enable_shadows" )]
[Title( "Spawn Blocker" ), Category( "Logic" ), Icon( "block" )]
public partial class HammerSpawnBlocker : ModelEntity
{
	/// <summary>
	/// Whether this entity is enabled or not.
	/// </summary>
	[Property]
	public bool Enabled { get; protected set; } = true;

	/// <summary>
	///  
	/// </summary>
	[Property, Title( "Spawn Type" )]
	public BlockType BlockType { get; set; } = BlockType.BlockSpawning;

	[Property]
	public bool AffectsCommonZombies { get; set; } = true;

	[Property]
	public bool AffectsSpecialZombies { get; set; } = true;

	[Property]
	public bool AffectsLootBoxes { get; set; } = false;

	public override void Spawn()
	{
		base.Spawn();

		Tags.Add( "trigger" );

		if (BlockType == BlockType.BlockSpawning)
			Tags.Add( "BlockSpawning" );
		else if (BlockType == BlockType.AllowSpawningRegardlessOfVision)
			Tags.Add( "AllowSpawning" );
		
		if(AffectsCommonZombies)
			Tags.Add( "AffectsCommonZombies" );
		if ( AffectsSpecialZombies )
			Tags.Add( "AffectsSpecialZombies" );
		if ( AffectsLootBoxes )
			Tags.Add( "AffectsLootBoxes" );

		SetupPhysicsFromModel( PhysicsMotionType.Static );
		EnableSolidCollisions = false;
		EnableTouch = true;

		Transmit = TransmitType.Never;
	}

	/// <summary>
	/// Enables this trigger
	/// </summary>
	[Input]
	public void Enable()
	{
		Enabled = true;
	}

	/// <summary>
	/// Disables this trigger
	/// </summary>
	[Input]
	public void Disable()
	{
		Enabled = false;
	}
}

public enum BlockType
{
	BlockSpawning,
	AllowSpawningRegardlessOfVision
}
