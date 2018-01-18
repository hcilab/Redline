using UnityEngine;

public class GameMaster : MonoBehaviour
{
	private static DamageNumberController _damageNumberController;

	// Use this for initialization
	void Awake()
	{
		_damageNumberController = GetComponent<DamageNumberController>();
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
