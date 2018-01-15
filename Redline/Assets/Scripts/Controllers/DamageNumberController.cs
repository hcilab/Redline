using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class DamageNumberController : MonoBehaviour
{
	[SerializeField] private float _spawnDelay = 0.3f;
	[SerializeField] private int _numberPoolSize = 10;
	private Canvas _canvas;
	private double _accumulatedDamage;
	private ObjectPoolController _poolController;
	private float _lastSpawn;

	void Start()
	{		
		_canvas = FindObjectOfType<Canvas>();
		DamageNumber numberPrefab = Resources.Load<DamageNumber>("Prefabs/DamageNumbers");
		_poolController = GameMaster.InstantiatePool(_numberPoolSize, numberPrefab);
		_lastSpawn = Time.time;
	}

	public void SpawnDamageNumber(double damage, Transform location)
	{
		_accumulatedDamage += damage;
		
		if (!_poolController.ObjectsAvailable() 
		    || !((Time.time - _lastSpawn) > _spawnDelay)) return;
		
		var instance = _poolController.Spawn() as DamageNumber;
			
		if(!instance) throw new NullReferenceException();
				
		Vector2 screenPosition = Camera.main.WorldToScreenPoint(
			new Vector3(
				location.position.x + Random.Range(-.2f, .2f),
				0f,
				location.position.z + Random.Range(-.2f, .2f)
			));
			
		instance.transform.SetParent( _canvas.transform, false );
		instance.transform.position = screenPosition;
		instance.setText(_accumulatedDamage.ToString());
		instance.startPlayback();
		_accumulatedDamage = 0;
		_lastSpawn = Time.time;
	}

	public void RemoveDamageNumber(DamageNumber sender)
	{
		_poolController.Remove(sender);
	}

	private void OnDestroy()
	{
		Destroy(_poolController);
	}
}
