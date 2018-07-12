using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{

	[SerializeField] private Text _continueMessage;
	[SerializeField] private RectTransform _bar;
	[SerializeField] private RectTransform _bkg;

	private void OnEnable()
	{
		_continueMessage.text = "Loading";
		_bar.sizeDelta = new Vector2( 0, _bar.rect.height );
	}

	public void UpdateProgress( float newWidth )
	{
		_continueMessage.text += ".";
		_bar.sizeDelta = Vector2.Lerp(
			_bar.rect.size,
			new Vector2(
				_bkg.rect.width * newWidth,
				_bar.rect.height
			),
			5 * Time.deltaTime
		);
	}

	public void LoadingComplete()
	{
		_bar.sizeDelta = new Vector2( _bkg.rect.width, _bar.rect.height );
		_continueMessage.text = "Press any button to Start!";
	}
}
