global using Sandbox;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using SandboxEditor;

using Sandbox.Component;
using System.ComponentModel;
using Sandbox.UI;

namespace ZombieHorde;

/// <summary>
/// This is the heart of the gamemode. It's responsible
/// for creating the player and stuff.
/// </summary>
partial class ZombieGame : Game
{
	[Net]
	ZomHud Hud { get; set; }
	[Net]
	public BaseGamemode Gamemode { get; set; } = new();

	StandardPostProcess postProcess;

	public ZombieGame()
	{
		//
		// Create the HUD entity. This is always broadcast to all clients
		// and will create the UI panels clientside.
		//
		if ( IsServer )
		{
			Hud = new ZomHud();
		}

		if ( IsClient )
		{
			postProcess = new StandardPostProcess();
			PostProcess.Add( postProcess );
		}

		//precache a bunch of stuff
		Precache.Add( "models/zombie/citizen_zombie/skins/citizen_skin_zombie01.vmat" );
		Precache.Add( "models/zombie/citizen_zombie/skins/citizen_skin_zombie02.vmat" );
		Precache.Add( "models/zombie/citizen_zombie/skins/citizen_skin_zombie03.vmat" );
		Precache.Add( "models/zombie/citizen_zombie/skins/citizen_eyes_zombie01.vmat" );
		Precache.Add( "models/zombie/citizen_zombie/skins/skin_zombie01.clothing" );
		Precache.Add( "models/zombie/citizen_zombie/skins/skin_zombie02.clothing" );
		Precache.Add( "models/zombie/citizen_zombie/skins/skin_zombie03.clothing" );
		Precache.Add( "weapons/ak47/ak47.vmat" );
		Precache.Add( "weapons/grenade/grenade.vmat" );
		Precache.Add( "weapons/magnum/magnum.vmat" );
	}

	public override void PostLevelLoaded()
	{
		base.PostLevelLoaded();

		// hack to disable nav blockers on "static" prop_physics
		foreach ( var prop in Entity.All.OfType<Prop>().ToArray() )
			prop.Components.RemoveAll();

		// just delete all doors in maps
		foreach ( var door in Entity.All.OfType<DoorEntity>().ToArray() )
			door.Delete();
			//door.Health = 5; // setting health of doors doesn't work :(


		// create MASTER AI DIRECTOR!
		var gameDirector = new GameDirector();

		// create MASTER GAMEMODE!
		Gamemode = new SurvivalGamemode();

	}

	public override void ClientJoined( Client cl )
	{
		base.ClientJoined( cl );

		Log.Info( $"\"{cl.Name}\" has joined the game" );
		ZomChatBox.AddInformation( To.Everyone, $"{cl.Name} has joined", $"avatar:{cl.PlayerId}" );

		var player = new HumanPlayer();
		player.UpdateClothes( cl );

		if ( BaseGamemode.Ent.EnableRespawning() )
		{
			player.Respawn();
		}
		else
		{
			player.SpawnAsSpectator();
		}

		cl.Pawn = player;
	}

	public override void MoveToSpawnpoint( Entity pawn )
	{
		var spawnpoint = Entity.All
								.OfType<SpawnPoint>()
								.OrderByDescending( x => SpawnpointWeight( pawn, x ) )
								.ThenBy( x => Guid.NewGuid() )
								.FirstOrDefault();

		//Log.Info( $"chose {spawnpoint}" );

		if ( spawnpoint == null )
		{
			Log.Warning( $"Couldn't find spawnpoint for {pawn}!" );
			return;
		}

		pawn.Transform = spawnpoint.Transform;
	}

	/// <summary>
	/// The higher the better
	/// </summary>
	public float SpawnpointWeight( Entity pawn, Entity spawnpoint )
	{
		float distance = 0;

		foreach ( var client in Client.All )
		{
			if ( client.Pawn == null ) continue;
			if ( client.Pawn == pawn ) continue;
			if ( client.Pawn.LifeState != LifeState.Alive ) continue;

			var spawnDist = (spawnpoint.Position - client.Pawn.Position).Length;
			distance = MathF.Max( distance, spawnDist );
		}

		//Log.Info( $"{spawnpoint} is {distance} away from any player" );

		return distance;
	}

