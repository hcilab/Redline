using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class MenuAvatarController : MonoBehaviour
{
	private Animation _animation;

	void Start()
	{
		_animation = GetComponentInChildren< Animation >();
		_animation.CrossFade( "Idle" );
		_animation.Stop();
		StartCoroutine( StartIdleAnimation() );
	}

	private IEnumerator StartIdleAnimation()
	{
		yield return new WaitForSeconds( Random.Range( 0, 2 ) );
		_animation.CrossFadeQueued( "JumpA" );
		_animation.CrossFadeQueued( "Idle" );
	}

	// Update is called once per frame
	void Update ()
	{
	}
}
