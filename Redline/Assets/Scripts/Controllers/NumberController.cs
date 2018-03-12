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
	[SerializeField] private Canvas _canvas;
	[SerializeField] private int _poolCount;
	private double _accumulator;
	private ObjectPoolController _poolController;
	private float _lastSpawn;
	[SerializeField] private string _poolName;

	void Awake()
	{
		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		_poolController = GameMaster.InstantiatePool(_numberPoolSize, _numberPrefab, _poolName);
		_lastSpawn = Time.time;
		_poolCount = _poolController.ObjectCount();
	}

	public void SpawnNumber(double content, Vector3 location)
	{
		_accumulator += content;
		
		if (!_poolController.ObjectsAvailable() 
		    || !(Time.time - _lastSpawn > _spawnDelay)
		    || _accumulator < 11 ) return;
		
		var instance = _poolController.Spawn() as FloatingNumber;
			
		if( !instance ) throw new NullReferenceException();
				
		Vector2 screenPosition = Camera.main.WorldToScreenPoint(
			new Vector3(
				location.x + Random.Range(-.2f, .2f),
				0f,
				location.z + Random.Range(-.2f, .2f)
			));
			
		instance.transform.SetParent( _canvas.transform, false );
		instance.transform.position = screenPosition;
		instance.SetNumber(_accumulator);
		instance.StartPlayback();
		_accumulator = 0;
		_lastSpawn = Time.time;
		_poolCount = _poolController.ObjectCount();
	}

	public void RemoveNumber(ObjectPoolItem sender)
	{
		_poolController.Remove(sender);
		_poolCount = _poolController.ObjectCount();
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
		transform.position = new Vector3( -200, -200, -200 );
		Animator.enabled = false;
		base.Disable();
	}

	public override void Enable()
	{
		Animator.enabled = true;
		base.Enable();
	}
}