	public override void OnKilled( Client client, Entity pawn )
	{
		base.OnKilled( client, pawn );

		Hud.OnPlayerDied( To.Everyone, pawn as HumanPlayer );
	}


	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );

		postProcess.Sharpen.Enabled = true;
		postProcess.Sharpen.Strength = 0f;//0.5f;

		postProcess.FilmGrain.Enabled = true;
		postProcess.FilmGrain.Intensity = 0f;//0.2f;
		postProcess.FilmGrain.Response = 1;

		postProcess.Vignette.Enabled = true;
		postProcess.Vignette.Intensity = 1.0f;
		postProcess.Vignette.Roundness = 1.5f;
		postProcess.Vignette.Smoothness = 0.5f;
		postProcess.Vignette.Color = Color.Black;

		postProcess.Saturate.Enabled = true;
		postProcess.Saturate.Amount = 1;

		postProcess.Blur.Enabled = false;

		Audio.SetEffect( "core.player.death.muffle1", 0 );

		if ( Local.Pawn is HumanPlayer localPlayer )
		{
			var timeSinceDamage = localPlayer.TimeSinceDamage.Relative;
			var damageUi = timeSinceDamage.LerpInverse( 0.25f, 0.0f, true ) * 0.2f;
			if ( damageUi > 0 )
			{
				postProcess.Saturate.Amount -= damageUi;
				postProcess.Vignette.Color = Color.Lerp( postProcess.Vignette.Color, Color.Red, damageUi );
				postProcess.Vignette.Intensity += damageUi;
				postProcess.Vignette.Smoothness += damageUi;
				postProcess.Vignette.Roundness += damageUi;

				postProcess.Blur.Enabled = true;
				postProcess.Blur.Strength = damageUi * 0.5f;
			}


			var healthDelta = localPlayer.Health.LerpInverse( 0, 90.0f, true ); // start lowhp effects at 90 instead of 100
			if ( localPlayer.LifeState == LifeState.Dying ) healthDelta = 0;

			healthDelta = MathF.Pow( healthDelta, 0.5f );

			postProcess.Vignette.Color = Color.Lerp( postProcess.Vignette.Color, Color.Red, 1 - healthDelta );
			postProcess.Vignette.Intensity += (1 - healthDelta) * 0.5f;
			postProcess.Vignette.Smoothness += (1 - healthDelta);
			postProcess.Vignette.Roundness += (1 - healthDelta) * 0.5f;
			postProcess.Saturate.Amount *= healthDelta;
			postProcess.FilmGrain.Intensity += (1 - healthDelta) * 0.5f;

			Audio.SetEffect( "core.player.death.muffle1", 1 - healthDelta, velocity: 2.0f );

		}
	}

	public static void Explosion( Entity weapon, Entity owner, Vector3 position, float radius, float damage, float forceScale, bool doEffects = true)
	{
		// Effects
		if ( doEffects )
		{
			Sound.FromWorld( "rust_pumpshotgun.shootdouble", position );
			Particles.Create( "particles/explosion/barrel_explosion/explosion_barrel.vpcf", position );
		}

		// Damage, etc
		var overlaps = Entity.FindInSphere( position, radius );

		foreach ( var overlap in overlaps )
		{
			if ( overlap is not ModelEntity ent || !ent.IsValid() )
				continue;

			if ( ent.LifeState != LifeState.Alive )
				continue;

			if ( !ent.PhysicsBody.IsValid() )
				continue;

			if ( ent.IsWorld )
				continue;

			var targetPos = ent.PhysicsBody.MassCenter;

			var dist = Vector3.DistanceBetween( position, targetPos );
			if ( dist > radius )
				continue;

			var tr = Trace.Ray( position, targetPos )
				.Ignore( weapon )
				.WorldOnly()
				.Run();

			if ( tr.Fraction < 0.98f )
				continue;

			var distanceMul = 1.0f - Math.Clamp( dist / radius, 0.0f, 1.0f );
			var dmg = damage * distanceMul;
			var force = (forceScale * distanceMul) * ent.PhysicsBody.Mass;
			var forceDir = (targetPos - position).Normal;

			var damageInfo = DamageInfo.Explosion( position, forceDir * force, dmg )
				.WithWeapon( weapon )
				.WithAttacker( owner );

			ent.TakeDamage( damageInfo );
		}
	}

	[ClientRpc]
	public override void OnKilledMessage( long leftid, string left, long rightid, string right, string method )
	{
		Sandbox.UI.KillFeed.Current?.AddEntry( leftid, left, rightid, right, method );
	}

	public override void RenderHud()
	{
		var localPawn = Local.Pawn as HumanPlayer;
		if ( localPawn == null ) return;

		//
		// scale the screen using a matrix, so the scale math doesn't invade everywhere
		// (other than having to pass the new scale around)
		//

		var scale = Screen.Height / 1080.0f;
		var screenSize = Screen.Size / scale;
		var matrix = Matrix.CreateScale( scale );

		using ( Render.Draw2D.MatrixScope( matrix ) )
		{
			localPawn.RenderHud( screenSize );
		}
	}

	public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
	{
		Log.Info( $"\"{cl.Name}\" has left the game ({reason})" );
		ZomChatBox.AddInformation( To.Everyone, $"{cl.Name} has left ({reason})", $"avatar:{cl.PlayerId}" );

		if ( cl.Pawn.IsValid() )
		{
			cl.Pawn.Delete();
			cl.Pawn = null;
		}

	}

}
