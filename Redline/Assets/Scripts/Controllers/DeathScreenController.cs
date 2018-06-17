using System;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{

	private int _hpRating;
	private int _flameRating;
	private int _timeRating;
	private int _overallRating;

	[SerializeField] private GameMaster _gameMaster;
	[SerializeField] private float _starWidth = 100;
	[SerializeField] private RectTransform _hpRatingBar;
	[SerializeField] private RectTransform _flameRatingBar;
	[SerializeField] private RectTransform _timeRatingBar;
	[SerializeField] private Text _sessionId;
	[SerializeField] private Button _nextLevelButton;
	[SerializeField] private Text _infoMessage;

	[SerializeField] private int _cutOff;
//	[SerializeField] private Animator _animator;

	void Awake()
	{
		_sessionId.text = _gameMaster.SessionID.ToString();
	}

	public void show()
	{
		UpdateRatings();
		gameObject.SetActive( true );
	}

	public void hide()
	{
		gameObject.SetActive( false );
	}

	public void setScore( string score )
	{
//		var scoreText = transform.Find( "final_score" ) as RectTransform;
//		scoreText.GetComponent< Text >().text = "Final Score: " + score;
	}

	public int SetHealthRating( double hpRemaining, float totalHp )
	{
		float hpPercentage = ( float ) ( hpRemaining / totalHp );
		if ( Math.Abs( hpRemaining ) > 0.000001f ) {
			hpPercentage += 0.15f; //give 10% hp mercy
			_hpRating = ( int ) ( RoundStarRating(
				                      Mathf.Clamp( hpPercentage, 0f, 1f )  
				                      * 5) * 2 );
		}
		else _hpRating = 0;

		return _hpRating;
	}

	public int SetFlameRating( int activeFlames, int totalFlames, float averageIntensity, float maxIntensity )
	{
		_flameRating = ( int ) (RoundStarRating( ( 1 - activeFlames / totalFlames ) * 5 ) * 2);
		return _flameRating;
	}

	public int SetTimeRating( double timeRemaining, float totalTime )
	{
		_timeRating = (int) ( RoundStarRating( 
			              Mathf.Clamp( (float) timeRemaining / ( totalTime - 20 ), 0f, 1f ) //give 20 second mercy
		                                       * 5) * 2);
		return _timeRating;
	}

	private float RoundStarRating( float rating )
	{
		int floor = Mathf.FloorToInt( rating );
		int rounded = Mathf.RoundToInt( rating );

		if ( floor == rounded ) return ( int ) rating;
		if ( floor < rounded ) return ( int ) rating + 0.5f;
		throw new Exception("please the humanity");
	}

	private void UpdateRatings()
	{
		if ( _flameRating >= _cutOff
		     && _timeRating >= _cutOff )
		{
			_nextLevelButton.interactable = true;
			_infoMessage.enabled = false;
		}
		else if ( _hpRating >= 0 )
		{
			_nextLevelButton.interactable = false;
			_infoMessage.enabled = true;
			_infoMessage.text = "In order to proceed you must reach at least "
			                    + _cutOff / 2f + " stars in the time and firefighting categories.";
		}
		else
		{
			_nextLevelButton.interactable = false;
			_infoMessage.enabled = true;
			_infoMessage.text = "Slow down there! Before continuing you'll have to show us you can handle this one.";
		}
		
		_flameRatingBar.sizeDelta = new Vector2( _flameRating * _starWidth / 2, 60 );
		_hpRatingBar.sizeDelta = new Vector2( _hpRating * _starWidth / 2, 60 );
		_timeRatingBar.sizeDelta = new Vector2( _timeRating * _starWidth / 2, 60 );
	}

	public void setMessage( string message )
	{
		var msgText = transform.Find( "message" ) as RectTransform;
		msgText.GetComponent< Text >().text = message;
	}
}
