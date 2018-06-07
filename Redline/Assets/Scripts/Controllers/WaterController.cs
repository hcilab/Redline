using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterController : MonoBehaviour
{
	[SerializeField] private LevelManager _levelManager;
	private ParticleSystem water;
	
	// Use this for initialization
	void Start () {
		water = GetComponent< ParticleSystem >();
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
				FlameController flame = _levelManager.FireSystem.LowerIntensity(
					other.transform.position,
					waterCount,
					out intensity
				);
				_levelManager.Player.Score( flame, intensity, other.transform );
			}
			catch ( IndexOutOfRangeException ){}
		}
	}
}
