using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class NumberController : MonoBehaviour
{
	[SerializeField] private float _spawnDelay = 0.3f;
	[SerializeField] private int _numberPoolSize = 10;
	[SerializeField] private ObjectPoolItem _numberPrefab;
	private Canvas _canvas;
	private double _accumulator;
	private ObjectPoolController _poolController;
	private float _lastSpawn;
	private GameMaster _gameMaster;

	void Awake()
	{
		_gameMaster = FindObjectOfType< GameMaster >();
		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		_canvas = FindObjectOfType<Canvas>();
		_poolController = GameMaster.InstantiatePool(_numberPoolSize, _numberPrefab);
		_lastSpawn = Time.time;
	}

	public void SpawnNumber(double damage, Transform location)
	{
		_accumulator += damage;
		
		if (!_poolController.ObjectsAvailable() 
		    || !((Time.time - _lastSpawn) > _spawnDelay)) return;
		
		var instance = _poolController.Spawn();
			
		if(!instance) throw new NullReferenceException();
				
		Vector2 screenPosition = Camera.main.WorldToScreenPoint(
			new Vector3(
				location.position.x + Random.Range(-.2f, .2f),
				0f,
				location.position.z + Random.Range(-.2f, .2f)
			));
			
		instance.transform.SetParent( _canvas.transform, false );
		instance.transform.position = screenPosition;
		(instance as FloatingNumber).setText(_accumulator.ToString());
		(instance as FloatingNumber).startPlayback();
		_accumulator = 0;
		_lastSpawn = Time.time;
	}

	public void RemoveNumber(ObjectPoolItem sender)
	{
		_poolController.Remove(sender);
	}

	private void OnDestroy()
	{
		if ( _poolController != null )
		{
			foreach ( var component in _poolController.GetComponents<DamageNumber>(  ) )
			{
				Destroy( component );
			}
			Destroy(_poolController);
		}
	}
}

internal interface FloatingNumber
{
	void setText(string text);
	void startPlayback();
}
