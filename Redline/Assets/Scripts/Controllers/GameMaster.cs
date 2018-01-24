using System;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
	private static DamageNumberController _damageNumberController;
	private static DeathScreenController _deathScreenController;
	private static bool _gameOver;
	public static bool _paused;
	private int _currentHpBarindex;
	[SerializeField] private HpBarController _currentHpBar;
	[SerializeField] private List<HpBarController> _hpBarControllers;
	[SerializeField] private Text _hpbarlabel;

	// Use this for initialization
	void Awake()
	{
		_damageNumberController = GetComponent<DamageNumberController>();
		_deathScreenController = FindObjectOfType< DeathScreenController >();
		_paused = false;

		_currentHpBarindex = _hpBarControllers.IndexOf( _currentHpBar );
		_hpbarlabel.text = _currentHpBar.name;

	}

	private void Update()
	{
		if ( Input.GetKeyDown( KeyCode.R ) && _gameOver )
		{
			restart();
		} else if ( Input.GetKeyDown( KeyCode.Period ) )
		{
			ChangeHpBar( -1 );
		} else if ( Input.GetKeyDown( KeyCode.Comma ) )
		{
			ChangeHpBar( 1 );
		}
	}

	private void ChangeHpBar( int direction )
	{
		Debug.Log("change HP bar"  );
		_currentHpBar.enabled = false;
		
		_currentHpBarindex = ( ( ( _currentHpBarindex + direction ) % _hpBarControllers.Count ) 
		+ _hpBarControllers.Count ) % _hpBarControllers.Count;
		_currentHpBar = _hpBarControllers[ _currentHpBarindex ];
		
		_currentHpBar.enabled = true;
		_hpbarlabel.text = _currentHpBar.name;
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
