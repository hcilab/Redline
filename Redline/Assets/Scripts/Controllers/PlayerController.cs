using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

	[SerializeField] public FireSystemController FireSystemController;
	[SerializeField] private int _viewDistance = 40;
	[SerializeField] private float _speed = 3.0f;
	[SerializeField] private float _rotationSpeed = 130f;
	[SerializeField] private double _damageScaling = 10f;
	[SerializeField] private double _totalHp = 100;
	[SerializeField] private bool _showCollider = false;
	
	
	private Rigidbody _myBody;
	private double _hitPoints;
	private List<Collider> _enemiesNearBy;
	private double _score = 0;
	private GameMaster _gameMaster;
	[SerializeField] private double _damageTick = 0f;
	private double _lastTick;

	// Use this for initialization
	void Start ()
	{
		_gameMaster = FindObjectOfType< GameMaster >();
		
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
		_lastTick = 0f;
	}
	
	// Update is called once per frame

	void Update ()
	{
		if ( _gameMaster.Paused ) return;
		
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");
		
		Vector3 movement = new Vector3( x, 0f, z);

		gameObject.transform.position = gameObject.transform.position + movement * _speed;
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
			_gameMaster.OnDeath();
		}
		else if(Time.time - _lastTick > _damageTick) 
		{
			TakeDamage();
			_lastTick = Time.time;
		}
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
			totalDmg += FireSystemController.GetFlameIntensity( enemy )
			                  /
			                  Vector3.Distance(enemy.transform.position, transform.position)
			                  *
			                  Time.deltaTime;
		}
		totalDmg = Math.Round( totalDmg * _damageScaling );
		if ( totalDmg > 0.5 && _hitPoints >= 0) 
		{
			_gameMaster.GetDamageNumberController().SpawnNumber( totalDmg, transform.position);
			_hitPoints -= totalDmg;
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

	public double GetScore()
	{
		return _score;
	}

	public void Score( FlameController flame, double intensity, Transform position )
	{
		_score += ( intensity + 1 )* 10;
		_gameMaster.GetScoreNumberController().SpawnNumber( (intensity + 1 ) * 10, position.position );
		if ( flame != null )
		{
			_enemiesNearBy.Remove( flame.GetComponentInChildren< Collider >() );
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
}
