using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool creator and maintainer.
/// Controllers that require access to large amounts of recycable objects should initiate 
/// this controller with a pool size and request and releast items through it.
/// </summary>


//TODO rewrite this to use the payload design for ObjectPoolItems as in GridItem

//TODO don't make it extend MonoBehaviour. It can be set up like the grid controller. 
public class ObjectPoolController : MonoBehaviour, IEnumerable<FlameController>
{
	private ObjectPoolItem _objectPrefab;
	private int _poolSize;
	private Queue<ObjectPoolItem> _pool;
	private Vector3 _outOfView;
	
	//TODO write docs
	/// <summary>
	/// 
	/// </summary>
	/// <param name="poolSize"></param>
	/// <param name="itemPrefab"></param>
	/// <exception cref="TypeLoadException"></exception>
	public void Init(int poolSize, ObjectPoolItem itemPrefab)
	{
		_outOfView =
			Camera.main.transform.position +
			Vector3.Cross( 
				new Vector3( 100, 100, 100 ), 
				Vector3.up
			);
		_poolSize = poolSize;
		_objectPrefab = itemPrefab;
		
		_pool = new Queue<ObjectPoolItem>(_poolSize);
		
		if(!_objectPrefab) throw new TypeLoadException();
		
		for (int i = 0; i < _poolSize; i++)
		{
			ObjectPoolItem newItem = Instantiate( _objectPrefab );
			newItem.transform.position = _outOfView;
			_pool.Enqueue( newItem );
		}
	}

	public bool ObjectsAvailable()
	{
		return _pool.Count > 0;
	}

	/// <summary>
	/// Returns the oldest ObjectPoolItem in the pool and enables it.
	/// </summary>
	/// <returns>An ObjectPoolItem.</returns>
	public ObjectPoolItem Spawn()
	{
		ObjectPoolItem item = null;
		if (_pool.Count > 0)
			item = _pool.Dequeue();
		
		//Once ObjectPoolItems switch to using the payload design this controller
		//won't enable items anymore.
		if (item)
			item.enabled = true;
		return item;
	}

	/// <summary>
	/// Deactives and adds an item back into the pool. 
	/// </summary>
	/// <param name="sender"></param>
	public void Remove(ObjectPoolItem sender)
	{
		sender.enabled = false;
		sender.transform.position = _outOfView;
		_pool.Enqueue(sender);
	}

	private void OnDestroy()
	{
		//loop through and clear all items in the pool
		foreach (ObjectPoolItem item in _pool)
		{
			Destroy(item);
		}
	}

	public IEnumerator<FlameController> GetEnumerator()
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _pool.GetEnumerator();
	}
}
