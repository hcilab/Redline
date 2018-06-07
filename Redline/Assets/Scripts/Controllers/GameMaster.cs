using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
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
	[SerializeField] private DeathScreenController _deathScreenController;
	[SerializeField] private DeathScreenController _victoryScreenController;
	[SerializeField] private HpBarController _currentHpBar;
	[SerializeField] private List< HpBarController > _hpBarControllers;
	[SerializeField] private Text _hpbarlabel;

	[SerializeField] private NumberController _damageNumberController;

	[SerializeField] private NumberController _scoreNumberController;
	[SerializeField] private Canvas _gameInterface;

	[SerializeField] public DataCollectionController DataCollector;
	[SerializeField] private GameObject _uploadModal;
	
	private int _sessionId;
	private bool _uploadComplete = false;
	private MainMenuController _mainMenu;
	private LevelManager _levelManager;

	public int SessionID
	{
		get { return _sessionId; }	
	}
	
	public enum GameEnd 
	{
		Victory,
		Timeout,
		Death
	}
	
	private class IdObject
	{
		public string id = "NONE";
	}

	private class BarObject
	{
		public string bar = "NONE";
	}

	[UsedImplicitly]
	private class Levels
	{ 
		public const string MainMenu = "mainMenu";
		public const string Level1 = "level1";
	}

	// Use this for initialization
	void Awake()
	{

		if ( Instance == null )
			Instance = this;
		else if ( Instance != this )
			Destroy( this );

		DontDestroyOnLoad( Instance );
	}

	private void Start()
	{
		DataCollector.GetBarType( data =>
		{
			ChangeHpBar( Int32.Parse(
				JsonUtility.FromJson< BarObject >( data ).bar ) );
		} );
		
		DataCollector.GetNewID( data =>
		{
			_sessionId = Int32.Parse(
				JsonUtility.FromJson< IdObject >( data ).id );
			FindObjectOfType<MainMenuController>().SetSessionId( _sessionId.ToString() );
		} );
	}

	public void RegisterLevel( LevelManager levelManager )
	{
		Debug.Log( "Registering new level"  );
		_levelManager = levelManager;
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
			ScrollHpBar( -1 );
		}
		else if ( Input.GetKeyDown( KeyCode.Comma ) )
		{
			ScrollHpBar( 1 );
		}

		if ( _uploadComplete ) _uploadModal.SetActive( false );
	}

	public void GoToMenu()
	{
		ResetUi();
		SceneManager.LoadScene( Levels.MainMenu );
		_gameInterface.gameObject.SetActive( false );
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
		else if ( !string.IsNullOrEmpty( customLevel ) )
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
			_gameInterface.gameObject.SetActive( false );
			SceneManager.LoadScene( Levels.MainMenu );
		}
		else
		{
			_gameInterface.gameObject.SetActive( true );
			SceneManager.LoadScene( "level" + nextLvl );
		}
	}

	private void ScrollHpBar( int direction )
	{
		_currentHpBarindex = ((_currentHpBarindex + direction) % _hpBarControllers.Count
		                      + _hpBarControllers.Count) % _hpBarControllers.Count;
		ChangeHpBar( _currentHpBarindex );
	}

	private void ChangeHpBar( int index )
	{
		Debug.Log( "change HP bar" );
		_currentHpBar.gameObject.SetActive( false );
		_currentHpBar = _hpBarControllers[ _currentHpBarindex ];

		_currentHpBar.gameObject.SetActive( true );
		_hpbarlabel.text = _currentHpBar.name;
	}

	public void GameOver( [CanBeNull] GameEnd reason )
	{
		if( !_gameOver ) 
			_levelManager.Player.LogCumulativeData();
		Paused = true;
		_gameOver = true;

		StartCoroutine( DataCollector.ProcessUploadBacklog( progress =>
		{
			_uploadModal.SetActive( true );
			_uploadModal.GetComponentInChildren< Text >().text = "Uploading data: " + progress + "%";
			Debug.Log( "Upload progress: " + progress );
			if ( progress >= 1 ) _uploadComplete = true;
		} ) );

		string message;
		switch ( reason )
		{
				case GameEnd.Victory:
					message = "Congratulations you did it!";
					break;
				case GameEnd.Death:
					message = "You're gonna hurt yourself! Get out of there!";
					break;
				case GameEnd.Timeout:
					message = "Reinforcements have arrived! Take a breather!";
					break;
				default:
					message = "Game Over!";
					break;
		}
		
		_deathScreenController.enabled = true;
		_deathScreenController.setMessage( message );
		_deathScreenController.setScore(  _levelManager.Player.GetScore().ToString());;
		_deathScreenController.SetFlameRating( _levelManager.FireSystem.GetActiveFlames(), _levelManager.FireSystem.GetTotalFlames(), 0f, 0f );
		_deathScreenController.SetHealthRating( _levelManager.Player.GetRemainingHitPoints(), _levelManager.Player.GetStartingHealth() );
		_deathScreenController.SetTimeRating( GetTimeRemaining(), _levelManager.FireSystem.TotalTime() );
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
	

	public void OnTimeout()
	{
		GameOver( GameEnd.Timeout );
	}

	public void ResetUi()
	{
		_victoryScreenController.hide();
		_deathScreenController.hide();
		_hpbarlabel.text = _currentHpBar.name;
		Paused = false;
		_gameOver = false;
	}

	public void StartGame( string customLevel )
	{
		NextLevel( customLevel );
	}

	public void SaveToConfig( string level, MonoBehaviour gameObject )
	{
		string json = JsonUtility.ToJson( gameObject, true );
		
		string path = Path.Combine( Application.streamingAssetsPath, level + ".json" );

		byte[] jsonAsBytes = Encoding.ASCII.GetBytes( json );
		File.WriteAllBytes( path, jsonAsBytes  );
	}

	public void LoadFromConfig( string level, MonoBehaviour gameObject )
	{
		string path = Path.Combine( Application.streamingAssetsPath, level + ".json" );
		DataCollector.LoadConfig( path, data =>
		{
			Debug.Log( "Attempting to load " + data + " into " + gameObject.name );
			JsonUtility.FromJsonOverwrite( data, gameObject );
		} );
	}

	public double GetTimeRemaining()
	{
		return Math.Round( _levelManager.FireSystem.GetTimeLeft() );
	}

	public int GetActiveFlames()
	{
			return _levelManager.FireSystem.GetActiveFlames();
	}

	public string GetHpBarType()
	{
		return _hpbarlabel.text;
	}
}
