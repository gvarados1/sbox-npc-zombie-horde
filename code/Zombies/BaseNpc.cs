using Sandbox;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ZombieHorde;

[Category( "NPC" )]
public partial class BaseNpc : AnimatedEntity
{
	// it's nothing lol
	// nvm not anymore

	DamageInfo LastDamage;

	public override void Spawn()
	{
		base.Spawn();
		Tags.Add( "npc" );
	}

	public override void TakeDamage( DamageInfo info )
	{
		LastDamage = info;

		// hack - hitbox group 1 is head
		// we should be able to get this from somewhere (it's pretty specific to citizen though?)
		if ( GetHitboxGroup( info.HitboxIndex ) == 1 )
		{
			info.Damage *= 1.5f;
		}

		this.ProceduralHitReaction( info );

		base.TakeDamage( info );
	}

	public override void OnKilled()
	{
		base.OnKilled();

		if ( LastDamage.Flags.HasFlag( DamageFlags.Blast ) )
		{
			using ( Prediction.Off() )
			{
				var particles = Particles.Create( "particles/gib.vpcf" );
				if ( particles != null )
				{
					particles.SetPosition( 0, Position + Vector3.Up * 40 );
				}
			}
		}
		else
		{
			//BecomeRagdollOnClient( LastDamage.Force*5, GetHitboxBone( LastDamage.HitboxIndex )); // increased damage force, make ragdolls go flying!
			BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex )); // increased damage force, make ragdolls go flying!
		}
	}
}
