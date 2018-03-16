using UnityEngine;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{

//	[SerializeField] private Animator _animator;

	void Awake()
	{
//		_animator.enabled = false;
	}

	public void show()
	{
		gameObject.SetActive( true );
//		_animator.enabled = true;
	}

	public void hide()
	{
//		_animator.enabled = false;
		gameObject.SetActive( false );
	}

	public void setScore( string score )
	{
		var scoreText = transform.Find( "final_score" ) as RectTransform;
		scoreText.GetComponent< Text >().text = "Final Score: " + score;
	}

	public void setMessage( string message )
	{
		if( message == null ) message = "Oh no! You died!";
		var msgText = transform.Find( "message" ) as RectTransform;
		msgText.GetComponent< Text >().text = message;

	}
}
