﻿namespace ZombieHorde;
public partial class BaseNpc
{
	// TODO - make ragdolls one per entity
	// TODO - make ragdolls dissapear after a load of seconds
	static EntityLimit RagdollLimit = new EntityLimit { MaxTotal = 25 };

	[ClientRpc]
	void BecomeRagdollOnClient( Vector3 force, int forceBone)
	{
		// TODO - lets not make everyone write this shit out all the time
		// maybe a CreateRagdoll<T>() on ModelEntity?
		var ent = new ModelEntity();
		ent.Position = Position;
		ent.Rotation = Rotation;
		ent.PhysicsEnabled = true;
		ent.UsePhysicsCollision = true;
		ent.Tags.Add( "gib" );

		ent.CopyFrom( this );
		ent.CopyBonesFrom( this );
		ent.SetRagdollVelocityFrom( this );
		ent.DeleteAsync( 20.0f );
		ent.RenderColor = RenderColor;

		// Copy the clothes over
		foreach ( var child in Children )
		{
			if ( !child.Tags.Has( "clothes" ) )
				continue;

			if ( child is ModelEntity e )
			{
				var clothing = new ModelEntity();
				clothing.CopyFrom( e );
				clothing.SetParent( ent, true );
				clothing.RenderColor = RenderColor;
			}
		}

		ent.PhysicsGroup.AddVelocity( force );

		if ( forceBone >= 0 )
		{
			var body = ent.GetBonePhysicsBody( forceBone );
			if ( body != null )
			{
				body.ApplyForce( force * 1000 );
			}
			else
			{
				ent.PhysicsGroup.AddVelocity( force );
			}
		}


		//Corpse = ent;

		RagdollLimit.Watch( ent );
	}
}
