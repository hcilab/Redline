using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
	private static DamageNumberController _damageNumberController;
	private static Dictionary< string, object > _controllerRegistry;

	// Use this for initialization
	void Awake()
	{
		_damageNumberController = GetComponent<DamageNumberController>();
		_controllerRegistry = new Dictionary< string, object >();
	}

	public static DamageNumberController GetDamageNumberController()
	{
		return _damageNumberController;
	}

	public static ObjectPoolController InstantiatePool(int poolSize, ObjectPoolItem item)
	{
		ObjectPoolController pool = Instantiate(
          			Resources.Load<ObjectPoolController>("Prefabs/ObjectPool")
          		);
		
		pool.Init( poolSize, item);
		return pool;
	}

	public static bool RegisterController( string name, object controller )
	{
		if ( _controllerRegistry.ContainsKey( name ) )
		{
			throw new Exception("Controller [" + name + "] is already registered!");
		}
		_controllerRegistry.Add( name, controller );
		return _controllerRegistry.ContainsKey( name );
	}

	public static object GetResgisteredController( string name )
	{
		if ( !_controllerRegistry.ContainsKey( name ) )
		{
			throw new Exception( "[" + name + "] is not registered!" );
		}
		return _controllerRegistry[ name ];
	}
}
