using System;
using System.Collections.Generic;
using System.Resources;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
	private DamageNumberController _damageNumberController;
	private bool _gameOver;
	public bool Paused;
	private int _currentHpBarindex;
	public static GameMaster Instance = null;
	[SerializeField] private DeathScreenController _deathScreenController;
	[SerializeField] private HpBarController _currentHpBar;
	[SerializeField] private List<HpBarController> _hpBarControllers;
	[SerializeField] private Text _hpbarlabel;

	// Use this for initialization
	void Awake()
	{

		if ( Instance == null )
			Instance = this;
		else if ( Instance != this ) 
			Destroy( this );

		DontDestroyOnLoad( Instance );
		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		_damageNumberController = GetComponent<DamageNumberController>();
		Paused = arg0.name == "mainMenu";
        
        _currentHpBarindex = _hpBarControllers.IndexOf( _currentHpBar );
        _hpbarlabel.text = _currentHpBar.name;
	}
	
	private void Update()
	{
		if ( Input.GetKeyDown( KeyCode.R ) && _gameOver )
		{
			Restart();
		} else if ( Input.GetKeyDown( KeyCode.Escape ) )
		{
			SceneManager.LoadScene( "mainMenu" );
		} else if ( Input.GetKeyDown( KeyCode.N ) && _gameOver )
		{
			NextLevel();
		} else if ( Input.GetKeyDown( KeyCode.Period ) )
		{
			ChangeHpBar( -1 );
		} else if ( Input.GetKeyDown( KeyCode.Comma ) )
		{
			ChangeHpBar( 1 );
		} 
	}

	private void NextLevel()
	{
		string currentLvl = SceneManager.GetActiveScene().name.Substring( 5 );
		Debug.Log( currentLvl );
		var nextLvl = Int32.Parse( currentLvl ) + 1;

		if ( nextLvl > 3 )
		{
			SceneManager.LoadScene( "mainMenu" );
		}
		else
		{
			SceneManager.LoadScene( "level" + nextLvl );
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

	public void OnDeath( double score )
	{
		Paused = true;
		_gameOver = true;
		_deathScreenController.enabled = true;
		_deathScreenController.setScore( score.ToString() );
		_deathScreenController.show();
	}

	private static void Restart()
	{
		string sceneName = SceneManager.GetActiveScene().name;
		SceneManager.LoadScene( sceneName );
	}

	public DamageNumberController GetDamageNumberController()
	{
		return _damageNumberController;
	}

	public ObjectPoolController InstantiatePool(int poolSize, ObjectPoolItem item)
	{
		ObjectPoolController pool = Instantiate(
          			Resources.Load<ObjectPoolController>("Prefabs/ObjectPool")
          		);
		
		pool.Init( poolSize, item);
		return pool;
	}

	public void OnVictory()
	{
		OnDeath( FindObjectOfType<PlayerController>().GetScore() );
	}

	public void ResetUi()
	{
		Paused = false;
		_gameOver = false;
		_deathScreenController.hide();
	}
}
