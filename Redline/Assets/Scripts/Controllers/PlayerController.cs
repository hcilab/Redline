using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
	[SerializeField] private LevelManager _levelManager;
	[SerializeField] private int _viewDistance = 40;
	[SerializeField] private float _speed = 3.0f;
	[SerializeField] private float _rotationSpeed = 130f;
	[SerializeField] private double _damageScaling = 10f;
	[SerializeField] private double _totalHp = 100;
	[SerializeField] private bool _showCollider = false;
	[SerializeField] private double _damageTick = 0f;
	[SerializeField] private double _loggingTick = 1f;
	
	
	private Rigidbody _myBody;
	private double _hitPoints;
	private List<Collider> _enemiesNearBy;
	private double _score = 0;
	private double _lastTick = 0f;
	private double _lastLog = 0f;
	private double _logDamage = 0f;
	private double _logScore = 0f;
	private int _logFireExtinguished = 0;
	private double _totalDamageTaken = 0f;
	private double _averageEnemiesNearBy = 0;
	private double _averageNearByIntensity = 0;
	private double _averageActiveFlames = 0;
	private int _frames;
	private double _averageFps;
	private bool _initalizeAverages = true;
	private double _accumulatedDamage = 0f;
	private Action _damageAnimation;

	// Use this for initialization

	void Start ()
	{
		
		_frames = 0;
		_hitPoints = _totalHp;

		if ( _showCollider )
		{
			LineRenderer outline = gameObject.AddComponent<LineRenderer>();
	
			outline.startWidth = 0.1f;
			outline.endWidth = 0.1f;
			outline.positionCount = 129;
			outline.useWorldSpace = false;
	
			float deltaTheta = (float) (2.0 * Mathf.PI) / 128;
			float theta = 0f;
	
			for (int i = 0; i < 129; i++)
			{
				float x = 4.2f * Mathf.Cos(theta);
				float z = 4.2f * Mathf.Sin(theta);
				Vector3 pos = new Vector3(x, 1, z);
	
				outline.SetPosition(i, pos);
				theta += deltaTheta;
			}
		}
		
		_enemiesNearBy = new List<Collider>();
		_myBody = GetComponent<Rigidbody>();
	}

	public void Initialize()
	{
		_enemiesNearBy = new List<Collider>();
		_hitPoints = _totalHp;
		_lastTick = 0f;
		_averageActiveFlames = _levelManager.GameMaster.GetActiveFlames();
	}

	// Update is called once per frame

	void Update ()
	{
		_frames++;
		if ( _levelManager.GameMaster.Paused ) return;
		
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");
		
		Vector3 movement = new Vector3( x, 0f, z);

		gameObject.transform.position = gameObject.transform.position + movement * _speed * Time.deltaTime;
		_myBody.velocity = Vector3.zero;
		_myBody.useGravity = false;
		
		var w = GetComponentInChildren< ParticleSystem >().emission;
		if ( Input.GetMouseButtonDown( 0 ) )
		{
			w.enabled = true;
			GetComponentInChildren< ParticleSystem >().Play();
//			
		} else if ( Input.GetMouseButtonUp( 0 ) )
		{
			w.enabled = false;
		}
		LookAtMouse();
		 
		if ( _hitPoints <= 0 )
		{
			_levelManager.GameMaster.GameOver( DataCollectionController.DataType.Death );
			enabled = false;
		}
		
		TakeDamage();
		
		if ( Time.time - _lastLog > _loggingTick )
		{
			// time, id, level, hp, damage, score, 
			LogData( _frames / _loggingTick );
			_frames = 0;
			_logDamage = 0f;
			_logScore = _logFireExtinguished;
			_lastLog = Time.time;
		}
	}

	public void LogData( double fps )
	{
		
		var averageIntensity = AverageIntensity( _enemiesNearBy );

		if ( _initalizeAverages )
		{
			_averageNearByIntensity = averageIntensity;
			_averageEnemiesNearBy = _enemiesNearBy.Count;
			_averageActiveFlames = _levelManager.GameMaster.GetActiveFlames();
			_averageFps = fps;
		}
		else
		{
			if ( Math.Abs( _averageNearByIntensity ) < 0.005 ) _averageNearByIntensity = averageIntensity;
			else _averageNearByIntensity = ( _averageNearByIntensity + averageIntensity ) / 2;
			_averageEnemiesNearBy = ( _averageEnemiesNearBy + _enemiesNearBy.Count ) / 2;
			_averageActiveFlames = ( _averageActiveFlames + _levelManager.GameMaster.GetActiveFlames() ) / 2;
			_averageFps = ( _averageFps + fps ) / 2;
		}
		
		_levelManager.GameMaster.DataCollector.LogData(
			Time.time
			, _levelManager.GameMaster.GetTimeRemaining().ToString()
			, _levelManager.GameMaster.SessionID
			, _levelManager.GameMaster.TrialNumber
			, _levelManager.GameMaster.CurrentLevel.ToString()
			, _levelManager.GameMaster.GetHpBarType()
			, _hitPoints
			, _logDamage
			, _logFireExtinguished - _logScore
			, _enemiesNearBy.Count
			, averageIntensity
			, _levelManager.GameMaster.GetActiveFlames()
			, fps
		);
	}

	private double AverageIntensity( List< Collider > enemiesNearBy )
	{
		double total = 0f;
		foreach ( var collider in enemiesNearBy )
		{
			total += 
				_levelManager.FireSystem.GetFlameIntensity( 
					collider.GetComponentInParent< FlameController >() );
		}
		if( enemiesNearBy.Count > 0 )
			return total / enemiesNearBy.Count;
		return 0;
	}

	public void LogCumulativeData( DataCollectionController.DataType type )
	{
		_levelManager.GameMaster.DataCollector.LogData( 
			Time.time
			, _levelManager.GameMaster.GetTimeRemaining().ToString()
			, _levelManager.GameMaster.SessionID
			, _levelManager.GameMaster.TrialNumber
			, _levelManager.GameMaster.CurrentLevel.ToString()
			, _levelManager.GameMaster.GetHpBarType()
			, _hitPoints
			, _totalDamageTaken
			, _logFireExtinguished
			, _averageEnemiesNearBy
			, _averageNearByIntensity
			, _averageActiveFlames
			, _averageFps
			, type
			);
	}

	private void OnTriggerEnter(Collider other)
	{
		EnemyController enemy = other.GetComponentInParent<EnemyController>();
		if (!_enemiesNearBy.Contains(other) && enemy)
		{
//			enemy.setActive();
//			Debug.Log("Adding new enemy: " + enemiesNearBy.Count+1);
			_enemiesNearBy.Add(other);
		}
	}

	private void OnTriggerExit(Collider other)
	{
//		Debug.Log("Removing " + enemiesNearBy.IndexOf(other)+1);
		if (_enemiesNearBy.Remove(other))
		{
//			other.GetComponentInParent<EnemyController>().setInactive();
		}
	}
	
	private void TakeDamage()
	{
		double totalDmg = 0;
		foreach (Collider enemyCollider in _enemiesNearBy)
		{
			FlameController enemy = enemyCollider.GetComponentInParent< FlameController >();
			totalDmg += _levelManager.FireSystem.GetFlameIntensity( enemy )
			                  /
			                  Vector3.Distance(enemy.transform.position, transform.position)
			                  *
			                  Time.deltaTime;
		}
		totalDmg = Math.Round( totalDmg * _damageScaling );
		if ( totalDmg > 0.5 && _hitPoints >= 0 ) 
		{
			if ( _accumulatedDamage < _hitPoints )
			{
				_accumulatedDamage += totalDmg;
			} else if ( _accumulatedDamage >= _hitPoints )
			{
				_accumulatedDamage = _hitPoints;
			}
		}

		if ( Time.time - _lastTick > _damageTick && _accumulatedDamage > 30 )
		{
			Debug.Log( "TAKING DAMAGE"  );
			_damageAnimation();
			_levelManager.GameMaster.GetDamageNumberController().SpawnNumber( _accumulatedDamage, transform.position);
			_hitPoints -= _accumulatedDamage;
			
			_totalDamageTaken += _accumulatedDamage;
			_logDamage += _accumulatedDamage;

			_accumulatedDamage = 0;
			_lastTick = Time.time;
		}
		
	}

	private void LookAtMouse( )
	{
		Plane mousePlane = new Plane( Vector3.up, transform.position);

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		float hitDist = 0f;
		
		if (mousePlane.Raycast(ray, out hitDist))
		{
			Vector3 point = ray.GetPoint(hitDist);

			Quaternion rotation = Quaternion.LookRotation(point - transform.position, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, rotation, _rotationSpeed * Time.deltaTime);
		}
	}

	public double GetHealth()
	{
		return _hitPoints / _totalHp;
	}

	public double GetRemainingHitPoints()
	{
		return _hitPoints;
	}

	public double GetScore()
	{
		return _score;
	}

	public void Score( FlameController flame, double intensity, Transform position )
	{
		_score += ( intensity + 1 )* 10;
		_levelManager.GameMaster.GetScoreNumberController().SpawnNumber( (intensity + 1 ) * 10, position.position );
		if ( flame != null )
		{
			if ( _enemiesNearBy.Remove( flame.GetComponentInChildren< Collider >() ) )
			{
				_logFireExtinguished++;
			}
		}
	}

	public bool HasLineOfSight( Vector3 location )
	{
		var distance = Vector3.Distance( location, transform.position );
		if ( distance < _viewDistance )
		{
			Ray ray = new Ray(transform.position, Vector3.Normalize(location - transform.position) * distance);
			var hit = Physics.Raycast( ray, distance );
			if( hit )
				Debug.DrawRay( ray.origin, ray.direction * distance, Color.red  );
			else
			{
				Debug.DrawRay( ray.origin, ray.direction * distance, Color.green );
			}
			return !Physics.Raycast( ray, distance );
		}
		return false;
	}

	public float GetStartingHealth()
	{
		return ( float ) _totalHp;
	}

	public void SetDamageAnimation( Action action )
	{
		_damageAnimation = action;
	}
}
