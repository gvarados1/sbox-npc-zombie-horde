using Sandbox;
using Sandbox.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZombieHorde;

[Library( "zom_npcspawner", Title = "NPC Spawner" )]
partial class NpcSpawner : DeathmatchWeapon
{ 
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	public override float PrimaryRate => 15.0f;
	public override float SecondaryRate => 1.0f;
	public override float ReloadTime => 3.0f;

	public override int Bucket => 4;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
		AmmoClip = 69;
	}

	public override bool CanPrimaryAttack()
	{
		return base.CanPrimaryAttack() && Input.Pressed( InputButton.PrimaryAttack );
	}

	public override void RenderCrosshair( in Vector2 center, float lastAttack, float lastReload )
	{
		var draw = Render.Draw2D;

		var color = Color.Lerp( Color.Red, Color.Yellow, lastReload.LerpInverse( 0.0f, 0.4f ) );

		draw.BlendMode = BlendMode.Lighten;
		draw.Color = color.WithAlpha( 0.2f + CrosshairLastShoot.Relative.LerpInverse( 1.2f, 0 ) * 0.5f );

		draw.Circle( center, 3 );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();
		//PlaySound( "rust_pistol.shoot" );


		// spawn npc
		if (!Host.IsServer) return;

		var startPos = Owner.EyePosition;
		var dir = Owner.EyeRotation.Forward;
		var tr = Trace.Ray(startPos, startPos + dir * 5000)
					.Ignore(Owner)
					.Run();

		var npc = new CommonZombie
		{
			Position = tr.EndPosition,
			//Position = Owner.Position,

			Rotation = Rotation.LookAt(Owner.EyeRotation.Backward.WithZ(0))

		};
	}

	public override void Reload()
	{
		// Tell the clients to play the shoot effects
		//ShootEffects();
		//PlaySound( "rust_pistol.shoot" );


		// move npc
		if (!Host.IsServer) return;

		foreach ( var npc in Entity.All.OfType<CommonZombie>().ToArray() )
		{
			npc.Target = Entity.All.OfType<Player>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault(); // find a random player
			npc.StartChase();
		}

	}

	/*
    public override void AttackSecondary()
    {
		if (!Host.IsServer) return;

		var HasLos = true;
		var SpawnPos = Position;
		var Tries = 0;

		while (HasLos && Tries <= 50)
		{
			Tries += 1;
			var t = NavMesh.GetPointWithinRadius(Position, 1000, 4000);
			if (t.HasValue)
			{
				SpawnPos = t.Value;
				var AddHeight = new Vector3(0,0,70);

				var PlayerPos = Owner.EyePos;
				var tr = Trace.Ray(SpawnPos + AddHeight, PlayerPos)
							.UseHitboxes()
							.Run();

				if(Vector3.DistanceBetween(tr.EndPos, PlayerPos) > 100)
				{
					HasLos = false;
					//Log.Info("no los");
					//var npc1 = new BaseInfected{Position = tr.EndPos,};
				}
				else
                {
					Log.Warning("Can't Find Valid Zombie Spawn");
                }

				
			}
		}

		if (Tries > 10) return;
		var npc = new BaseInfected
		{
			Position = SpawnPos,
			//Position = Owner.Position,

			Rotation = Rotation.LookAt(Owner.EyeRot.Backward.WithZ(0))

		};

	}
	/*
    public override void Reload()
    {
		if (!Host.IsServer) return;

		var npcs = Entity.All.Where(entity => entity is BaseInfected);
		foreach (var ent in npcs)
		{
			if (ent is BaseInfected npc)
			{
				var wander = new Sandbox.Nav.Wander();
				wander.MinRadius = 500;
				wander.MaxRadius = 2000;
				npc.Steer = wander;

				if (!wander.FindNewTarget(npc.Position))
				{
					DebugOverlay.Text(npc.EyePos, "COULDN'T FIND A WANDERING POSITION!", 5.0f);
				}
			}
		}
	}
	*/
}
