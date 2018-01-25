using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

	[SerializeField] private FireSystemController _fireSystemController;
	
	[SerializeField] private float _speed = 3.0f;
	[SerializeField] private float _rotationSpeed = 130f;
	[SerializeField] private double _damage = 0.2;
	[SerializeField] private double _damageScaling = 10f;
	[SerializeField] private double _totalHp = 100;
	[SerializeField] private float _waterStrength = 3;
	[SerializeField] private bool _showCollider = false;
	
	
	private Rigidbody _myBody;
	private double _hitPoints;
	private DamageNumberController _damageNumberController;
	private List<Collider> _enemiesNearBy;
	private double _score = 0;
	private GameMaster _gameMaster;

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
		_damageNumberController = _gameMaster.GetDamageNumberController();
	}
	
	// Update is called once per frame

	void Update ()
	{
		if ( _gameMaster.Paused ) return;
		
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");
		
		Vector3 movement = new Vector3( x, 0f, z);

		gameObject.transform.position = gameObject.transform.position + movement * _speed;
		
		if ( Input.GetMouseButton( 0 ) )
		{
			var cursor = new Vector3(
				Input.mousePosition.x,
				Input.mousePosition.y,
				1f
			);
			cursor = Camera.main.ScreenToWorldPoint( cursor );
			cursor.y = transform.position.y;
			double distance = Vector3.Distance( cursor, transform.position );
			ApplyWater( distance );

			//update the distance of the water stream
			var water = GetComponentInChildren< ParticleSystem >().main;
			water.startSpeed = new ParticleSystem.MinMaxCurve( ( float ) distance * 30 );
			var waterEmission = GetComponentInChildren< ParticleSystem >().emission;
			waterEmission.enabled = true;
		} else if ( Input.GetMouseButtonUp( 0 ) )
		{
			var waterEmission = GetComponentInChildren< ParticleSystem >().emission;
			waterEmission.enabled = false;
		}
		LookAtMouse();
		
		if ( _hitPoints <= 0 )
		{
			_gameMaster.OnDeath( _score );
		}
		else
		{
			TakeDamage();
		}
	}

	private void ApplyWater( double distance )
	{
		double hoseStrength = 1 - distance;
		hoseStrength = hoseStrength < 0 ? 0 : hoseStrength;

		try
		{
			double outIntensity = 0;
			var flame =
				_fireSystemController.LowerIntensity( 
					_waterStrength * hoseStrength, 
					out outIntensity 
					);
			_score += hoseStrength * outIntensity * 100;
			if ( flame != null )
			{
				_score += 10;
				_enemiesNearBy.Remove( flame.GetComponentInChildren< Collider >() );
			}
		}
		catch ( Exception ){}
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
			totalDmg += _fireSystemController.GetFlameIntensity( enemy )
			                  /
			                  Vector3.Distance(enemy.transform.position, transform.position)
			                  *
			                  Time.deltaTime;
		}
		if ( totalDmg > 0.01 && _hitPoints >= 0) 
		{
			totalDmg = Math.Round( totalDmg * _damageScaling );
			_damageNumberController.SpawnDamageNumber( totalDmg, transform );
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
}
