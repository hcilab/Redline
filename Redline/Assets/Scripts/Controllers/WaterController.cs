using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterController : MonoBehaviour
{
	private PlayerController _playerController;
	private FireSystemController _fireSystemController;
	private ParticleSystem water;
	
	// Use this for initialization
	void Start () {
		water = GetComponent< ParticleSystem >();
		_playerController = GetComponentInParent< PlayerController >();
		
		_fireSystemController = _playerController.FireSystemController;
	}

	private void OnParticleCollision( GameObject other )
	{
		if ( other.GetComponent<FlameController>(  ) )
		{
			var events = new List<ParticleCollisionEvent>();
			var waterCount = water.GetCollisionEvents( other, events );
			
			try
			{
				double intensity = 0;
				FlameController flame = _fireSystemController.LowerIntensity(
					other.transform.position,
					waterCount,
					out intensity
				);
				_playerController.Score( flame, intensity, other.transform );
			}
			catch ( IndexOutOfRangeException ){}
		}
	}
}
