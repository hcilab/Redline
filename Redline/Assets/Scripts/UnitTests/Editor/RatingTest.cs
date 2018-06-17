using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngineInternal;

public class RatingTest
{
	private DeathScreenController _deathScreenController;

	[SetUp]
	public void TestSetup()
	{
		var gameObject = new GameObject();
		_deathScreenController = gameObject.AddComponent< DeathScreenController >();
	}
	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator DeathScreenShouldBeActiveByDefault() 
	{
		
		Assert.AreEqual( true, _deathScreenController.gameObject.activeSelf );

		yield return null;
	}
	
	[UnityTest]
	public IEnumerator DeathScreenShouldBeInactiveAfterCallingHide() 
	{

		_deathScreenController.hide();
		
		Assert.AreEqual( false, _deathScreenController.gameObject.activeSelf );
		yield return null;
	}

	[UnityTest]
	public IEnumerator NoStarsShouldBeAwardedOnDeath()
	{
		var hpRating = _deathScreenController.SetHealthRating( 0, 100 );
		
		Assert.AreEqual( 0, hpRating );

		yield return null;
	}
	
	[UnityTest]
	public IEnumerator FiveStarsShouldBeAwardedOnFullHealth()
	{
		var hpRating = _deathScreenController.SetHealthRating( 100, 100 );
		
		Assert.AreEqual( 10, hpRating );

		yield return null;
	}
}
