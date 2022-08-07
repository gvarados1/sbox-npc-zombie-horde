namespace ZombieHorde;
public partial struct DamageInfoExt
{
	//
	// Summary:
	//     Creates a new DamageInfo with the DamageFlag Blast
	public static DamageInfo FromCustom( Vector3 sourcePosition, Vector3 force, float damage, DamageFlags damageFlag )
	{
		DamageInfo result = default( DamageInfo );
		result.Position = sourcePosition;
		result.Force = force;
		result.Damage = damage;
		result.Flags = damageFlag;
		return result;
	}
}
