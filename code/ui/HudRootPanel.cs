using Sandbox.UI;

namespace ZombieHorde;

public class HudRootPanel : RootPanel
{
	public static HudRootPanel Current;

	public Scoreboard Scoreboard { get; set; }

	public HudRootPanel()
	{
		Current = this;

		StyleSheet.Load( "/resource/styles/hud.scss" );
		SetTemplate( "/resource/templates/hud.html" );

		AddChild<DamageIndicator>();
		AddChild<HitIndicator>();

		AddChild<InventoryBar>();
		AddChild<PickupFeed>();

		AddChild<ZomChatBox>();
		Scoreboard = AddChild<Scoreboard>();
		AddChild<VoiceList>();
		AddChild<VoiceSpeaker>();

	}

	public override void Tick()
	{
		base.Tick();
	}

	protected override void UpdateScale( Rect screenSize )
	{
		base.UpdateScale( screenSize );
	}
}
