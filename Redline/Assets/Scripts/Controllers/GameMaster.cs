using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework.Constraints;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
	private bool _gameOver;
	public bool Paused;
	private int _currentHpBarindex;
	public static GameMaster Instance = null;
	private PlayerController _player;
	[SerializeField] private DeathScreenController _deathScreenController;
	[SerializeField] private DeathScreenController _victoryScreenController;
	[SerializeField] private HpBarController _currentHpBar;
	[SerializeField] private List< HpBarController > _hpBarControllers;
	[SerializeField] private Text _hpbarlabel;

	[SerializeField] private NumberController _damageNumberController;

	[SerializeField] private NumberController _scoreNumberController;
	private float _roundStart;

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
		_roundStart = Time.time;
		_currentHpBarindex = _hpBarControllers.IndexOf( _currentHpBar );
		_hpbarlabel.text = _currentHpBar.name;
		_player = FindObjectOfType< PlayerController >();
	}

	private void Update()
	{
		if ( Input.GetKeyDown( KeyCode.R ) && _gameOver )
		{
			Restart();
		}
		else if ( Input.GetKeyDown( KeyCode.Escape ) && _gameOver )
		{
			SceneManager.LoadScene( "mainMenu" );
			_gameOver = false;
		}
		else if ( Input.GetKeyDown( KeyCode.N ) && _gameOver )
		{
			NextLevel();
		}
		else if ( Input.GetKeyDown( KeyCode.Period ) )
		{
			ChangeHpBar( -1 );
		}
		else if ( Input.GetKeyDown( KeyCode.Comma ) )
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
		Debug.Log( "change HP bar" );
		_currentHpBar.enabled = false;

		_currentHpBarindex = (((_currentHpBarindex + direction) % _hpBarControllers.Count)
		                      + _hpBarControllers.Count) % _hpBarControllers.Count;
		_currentHpBar = _hpBarControllers[ _currentHpBarindex ];

		_currentHpBar.enabled = true;
		_hpbarlabel.text = _currentHpBar.name;
	}

	public void OnDeath( )
	{
		Paused = true;
		_gameOver = true;
		_deathScreenController.enabled = true;
		_deathScreenController.setScore(  _player.GetScore().ToString());
		_deathScreenController.show();
	}

	private static void Restart()
	{
		string sceneName = SceneManager.GetActiveScene().name;
		SceneManager.LoadScene( sceneName );
	}

	public NumberController GetDamageNumberController()
	{
		return _damageNumberController;
	}

	public NumberController GetScoreNumberController()
	{
		return _scoreNumberController;
	}

	public static ObjectPoolController InstantiatePool(int poolSize, ObjectPoolItem item, string poolName)
	{
		ObjectPoolController pool = Instantiate(
          			Resources.Load<ObjectPoolController>("Prefabs/ObjectPool")
          		);
		pool.name = poolName;
		pool.Init( poolSize, item);
		return pool;
	}

	public void OnVictory( )
	{
		var roundEnd = Time.time;
		var roundDuration = ( roundEnd - _roundStart ) * 500;
		var bonus = Math.Round(10000 - roundDuration);
		bonus = bonus > 0 ? bonus : 0;
		Paused = true;
		_gameOver = true;
		_victoryScreenController.enabled = true;
		_victoryScreenController.setScore( _player.GetScore()  + " + a time bonus of " + bonus );
		_victoryScreenController.show();
	}

	public void ResetUi()
	{
		_victoryScreenController.hide();
		_deathScreenController.hide();
		Paused = false;
		_gameOver = false;
	}

	public void StartGame( string customLevel )
	{
		
		SceneManager.LoadScene( "level" + customLevel );
	}

	public void SaveToConfig( string level, FireSystemController fireSystemController )
	{
		string json = JsonUtility.ToJson( fireSystemController, true );
		
		string path = Path.Combine( Application.streamingAssetsPath, level + ".json" );

		byte[] jsonAsBytes = Encoding.ASCII.GetBytes( json );
		File.WriteAllBytes( path, jsonAsBytes  );
	}

	public void LoadFromConfig( string level, FireSystemController fireSystemController )
	{
		string path = Path.Combine( Application.streamingAssetsPath, level + ".json" );

		if ( File.Exists( path ) )
		{
			Debug.Log( "Loading data for " + level  );
			var stringData = File.ReadAllText( path );
			JsonUtility.FromJsonOverwrite( stringData, fireSystemController );
		} 
	}
}
