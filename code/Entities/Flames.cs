using Sandbox;
using Sandbox.Component;
using Sandbox.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;

namespace ZombieHorde;

partial class Flames : ModelEntity
{
	public static readonly Model WorldModel = Model.Load( "assets/ammobox/ammo_box.vmdl" );

	public TimeUntil TimeUntilExpire = 10;
	public Particles Particle;
	public Sound Sound;

	public Flames( bool playSound = true )
	{
		Particle = Particles.Create( "particles/fire_molotov_01.vpcf", this );
		if(playSound)
			Sound = Sound.FromEntity( "molotov.burn_loop", this );
	}
	public override void Spawn()
	{
		base.Spawn();

		Model = WorldModel;

		PhysicsEnabled = true;
		UsePhysicsCollision = true;
	}

	[Event.Tick]
	public void Tick()
	{
		//if ( !IsServer ) return;
		//if ( TimeUntilExpire < 0 )
		//	Expire();
	}

	public void Expire()
	{
		Sound.Stop();
		Particle.Destroy();
		Delete();
	}

}
