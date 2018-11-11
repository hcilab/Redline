using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPComparison : IHPSource
{
	[SerializeField] private double _drainRate = 0.1f;
	[SerializeField] private double _totalHealth;
	private double _remainingHealth;
	private bool _drain = true;
	private bool _paused = false;

	// Use this for initialization
	void Start ()
	{
		_remainingHealth = _totalHealth;
	}
	
	// Update is called once per frame
	void Update ()
	{
		const float TOLERANCE = 0.001f;
		if ( _paused ) return;
		if ( Math.Abs( GetHealth() - 0.75f ) < TOLERANCE || 
		     Math.Abs( GetHealth() - 0.5f ) < TOLERANCE || 
		     Math.Abs( GetHealth() - 0.25 ) < TOLERANCE ) StartCoroutine( Sleep() );
		if ( _drain )
			_remainingHealth -= _drainRate;
		else
			_remainingHealth += _drainRate;

		if ( _drain && _remainingHealth <= 0 ) _drain = false;
		if ( !_drain && _remainingHealth >= _totalHealth ) _drain = true;
	}

	public IEnumerator Sleep()
	{
		_paused = true;
		yield return new WaitForSeconds( 0.5f );
		_paused = false;
	}
	
	public override double GetHealth()
	{
		return _remainingHealth / _totalHealth;
	}
}
