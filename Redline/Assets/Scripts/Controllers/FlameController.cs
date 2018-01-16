//TODO write flame controller

using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class FlameController : ObjectPoolItem
{

	private float _intensity = 0;

	public void SetIntensity( float s )
	{
		_intensity = s;
	}

	public float GetIntensity()
	{
		return _intensity;
	}
}
