using UnityEngine;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{

	[SerializeField] private Animator _animator;

	void Awake()
	{
		_animator.enabled = false;
	}

	public void show()
	{
		_animator.enabled = true;
	}

	public void hide()
	{
		_animator.enabled = false;
	}

	public void setScore( string score )
	{
		GetComponentsInChildren< Text >( true )[ 1 ].text = "Final Score: " + score;
	}
}
