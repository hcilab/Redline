using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameMaster : MonoBehaviour
{
	[DllImport( "__Internal" )]
	private static extern void RemoveLoader();
	
	[DllImport( "__Internal" )]
	private static extern int GetSetNumber();
	
	[DllImport( "__Internal" )]
	private static extern int GetBarType();

	[DllImport( "__Internal" )]
	private static extern int GetId();

	[DllImport( "__Internal" )]
	private static extern int GetGender();

	[DllImport( "__Internal" )]
	private static extern void RedirectOnEnd();
	
	private bool _gameOver;
	private bool _paused;
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
	[SerializeField] private LoadingScreenController _loadingScreen;
	
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
	private int _setNumber = 1;
	private int _avatarGender = 1;
	[SerializeField] private string[] _victoryPhrases = new string[0];
	[SerializeField] private string[] _timeoutPhrases = new string[0];
	[SerializeField] private string[] _deathPhrases = new string[0];

	public int AvatarGender
	{
		get { return _avatarGender; }
	}

	public bool Paused
	{
		get
		{
			return _paused ||
			       _loadingScreen.gameObject.activeSelf ||
			       _deathScreenController.gameObject.activeSelf;
		}
	}

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

		#if UNITY_WEBGL && !UNITY_EDITOR
			WebSetup();
	        ReloadConfigs(false, false, true);
			RemoveLoader();
	    #else
			ReloadConfigs();
		#endif
	}

	private void WebSetup()
	{
		var newId = GetId();

		if ( newId != -1 )
			_sessionId = newId;

		// get set number
		_setNumber = GetSetNumber();
		Debug.Log( "PARSED SET NUMBER " + _setNumber  );
		// get bar type
		var barIndex = GetBarType();
		Debug.Log( "PARSED BAR NUMBER" + barIndex  );
		if( barIndex != -1 )
			ChangeHpBar( barIndex );

		//get avatar gender
		var gender = GetGender();
		Debug.Log( "PARSED GENDER NUMBER " + gender );
		if ( gender != -1 )
			_avatarGender = gender;

	}

	public void RegisterLevel( LevelManager levelManager )
	{
		_paused = true;
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
		#if UNITY_EDITOR
		if ( Input.GetKeyDown( KeyCode.R ) && _paused )
		{
			RestartLevel();
		}
		else if ( Input.GetKeyDown( KeyCode.Escape ) )
		{
			TogglePause( !_paused );
		}
		else if ( Input.GetKeyDown( KeyCode.Q ) && _paused )
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

		#endif

		if ( _playerLoaded && _fireLoaded && _hasTrialNumber &&
		          _loadingScreen.gameObject.activeSelf && Input.anyKeyDown && !_loadingLevel)
		{
			_initialized = true;
			_loadingScreen.gameObject.SetActive( false );
			TogglePause( false );
		}

		if ( _uploadComplete ) _uploadModal.SetActive( false );

	}

	private void TogglePause( bool pause )
	{
		_paused = pause;
		_pauseScreen.SetActive( _paused );
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

		if ( _currentLevel < _levelCount )
		{
			_currentLevel++;
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
		_loadingScreen.gameObject.SetActive( true );
		TogglePause( true );
		AsyncOperation async = SceneManager.LoadSceneAsync( Levels.Level );
		while ( !async.isDone )
		{
			_loadingScreen.UpdateProgress( async.progress );
			Debug.Log( async.progress  );
			yield return null;
		}
		_gameInterface.enabled = true;
		_loadingScreen.LoadingComplete();
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
		_currentHpBar.gameObject.SetActive( false );
		_currentHpBarindex = index;
		_currentHpBar = _hpBarControllers[ _currentHpBarindex ];

		_currentHpBar.gameObject.SetActive( true );
		_hpbarlabel.text = _currentHpBar.name;
		Debug.Log( "change HP bar to " + _currentHpBar.name );
	}

	public IEnumerator GameOver( DataCollectionController.DataType reason )
	{
		_paused = true;
		//_gameOver = true;
		yield return new WaitForSecondsRealtime( 0.5f );
		if( !_gameOver )
			_levelManager.Player.LogCumulativeData( reason );

		_uploadComplete = false;
		_uploadModal.GetComponentInChildren< Text >().text = "Uploading Data....";
		_uploadModal.SetActive( true );

		StartCoroutine( DataCollector.ProcessUploadBacklog( progress =>
		{
			_uploadModal.GetComponentInChildren< Text >().text =
				"Uploading data: " +
				Mathf.RoundToInt(progress * 100) + "%";
			Debug.Log( "Upload progress: " + progress );
			if ( progress >= 1f && !_uploadComplete)
			{
				_uploadComplete = true;
				_uploadModal.SetActive( false );
			}
		} ) );

		string message;
		switch ( reason )
		{
				case DataCollectionController.DataType.Victory:
					message = "You put out the fire! " + RandomPhrase( _victoryPhrases );
					break;
				case DataCollectionController.DataType.Death:
					message = "You're going to hurt yourself! " + RandomPhrase( _deathPhrases );
					break;
				case DataCollectionController.DataType.Timeout:
					message = "Time's up! " + RandomPhrase( _timeoutPhrases );
					break;
				default:
					message = "Game Over!";
					break;
		}

		FindObjectOfType<PlayerController>().Death();

		_deathScreenController.setMessage( message );
		_deathScreenController.setScore(  _levelManager.Player.GetScore().ToString());;
		_deathScreenController.SetFlameRating( _levelManager.FireSystem.GetActiveFlames(), _levelManager.FireSystem.GetTotalFlames(), 0f, 0f );
		_deathScreenController.SetHealthRating( _levelManager.Player.GetRemainingHitPoints(), _levelManager.Player.GetStartingHealth() );
		_deathScreenController.SetTimeRating( GetTimeRemaining(), _levelManager.TotalTime() );
		_deathScreenController.show();
	}

	private static string RandomPhrase( string[] phraseSet )
	{
		return phraseSet[ Random.Range( 0, phraseSet.Length ) ];
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
		StartCoroutine(
			GameOver( DataCollectionController.DataType.Timeout ) );
	}

	public void ResetUi()
	{
		_uploadComplete = false;
		_uploadModal.SetActive( false );
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
				Debug.Log("Attempting to load player config");
				_playerConfig = data;
				JsonUtility.FromJsonOverwrite( _playerConfig, player );
				if ( cb != null ) cb();
				_playerLoaded = true;
			} );
		}
		else
		{
			JsonUtility.FromJsonOverwrite( _playerConfig, player );
			if ( cb != null ) cb();
			_playerLoaded = true;
		}
	}

	public void ReloadConfigs( bool bar = true, bool id = true, bool levels = true )
	{
		if( bar )
			DataCollector.GetBarType( data =>
			{
				ChangeHpBar( Int32.Parse(
					JsonUtility.FromJson< DataObject >( data ).bar ) );
			} );

		if( id )
			DataCollector.GetNewID( data =>
			{
				_sessionId = Int32.Parse(
					JsonUtility.FromJson< DataObject >( data ).id );
				FindObjectOfType<MainMenuController>().SetSessionId( _sessionId.ToString() );
			} );
		if( levels )
			DataCollector.GetNumberOfLevels( data => {
				_levelCount = Int32.Parse(
					JsonUtility.FromJson< DataObject >( data ).count );
			}, _setNumber.ToString() );
	}

	public void ExitGame()
	{
		#if UNITY_WEBGL && !UNITY_EDITOR
			RedirectOnEnd();
		#endif
		Application.Quit();
	}
}
