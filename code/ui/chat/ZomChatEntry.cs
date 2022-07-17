using Sandbox.UI.Construct;
using Sandbox.UI;
using Sandbox;


namespace ZombieHorde
{
	public partial class ZomChatEntry : Panel
	{
		public Label NameLabel { get; internal set; }
		public Label Message { get; internal set; }
		public Image Avatar { get; internal set; }

		public RealTimeSince TimeSinceBorn = 0;

		public ZomChatEntry()
		{
			Avatar = Add.Image();
			NameLabel = Add.Label( "Name", "name" );
			Message = Add.Label( "Message", "message" );
		}

		public override void Tick() 
		{
			base.Tick();

			if ( TimeSinceBorn > 12 ) 
			{ 
				Delete();
			}
		}
	}
}
