using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{

	[SerializeField] private float speed = 3.0f;
	[SerializeField] private float rotationSpeed = 130f;
	[SerializeField] private double damage = 0.2;

	private Rigidbody myBody;
	private double hitPoints = 100;
	private DamageNumberController _damageNumberController;
	private List<Collider> enemiesNearBy;
	
	// Use this for initialization
	void Start ()
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
			Vector3 pos = new Vector3(x, 0, z);

			outline.SetPosition(i, pos);
			theta += deltaTheta;
		}
		
		enemiesNearBy = new List<Collider>();
		myBody = GetComponent<Rigidbody>();
		_damageNumberController = GameMaster.GetDamageNumberController();
	}
	
	// Update is called once per frame
	void Update ()
	{
		
		if (Input.GetKey(KeyCode.Space) && hitPoints > 0)
		{
			if (hitPoints > 0)
			{
				hitPoints -= damage;
				_damageNumberController.SpawnDamageNumber( damage, transform );
			}
		};

		if (Input.GetKeyUp(KeyCode.R)) hitPoints = 100;
		LookAtMouse();
	}

	private void OnTriggerEnter(Collider other)
	{
		EnemyController enemy = other.GetComponentInParent<EnemyController>();
		if (!enemiesNearBy.Contains(other) && enemy)
		{
			enemy.setActive();
			Debug.Log("Adding new enemy: " + enemiesNearBy.Count+1);
			enemiesNearBy.Add(other);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		Debug.Log("Removing " + enemiesNearBy.IndexOf(other)+1);
		if (enemiesNearBy.Remove(other))
		{
			other.GetComponentInParent<EnemyController>().setInactive();
		}
	}
	
//	private void TakeDamage()
//	{
//		foreach (Collider enemyCollider in enemiesNearBy)
//		{
//			var enemyDamage = enemy.getIntensity()
//			                  /
//			                  Vector3.Distance(enemy.transform.position, transform.position)
//			                  *
//			                  Time.deltaTime;
//			
//			hitPoints -= enemyDamage;
//			_damageNumberController.SpawnDamageNumber( enemyDamage, transform );
//
//		}
//	}

	private void FixedUpdate()
	{
		float x = Input.GetAxis("Horizontal");
		float y = Input.GetAxis("Vertical");
		
		Vector3 movement = new Vector3( x, y, 0f);

		if (myBody != null)
		{
			myBody.velocity = movement * speed;
		}
	}

	private void LookAtMouse( )
	{
		Plane mousePlane = new Plane( Vector3.forward, transform.position);

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		float hitDist = 0f;
		
		if (mousePlane.Raycast(ray, out hitDist))
		{
			Vector3 point = ray.GetPoint(hitDist);

			Quaternion rotation = Quaternion.LookRotation(point - transform.position, Vector3.forward);
			transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
		}
	}

	public double GetHealth()
	{
		return hitPoints / 100f;
	}
}
