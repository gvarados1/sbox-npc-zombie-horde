
namespace ZombieHorde.Nav
{

	public class Wander : NavSteer
	{
		public float MinRadius { get; set; } = 500;
		public float MaxRadius { get; set; } = 1000;

		public Wander()
		{

		}

		public override void Tick( Vector3 position, Vector3 velocity = new Vector3(), float sharpStartAngle = 360f )
		{
			base.Tick( position, velocity * 10, 360f );

			if ( Path.IsEmpty )
			{
				if(Rand.Int(60) == 0)
					FindNewTarget( position );
			}
		}


		public virtual bool FindNewTarget( Vector3 center )
		{
			var t = NavMesh.GetPointWithinRadius( center, MinRadius, MaxRadius );
			if ( t.HasValue )
			{
				Target = t.Value;
			}

			return t.HasValue;
		}

	}

}
