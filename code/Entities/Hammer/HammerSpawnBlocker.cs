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

		SetupTags();

		SetupPhysicsFromModel( PhysicsMotionType.Static );
		EnableSolidCollisions = false;
		EnableTouch = true;

		Transmit = TransmitType.Never;
	}

	public void SetupTags()
	{
		Tags.Clear();

		Tags.Add( "trigger" );

		if (BlockType == BlockType.BlockSpawning )
		{
			if ( AffectsCommonZombies )
				Tags.Add( "BlockCommonZombieSpawn" );
			if ( AffectsSpecialZombies )
				Tags.Add( "BlockSpecialZombieSpawn" );
			if ( AffectsLootBoxes )
				Tags.Add( "BlockLootBoxSpawn" );
		}
		else if ( BlockType == BlockType.AllowSpawningRegardlessOfVision )
		{
			if ( AffectsCommonZombies )
				Tags.Add( "AllowCommonZombieSpawn" );
			if ( AffectsSpecialZombies )
				Tags.Add( "AllowSpecialZombieSpawn" );
			if ( AffectsLootBoxes )
				Tags.Add( "AllowLootBoxSpawn" );
		}
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

public enum AffectsType
{
	CommonZombies,
	SpecialZombies,
	LootBoxes
}
