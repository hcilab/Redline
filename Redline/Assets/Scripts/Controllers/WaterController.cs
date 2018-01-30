using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterController : MonoBehaviour
{
	[SerializeField] private double _waterStrength = .5f;
	[SerializeField] private FireSystemController _fireSystemController;
	private ParticleSystem water;
	
	// Use this for initialization
	void Start () {
		water = GetComponent< ParticleSystem >();
	}

	private void OnParticleCollision( GameObject other )
	{
		if ( other.GetComponent<FlameController>(  ) )
		{
			var events = new ParticleCollisionEvent[
				water.GetSafeCollisionEventSize()
			];
			var waterCount = water.GetCollisionEvents( other, events );

			double intensity = 0;
			FlameController flame = _fireSystemController.LowerIntensity(
				other.transform.position,
				_waterStrength * waterCount,
				out intensity
			);

			GetComponentInParent< PlayerController >().Score( flame, intensity, other.transform );

		}
	}
}
