using System;
using UnityEngine;

[Serializable]
public class ExponentialScale : Interfaces.HpBarScale
{

	[SerializeField] private double power = 2.5;
	
	public override double scale(double percentageHp)
	{
		if (percentageHp <= 0) return 0;
		
		return Math.Pow(percentageHp, power);
	}
}
