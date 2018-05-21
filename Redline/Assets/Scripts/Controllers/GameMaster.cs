using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
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
	[SerializeField] private Canvas _UI;

	[SerializeField] public DataCollectionController DataCollector;
	private float _roundStart;
	private FireSystemController _fireSystem;
	public int SessionID = 0;

	// Use this for initialization
	void Awake()
	{

		if ( Instance == null )
			Instance = this;
		else if ( Instance != this )
			Destroy( this );

		DontDestroyOnLoad( Instance );
		_currentHpBarindex = _hpBarControllers.IndexOf( _currentHpBar );
		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		_roundStart = Time.time;
		_player = FindObjectOfType< PlayerController >();
		_fireSystem = FindObjectOfType< FireSystemController >();
	}

	public void RestartLevel()
	{
		NextLevel("restart");
	}

	private void Update()
	{
		if ( Input.GetKeyDown( KeyCode.R ) && _gameOver )
		{
			RestartLevel();
		}
		else if ( Input.GetKeyDown( KeyCode.Escape ) && _gameOver )
		{
			GoToMenu();
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

	public void GoToMenu()
	{
		ResetUi();
		SceneManager.LoadScene( "mainMenu" );
		_UI.gameObject.SetActive( false );
		_gameOver = false;
	}

	public void NextLevel( [CanBeNull] string customLevel = null )
	{
		ResetUi();
		int nextLvl;
		string currentLvl = SceneManager.GetActiveScene().name.Substring( 5 );

		if ( customLevel == "restart" )
		{
			nextLvl = Int32.Parse( currentLvl );
		}
		else if ( customLevel != null )
		{
			nextLvl = Int32.Parse( customLevel );
		}
		else
		{
			nextLvl = Int32.Parse( currentLvl ) + 1;
		}
	
		Debug.Log( currentLvl );

		if ( nextLvl > 3 )
		{
			_UI.gameObject.SetActive( false );
			SceneManager.LoadScene( "mainMenu" );
		}
		else
		{
			_UI.gameObject.SetActive( true );
			SceneManager.LoadScene( "level" + nextLvl );
		}
	}

	private void ChangeHpBar( int direction )
	{
		Debug.Log( "change HP bar" );
		_currentHpBar.gameObject.SetActive( false );

		_currentHpBarindex = ((_currentHpBarindex + direction) % _hpBarControllers.Count
		                      + _hpBarControllers.Count) % _hpBarControllers.Count;
		_currentHpBar = _hpBarControllers[ _currentHpBarindex ];

		_currentHpBar.gameObject.SetActive( true );
		_hpbarlabel.text = _currentHpBar.name;
	}

	public void OnDeath( [CanBeNull] string message )
	{
		_player.LogCumulativeData();
		Paused = true;
		_gameOver = true;
		_deathScreenController.enabled = true;
		_deathScreenController.setMessage( message );
		_deathScreenController.setScore(  _player.GetScore().ToString());;
		_deathScreenController.SetFlameRating( _fireSystem.GetActiveFlames(), _fireSystem.GetTotalFlames(), 0f, 0f );
		_deathScreenController.SetHealthRating( _player.GetRemainingHitPoints(), _player.GetStartingHealth() );
		_deathScreenController.SetTimeRating( GetTimeRemaining(), _fireSystem.TotalTime() );
		_deathScreenController.show();
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
		_player.LogCumulativeData();
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
	
	

	public void OnTimeout()
	{
		OnDeath( "You ran out of time!" );
	}

	public void ResetUi()
	{
		_victoryScreenController.hide();
		_deathScreenController.hide();
		_hpbarlabel.text = _currentHpBar.name;
		Paused = false;
		_gameOver = false;
	}

	public void StartGame( string customLevel, int sessionId )
	{
		SessionID = sessionId;
		NextLevel( customLevel );
	}

	public void SaveToConfig( string level, MonoBehaviour gameObject )
	{
		string json = JsonUtility.ToJson( gameObject, true );
		
		string path = Path.Combine( Application.streamingAssetsPath, level + ".json" );

		byte[] jsonAsBytes = Encoding.ASCII.GetBytes( json );
		File.WriteAllBytes( path, jsonAsBytes  );
	}

	private IEnumerator DownloadConfig( string url )
	{
		using ( WWW www = new WWW( url ) )
		{
			yield return www;
			JsonUtility.FromJsonOverwrite( www.text, gameObject );
		}
	}

	public void LoadFromConfig( string level, MonoBehaviour gameObject )
	{
		string path = Path.Combine( Application.streamingAssetsPath, level + ".json" );
		#if UNITY_WEBGL
			StartCoroutine( DownloadConfig( path ) );
		#else
			if ( File.Exists( path ) )
			{
				Debug.Log( "Loading data for " + level  );
				var stringData = File.ReadAllText( path );
				JsonUtility.FromJsonOverwrite( stringData, gameObject );
			} 
		#endif
	}

	public double GetTimeRemaining()
	{
		return Math.Round( _player.FireSystemController.GetTimeLeft() );
	}

	public int GetActiveFlames()
	{
		return _fireSystem.GetActiveFlames();
	}

	public string GetHpBarType()
	{
		return _hpbarlabel.text;
	}
}
