using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
	[DllImport( "__Internal" )]
	private static extern void RemoveLoader();
	
	[DllImport( "__Internal" )]
	public static extern int GetSetNumber();
	
	[DllImport( "__Internal" )]
	private static extern int GetBarType();
	
	private bool _gameOver;
	public bool Paused;
	private int _currentHpBarindex;
	public static GameMaster Instance = null;
	[SerializeField] private DeathScreenController _deathScreenController;
	[SerializeField] private GameObject _endOfGameScreen;
	[SerializeField] private HpBarController _currentHpBar;
	[SerializeField] private List< HpBarController > _hpBarControllers;
	[SerializeField] private Text _hpbarlabel;

	[SerializeField] private NumberController _damageNumberController;

	[SerializeField] private NumberController _scoreNumberController;
	[SerializeField] private UIController _gameInterface;

	[SerializeField] public DataCollectionController DataCollector;
	[SerializeField] private GameObject _uploadModal;
	[SerializeField] private GameObject _pauseScreen;
	[SerializeField] private GameObject _loadingScreen;
	
	private int _sessionId;
	private bool _uploadComplete = false;
	private MainMenuController _mainMenu;
	private LevelManager _levelManager;
	private string _playerConfig = null;
	private int _currentLevel = 1;
	private int _levelCount = -1;
	private bool _fireLoaded;
	private bool _playerLoaded;
	private int _trialNumber;
	private bool _hasTrialNumber;
	private bool _initialized;
	private bool _loadingLevel = false;
	private int _turkId;
	private int _setNumber = 0;

	public int LevelCount
	{
		get { return _levelCount; }
	}
	
	public int CurrentLevel
	{
		get { return _currentLevel; }
	}

	public int TrialNumber
	{
		get { return _trialNumber; }
	}
	
	public int SessionID
	{
		get { return _sessionId; }	
	}

	public int TurkId
	{
		get { return _turkId; }
	}

	public int SetNumber
	{
		get { return _setNumber; }
	}

	private class DataObject
	{
		public string id = "NONE";
		public string bar = "NONE";
		public string count = "NONE";
		public string trial = "NONE";
	}

	[UsedImplicitly]
	private class Levels
	{ 
		public const string MainMenu = "mainMenu";
		public const string Level = "levelScene";
	}

	// Use this for initialization
	void Awake()
	{
		foreach ( HpBarController barController in _hpBarControllers )
		{
			barController.gameObject.SetActive( false );
		}
		
		if ( Instance == null )
			Instance = this;
		else if ( Instance != this )
			Destroy( this );

		ResetUi();
		_gameInterface.gameObject.SetActive( false );

		DontDestroyOnLoad( Instance );
	}

	private void Start()
	{
		_gameInterface.Awake();
		
		ReloadConfigs();
		
		#if UNITY_WEBGL && !UNITY_EDITOR
			WebSetup();
		#endif
		
	}

	private void WebSetup()
	{
		// get set number
		_setNumber = GetSetNumber();
		// get bar type
		var barIndex = GetBarType();
		if( barIndex != -1 )
			ChangeHpBar( _currentHpBarindex );
		RemoveLoader();		
		
		Debug.Log( "PARSED SET NUMBER" + _setNumber  );
		Debug.Log( "PARSED BAR NUMBER" + _setNumber  );
	}

	public void RegisterLevel( LevelManager levelManager )
	{
		Paused = true;
		_initialized = false;
		_hasTrialNumber = false;
		_fireLoaded = false;
		_playerLoaded = false;
		
		DataCollector.GetTrial( _sessionId, data =>
		{
			_trialNumber = Int32.Parse(
				JsonUtility.FromJson< DataObject >( data ).trial );
			_hasTrialNumber = true;
		} );
		
		Debug.Log( "Registering new level " + _currentLevel );
		_levelManager = levelManager;
	}

	public void RestartLevel()
	{
		NextLevel("restart");
	}

	private void Update()
	{
		if ( Input.GetKeyDown( KeyCode.R ) && Paused )
		{
			RestartLevel();
		}
		else if ( Input.GetKeyDown( KeyCode.Escape ) )
		{
			TogglePause( !Paused );
		}
		else if ( Input.GetKeyDown( KeyCode.Q ) && Paused )
		{
			DataCollector.InvalidateTrial( _sessionId, _trialNumber );
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
		else if ( _loadingScreen.activeSelf && Input.anyKeyDown && !_loadingLevel)
		{
			_loadingScreen.SetActive( false );
			TogglePause( false );
		}

		if ( _uploadComplete ) _uploadModal.SetActive( false );

		if ( _playerLoaded && _fireLoaded && _hasTrialNumber && !_initialized )
		{
			_initialized = true;
			TogglePause( false );
		}
	}

	private void TogglePause( bool pause )
	{
		Paused = pause;
		_pauseScreen.SetActive( Paused );
	}

	public void GoToMenu()
	{
		_gameInterface.gameObject.SetActive( false );
		ResetUi();
		SceneManager.LoadScene( Levels.MainMenu );
		_gameOver = false;
	}

	public void NextLevel( [CanBeNull] string customLevel = null )
	{
		if ( _levelCount == -1 ) return;
		
		ResetUi();

		if ( customLevel == "restart" )
		{
			_currentLevel--;
		} else if ( !string.IsNullOrEmpty( customLevel ) )
		{
			try
			{
				_currentLevel = Int32.Parse( customLevel ) - 1;
			}
			catch
			{
				return;
			}
		}
		
		_currentLevel++;

		if ( _currentLevel <= _levelCount )
		{
			_gameInterface.gameObject.SetActive( true );
			_playerLoaded = false;
			_fireLoaded = false;
			StartCoroutine( LoadLevel() );
		}
		else GameHasEnded();
	}

	private IEnumerator LoadLevel()
	{
		_loadingLevel = true;
		_gameInterface.enabled = true;
		_loadingScreen.SetActive( true );
		TogglePause( true );
		AsyncOperation async = SceneManager.LoadSceneAsync( Levels.Level );
		while ( !async.isDone )
		{
			Debug.Log( async.progress  );
			yield return null;
		}
		_loadingLevel = false;
	}

	private void GameHasEnded()
	{
		_endOfGameScreen.SetActive( true );
		_endOfGameScreen.transform.Find( "sessionnumber" ).GetComponent< Text >().text = SessionID.ToString();
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
		_currentHpBarindex = index;
		_currentHpBar = _hpBarControllers[ _currentHpBarindex ];

		_currentHpBar.gameObject.SetActive( true );
		_hpbarlabel.text = _currentHpBar.name;
	}

	public void GameOver( DataCollectionController.DataType reason )
	{
		if( !_gameOver ) 
			_levelManager.Player.LogCumulativeData( reason );
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
				case DataCollectionController.DataType.Victory:
					message = "Congratulations you did it!";
					break;
				case DataCollectionController.DataType.Death:
					message = "You're gonna hurt yourself! Get out of there!";
					break;
				case DataCollectionController.DataType.Timeout:
					message = "Reinforcements have arrived! Take a breather!";
					break;
				default:
					message = "Game Over!";
					break;
		}
		
		_deathScreenController.setMessage( message );
		_deathScreenController.setScore(  _levelManager.Player.GetScore().ToString());;
		_deathScreenController.SetFlameRating( _levelManager.FireSystem.GetActiveFlames(), _levelManager.FireSystem.GetTotalFlames(), 0f, 0f );
		_deathScreenController.SetHealthRating( _levelManager.Player.GetRemainingHitPoints(), _levelManager.Player.GetStartingHealth() );
		_deathScreenController.SetTimeRating( GetTimeRemaining(), _levelManager.TotalTime() );
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
		GameOver( DataCollectionController.DataType.Timeout );
	}

	public void ResetUi()
	{
		_deathScreenController.hide();
		_hpbarlabel.text = _currentHpBar.name;
		TogglePause( false );
		_gameOver = false;
	}

	public double GetTimeRemaining()
	{
		if( _levelManager )
			return Math.Round( _levelManager.GetTimeLeft() );
		return 0;
	}

	public int GetActiveFlames()
	{
			return _levelManager.FireSystem.GetActiveFlames();
	}

	public string GetHpBarType()
	{
		return _hpbarlabel.text;
	}

	public void LoadFireSystem( FireSystemController fireSystem, [CanBeNull] Action cb )
	{
		DataCollector.GetConfig( 
			_currentLevel.ToString(), 
			_setNumber.ToString(), 
			data => {
				Debug.Log("Attempting to load fire config");
				JsonUtility.FromJsonOverwrite( data, fireSystem );
				_fireLoaded = true;
				if ( cb != null ) cb();
			} );
	}

	public void LoadPlayer( PlayerController player, Action cb )
	{
		if ( _playerConfig == null )
		{
			DataCollector.GetConfig( "player", data =>
			{
				_playerConfig = data;
				JsonUtility.FromJsonOverwrite( _playerConfig, player );
				if ( cb != null ) cb();
				_playerLoaded = true;
			} );
		}
		else
		{
			JsonUtility.FromJsonOverwrite( _playerConfig, player );
			_playerLoaded = true;
		}
	}

	public void ReloadConfigs()
	{
		DataCollector.GetBarType( data =>
		{
			ChangeHpBar( Int32.Parse(
				JsonUtility.FromJson< DataObject >( data ).bar ) );
		} );
		
		DataCollector.GetNewID( data =>
		{
			_sessionId = Int32.Parse(
				JsonUtility.FromJson< DataObject >( data ).id );
			FindObjectOfType<MainMenuController>().SetSessionId( _sessionId.ToString() );
		} );

		DataCollector.GetNumberOfLevels( data => {
			_levelCount = Int32.Parse(
				JsonUtility.FromJson< DataObject >( data ).count );
		}, _setNumber.ToString() );
	}

	public void SetMTurkId( string text )
	{
		int id = Int32.Parse( text );

		_turkId = id;

		_sessionId = id * 1234 + 4030201;
	}
}
