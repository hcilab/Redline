using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

	void Awake()
	{
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
		    || !(Time.time - _lastSpawn > _spawnDelay)) return;
		
		var instance = _poolController.Spawn() as FloatingNumber;
			
		if( !instance ) throw new NullReferenceException();
				
		Vector2 screenPosition = Camera.main.WorldToScreenPoint(
			new Vector3(
				location.position.x + Random.Range(-.2f, .2f),
				0f,
				location.position.z + Random.Range(-.2f, .2f)
			));
			
		instance.transform.SetParent( _canvas.transform, false );
		instance.transform.position = screenPosition;
		(instance as FloatingNumber).SetNumber(_accumulator);
		(instance as FloatingNumber).StartPlayback();
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

public abstract class FloatingNumber : ObjectPoolItem
{
	protected Animator Animator;

	protected Text TextField;

	void Awake()
	{
		Animator = GetComponent< Animator >();
		TextField = Animator.GetComponentInChildren< Text >();
	}

	public void SetNumber( double number )
	{
		TextField.text = number.ToString();	
	}

	public void StartPlayback()
	{
		Animator.enabled = true;
	}

	public abstract void AnimationComplete();
	
	public void OnDestroy()
	{
		Destroy( Animator );
	}

	public override void Disable()
	{
		transform.position = new Vector3( 200, 200, 200 );
		Animator.enabled = false;
		base.Disable();
	}

	public override void Enable()
	{
		Animator.enabled = true;
		base.Enable();
	}
}
