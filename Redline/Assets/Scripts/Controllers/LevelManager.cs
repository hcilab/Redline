using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
	[SerializeField] private PlayerController _player;
	[SerializeField] private FireSystemController _fireSystem;
	
	[SerializeField] private int _levelTime = 60;
	private GameMaster _gameMaster;
	private float _startTime;
	private float _timeLeft = Int32.MaxValue;

	public float StartTime
	{
		get { return _startTime; }
	}
	
	public PlayerController Player
	{
		get { return _player; }
	}

	public FireSystemController FireSystem
	{
		get { return _fireSystem; }
	}

	public GameMaster GameMaster
	{
		get { return _gameMaster; }
	}
	
	// Use this for initialization
	private void Awake()
	{
		SceneManager.sceneLoaded += InitializeLevel;
	}

	private void InitializeLevel( Scene arg0, LoadSceneMode arg1 )
	{
		Debug.Log("initializing level" );
		_gameMaster = FindObjectOfType< GameMaster >();
		
		_gameMaster.RegisterLevel( this );
		
		GameMaster.LoadFireSystem( FireSystem );
		GameMaster.LoadPlayer( Player );
		
		_startTime = Time.time;
		//start the timer
		_timeLeft = _levelTime;
	}

	private void Update()
	{
		if ( _gameMaster.Paused ) return;
		if ( _timeLeft <= 0 )
		{
			GameMaster.OnTimeout();
			FireSystem.enabled = false;
		}
		
		_timeLeft -= Time.deltaTime;
	}

	public float TotalTime()
	{
		return _levelTime;
	}
	
	public float GetTimeLeft()
	{
		return _timeLeft;
	}

}
