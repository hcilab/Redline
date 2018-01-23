using UnityEngine;

public class GameMaster : MonoBehaviour
{
	private static DamageNumberController _damageNumberController;
	private static DeathScreenController _deathScreenController;

	// Use this for initialization
	void Awake()
	{
		_damageNumberController = GetComponent<DamageNumberController>();
		_deathScreenController = FindObjectOfType< DeathScreenController >();
		Debug.Log( _deathScreenController  );
	}

	public static void onDeath( double score )
	{
		_deathScreenController.enabled = true;
		_deathScreenController.setScore( score.ToString() );
		_deathScreenController.show();
	}

	public static DamageNumberController GetDamageNumberController()
	{
		return _damageNumberController;
	}

	public static ObjectPoolController InstantiatePool(int poolSize, ObjectPoolItem item)
	{
		ObjectPoolController pool = Instantiate(
          			Resources.Load<ObjectPoolController>("Prefabs/ObjectPool")
          		);
		
		pool.Init( poolSize, item);
		return pool;
	}
}
