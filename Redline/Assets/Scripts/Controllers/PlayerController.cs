using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : IHPSource
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
	[SerializeField] private double _recoveryThreshold = 5f;
	[SerializeField] private float _recoveryRate = 0;
	[SerializeField] private float _healthReward = 0f;
	
	
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
	private double _distanceTravelled = 0;
	private double _totalDistanceTravelled = 0;
	private Vector3 prevPosition;
	private double _waterUsed = 0;
	private double _totalWaterUsed = 0;
	private int _frames;
	private double _averageFps;
	private bool _initalizeAverages = true;
	private double _accumulatedDamage = 0f;
	private Animation _animation;
	private ParticleSystem.EmissionModule _water;
	private LineRenderer _outline;
	private GameObject _avatar;
	private bool _initialized = false;

	// Use this for initialization

	void Start ()
	{
		HasDamageAnimation = true;
		_frames = 0;
		_hitPoints = _totalHp;

		//distance travelled and water sprayed
		_distanceTravelled = 0;
		_waterUsed = 0;
		prevPosition = transform.position;

		_outline = gameObject.AddComponent<LineRenderer>();
		
		_enemiesNearBy = new List<Collider>();
		_myBody = GetComponent<Rigidbody>();
		_water = GetComponentInChildren< ParticleSystem >().emission;
	}

	public void Initialize()
	{
		_enemiesNearBy = new List<Collider>();
		_hitPoints = _totalHp;
		_lastTick = 0f;
		_averageActiveFlames = _levelManager.GameMaster.GetActiveFlames();
		string avatar;
		switch ( _levelManager.GameMaster.AvatarGender )
		{
				case 0:
					avatar = "Eliza_Prefab";
					break;
				case 1:
					avatar = "Joel_Prefab";
					break;
				default:
					avatar = "Eliza_Prefab";
					break;
		}
		_avatar = Instantiate( Resources.Load( "Prefabs/" + avatar, typeof( GameObject ) ), transform ) as GameObject;
		_avatar.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
		foreach ( Transform child in _avatar.GetComponentsInChildren<Transform>(  ) )
		{
			child.gameObject.layer = 8;
		}
		_animation = _avatar.GetComponent< Animation >();
		_initialized = true;
	}

	// Update is called once per frame

	void Update ()
	{
		if( !_initialized ) return;
		
		_frames++;
		if ( _levelManager.GameMaster.Paused ) return;
		
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");
		
		Vector3 movement = new Vector3( x, 0f, z);

		if( x != 0 || z != 0 ) _animation.CrossFade( "Run" );
		else _animation.CrossFade( "Idle" );
		
		gameObject.transform.position = gameObject.transform.position + movement * _speed * Time.deltaTime;
		_myBody.velocity = Vector3.zero;
		_myBody.useGravity = false;
		
		if ( Input.GetMouseButtonDown( 0 ) )
		{
			_water.enabled = true;
			GetComponentInChildren< ParticleSystem >().Play();
//			
		} else if ( Input.GetMouseButtonUp( 0 ) )
		{
			_water.enabled = false;
		}
		LookAtMouse();

		 
		if ( _hitPoints <= 0 )
		{
			enabled = false;
			StartCoroutine(
				_levelManager.GameMaster.GameOver( DataCollectionController.DataType.Death ));
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
			prevPosition = transform.position;
			_waterUsed = 0;
		}

		//for water used
		if(_water.enabled){
			_waterUsed += 0.01f;
			_totalWaterUsed += 0.01f;
		}
	}

	private void OnDrawGizmos()
	{
		_outline.startWidth = 0.1f;
		_outline.endWidth = 0.1f;
		_outline.positionCount = 129;
		_outline.useWorldSpace = false;
	
		float deltaTheta = (float) (2.0 * Mathf.PI) / 128;
		float theta = 0f;
	
		for (int i = 0; i < 129; i++)
		{
			float x = 4.2f * Mathf.Cos(theta);
			float z = 4.2f * Mathf.Sin(theta);
			Vector3 pos = new Vector3(x, 1, z);
	
			_outline.SetPosition(i, pos);
			theta += deltaTheta;
		}
	}
	//Not to be confused with the other log data (data collector)
	public void LogData( double fps )
	{
		_distanceTravelled = Vector3.Distance(transform.position, prevPosition);
		_totalDistanceTravelled += _distanceTravelled;
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
			, _levelManager.GameMaster.TurkId.ToString()
			, _levelManager.GameMaster.SessionID.ToString()
			, _levelManager.GameMaster.TrialNumber.ToString()
			, _levelManager.GameMaster.CurrentLevel.ToString()
			, _levelManager.GameMaster.SetNumber.ToString()
			, _levelManager.GameMaster.GetHpBarType()
			, _hitPoints.ToString()
			, _logDamage.ToString()
			, (_logFireExtinguished - _logScore).ToString()
			, _enemiesNearBy.Count.ToString()
			, averageIntensity.ToString()
			, _levelManager.GameMaster.GetActiveFlames().ToString()
			, _distanceTravelled.ToString()
			, _waterUsed.ToString()
			, fps.ToString()
		);
	}

	public void Death()
	{
		_water.enabled = false;
		_animation.Play( "Idle" );
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
			, _levelManager.GameMaster.TurkId.ToString()
			, _levelManager.GameMaster.SessionID.ToString()
			, _levelManager.GameMaster.TrialNumber.ToString()
			, _levelManager.GameMaster.CurrentLevel.ToString()
			, _levelManager.GameMaster.SetNumber.ToString()
			, _levelManager.GameMaster.GetHpBarType()
			, _hitPoints.ToString()
			, _totalDamageTaken.ToString()
			, _logFireExtinguished.ToString()
			, _averageEnemiesNearBy.ToString()
			, _averageNearByIntensity.ToString()
			, _averageActiveFlames.ToString()
			, _totalDistanceTravelled.ToString()
			, _totalWaterUsed.ToString()
			, _averageFps.ToString()
			, type
			);

		//Reset some telemetry
		_totalWaterUsed = 0;
		_totalDistanceTravelled = 0;
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

			_accumulatedDamage += totalDmg;
			if ( _accumulatedDamage >= _hitPoints )
			{
				_hitPoints = 0;
			}
		}

		if ( Time.time - _lastTick > _damageTick && _accumulatedDamage > 30 && _hitPoints > 0 ) 
		{
			_animation.Play( "Damage" );
			if(DamageAnimation != null )
				DamageAnimation();
			_levelManager.GameMaster.GetDamageNumberController().SpawnNumber( _accumulatedDamage, transform.position);
			_hitPoints -= _accumulatedDamage;
			
			_totalDamageTaken += _accumulatedDamage;
			_logDamage += _accumulatedDamage;

			_accumulatedDamage = 0;
			_lastTick = Time.time;
		}

		if ( _hitPoints > 0 && totalDmg < _recoveryThreshold ) RegainHp( _recoveryRate );

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

	public override double GetHealth()
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
		if ( Math.Abs( intensity ) < 0.1f ) RegainHp( _healthReward ); 
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

	private void RegainHp( float healthReward )
	{
		if( _hitPoints + healthReward < _totalHp ) _hitPoints += healthReward;
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
}
