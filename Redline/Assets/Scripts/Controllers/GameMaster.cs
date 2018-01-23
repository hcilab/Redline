using System.Resources;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMaster : MonoBehaviour
{
	private static DamageNumberController _damageNumberController;
	private static DeathScreenController _deathScreenController;
	private static bool _gameOver;
	public static bool _paused;

	// Use this for initialization
	void Awake()
	{
		_damageNumberController = GetComponent<DamageNumberController>();
		_deathScreenController = FindObjectOfType< DeathScreenController >();
		_paused = false;
	}

	private void Update()
	{
		if ( Input.GetKey( KeyCode.R ) && _gameOver )
		{
			restart();
		}
	}

	public static void onDeath( double score )
	{
		_paused = true;
		_gameOver = true;
		_deathScreenController.enabled = true;
		_deathScreenController.setScore( score.ToString() );
		_deathScreenController.show();
	}

	private static void restart()
	{
		string scene_name = SceneManager.GetActiveScene().name;
		SceneManager.LoadScene( scene_name );
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
